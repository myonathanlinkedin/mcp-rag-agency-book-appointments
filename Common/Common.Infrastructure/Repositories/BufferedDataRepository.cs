using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Transactions;

public abstract class BufferedDataRepository<TDbContext, TEntity> : DataRepository<TDbContext, TEntity>
    where TDbContext : DbContext
    where TEntity : Entity, IAggregateRoot
{
    private static readonly ConcurrentDictionary<Type, SemaphoreSlim> entityLocks = new();
    private readonly SemaphoreSlim semaphore;
    private readonly ConcurrentQueue<(TEntity Entity, OperationType Operation, int RetryCount)> operationQueue;
    private readonly ILogger logger;
    private const int MaxRetries = 5;
    private const int MaxQueueRetries = 3;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SemaphoreTimeout = TimeSpan.FromMinutes(5);

    protected BufferedDataRepository(
        TDbContext db,
        ILogger<BufferedDataRepository<TDbContext, TEntity>> logger) : base(db, logger)
    {
        this.logger = logger;
        operationQueue = new ConcurrentQueue<(TEntity, OperationType, int)>();
        semaphore = entityLocks.GetOrAdd(typeof(TEntity), _ => new SemaphoreSlim(1, 1));
    }

    private async Task<TEntity?> ReloadEntityAsync(TEntity entity, CancellationToken cancellationToken)
    {
        try
        {
            // Detach current entity to avoid tracking conflicts
            Data.Entry(entity).State = EntityState.Detached;

            // Reload the entity from database
            var reloadedEntity = await Data.Set<TEntity>()
                .FirstOrDefaultAsync(e => e.Id == entity.Id, cancellationToken);

            // Entity might have been deleted by another process
            if (reloadedEntity == null)
            {
                logger.LogWarning(
                    "Entity of type {EntityType} with ID {Id} was not found during reload. It may have been deleted.",
                    typeof(TEntity).Name, entity.Id);
                return null;
            }

            return reloadedEntity;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reloading entity of type {EntityType} with ID {Id}",
                typeof(TEntity).Name, entity.Id);
            throw;
        }
    }

    private TimeSpan GetExponentialBackoffDelay(int attempt)
    {
        var delay = InitialDelay.TotalMilliseconds * Math.Pow(2, attempt);
        return TimeSpan.FromMilliseconds(Math.Min(delay, MaxDelay.TotalMilliseconds));
    }

    private async Task<bool> TryProcessOperationWithRetriesAsync(TEntity entity, OperationType operation, CancellationToken cancellationToken)
    {
        var attempts = 0;
        var lastException = default(Exception);

        while (attempts < MaxRetries)
        {
            try
            {
                var currentEntity = attempts == 0 ? entity : await ReloadEntityAsync(entity, cancellationToken);

                // If entity doesn't exist anymore and it's not a save operation, we can consider it done
                if (currentEntity == null)
                {
                    if (operation == OperationType.Save)
                    {
                        // For save operations, we'll add it
                        await Data.Set<TEntity>().AddAsync(entity, cancellationToken);
                    }
                    else if (operation == OperationType.Delete)
                    {
                        // For delete operations, it's already gone - consider it a success
                        logger.LogInformation(
                            "Entity of type {EntityType} with ID {Id} already deleted, skipping delete operation",
                            typeof(TEntity).Name, entity.Id);
                        return true;
                    }
                    else
                    {
                        // For update operations when entity doesn't exist, we should handle this based on business logic
                        logger.LogWarning(
                            "Cannot update entity of type {EntityType} with ID {Id} as it no longer exists in the database",
                            typeof(TEntity).Name, entity.Id);
                        throw new DbUpdateConcurrencyException(
                            $"Entity of type {typeof(TEntity).Name} with ID {entity.Id} was not found during concurrency resolution.");
                    }
                }
                else
                {
                    switch (operation)
                    {
                        case OperationType.Save:
                            if (Data.Entry(currentEntity).State == EntityState.Detached)
                            {
                                await Data.Set<TEntity>().AddAsync(currentEntity, cancellationToken);
                            }
                            break;

                        case OperationType.Update:
                            // Apply updates to the reloaded entity
                            if (attempts > 0)
                            {
                                await OnConcurrencyResolveAsync(currentEntity, entity);
                            }
                            Data.Update(currentEntity);
                            break;

                        case OperationType.Delete:
                            Data.Remove(currentEntity);
                            break;
                    }
                }

                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                lastException = ex;
                attempts++;
                if (attempts >= MaxRetries)
                {
                    logger.LogError(ex,
                        "Concurrency conflict could not be resolved after {Attempts} attempts for {EntityType} with ID {Id}",
                        attempts, typeof(TEntity).Name, entity.Id);
                    throw;
                }

                logger.LogWarning(
                    "Concurrency conflict detected for {EntityType} with ID {Id}. Attempt {Attempt} of {MaxRetries}",
                    typeof(TEntity).Name, entity.Id, attempts, MaxRetries);

                var delay = GetExponentialBackoffDelay(attempts);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempts++;
                if (attempts >= MaxRetries)
                {
                    logger.LogError(ex,
                        "Operation failed after {Attempts} attempts for {EntityType} with ID {Id}",
                        attempts, typeof(TEntity).Name, entity.Id);
                    throw;
                }

                var delay = GetExponentialBackoffDelay(attempts);
                await Task.Delay(delay, cancellationToken);
            }
        }

        if (lastException != null)
        {
            throw lastException;
        }

        return false;
    }

    protected virtual async Task OnConcurrencyResolveAsync(TEntity currentEntity, TEntity conflictingEntity)
    {
        // Override this method in derived repositories to handle specific concurrency resolution logic
        // By default, we'll just copy all properties from the conflicting entity
        var properties = typeof(TEntity).GetProperties()
            .Where(p => p.CanRead && p.CanWrite && p.Name != "Id");

        foreach (var prop in properties)
        {
            prop.SetValue(currentEntity, prop.GetValue(conflictingEntity));
        }

        await Task.CompletedTask;
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        if (await semaphore.WaitAsync(SemaphoreTimeout, cancellationToken))
        {
            try
            {
                var failedOperations = new ConcurrentQueue<(TEntity Entity, OperationType Operation, int RetryCount)>();

                while (operationQueue.TryDequeue(out var operation))
                {
                    try
                    {
                        var success = await TryProcessOperationWithRetriesAsync(operation.Entity, operation.Operation, cancellationToken);
                        if (!success && operation.RetryCount < MaxQueueRetries)
                        {
                            // Re-enqueue with incremented retry count
                            operationQueue.Enqueue((operation.Entity, operation.Operation, operation.RetryCount + 1));
                            logger.LogWarning(
                                "Operation {Operation} for entity type {EntityType} with ID {Id} failed, re-queuing. Retry count: {RetryCount}",
                                operation.Operation, typeof(TEntity).Name, operation.Entity.Id, operation.RetryCount + 1);
                        }
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        logger.LogError(ex, "Concurrency error processing operation {Operation} for entity type {EntityType}",
                            operation.Operation, typeof(TEntity).Name);
                        
                        if (operation.RetryCount < MaxQueueRetries)
                        {
                            // Re-enqueue with incremented retry count
                            operationQueue.Enqueue((operation.Entity, operation.Operation, operation.RetryCount + 1));
                            logger.LogWarning(
                                "Concurrency conflict for entity type {EntityType} with ID {Id}, re-queuing. Retry count: {RetryCount}",
                                typeof(TEntity).Name, operation.Entity.Id, operation.RetryCount + 1);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing operation {Operation} for entity type {EntityType}",
                            operation.Operation, typeof(TEntity).Name);
                        
                        if (operation.RetryCount < MaxQueueRetries)
                        {
                            // Re-enqueue with incremented retry count
                            operationQueue.Enqueue((operation.Entity, operation.Operation, operation.RetryCount + 1));
                            logger.LogWarning(
                                "Operation failed for entity type {EntityType} with ID {Id}, re-queuing. Retry count: {RetryCount}",
                                typeof(TEntity).Name, operation.Entity.Id, operation.RetryCount + 1);
                        }
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
        else
        {
            logger.LogWarning("Failed to acquire semaphore for processing queue of {EntityType}",
                typeof(TEntity).Name);
        }
    }

    private async Task EnqueueOperation(TEntity entity, OperationType operation, CancellationToken cancellationToken)
    {
        operationQueue.Enqueue((entity, operation, 0));
        await ProcessQueueAsync(cancellationToken);
    }

    public override async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ProcessQueueAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving changes for entity type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public override async Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnqueueOperation(entity, OperationType.Save, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving entity of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public override async Task UpsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnqueueOperation(entity, OperationType.Update, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating entity of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public override async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                await EnqueueOperation(entity, OperationType.Delete, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting entity of type {EntityType} with ID {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    public override async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await SaveAsync(entity, cancellationToken);
    }

    public override void Update(TEntity entity)
    {
        // Queue the update operation
        operationQueue.Enqueue((entity, OperationType.Update, 0));
    }
}