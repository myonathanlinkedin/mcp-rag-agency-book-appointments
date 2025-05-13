using Common.Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

public abstract class BufferedDataRepository<TDbContext, TEntity> : DataRepository<TDbContext, TEntity>
    where TDbContext : DbContext
    where TEntity : Entity, IAggregateRoot
{
    private static readonly ConcurrentDictionary<Type, SemaphoreSlim> entityLocks = new();
    private readonly SemaphoreSlim semaphore;
    private readonly ConcurrentQueue<(TEntity Entity, OperationType Operation)> operationQueue;
    private readonly ILogger logger;
    private const int MaxRetries = 3;

    protected BufferedDataRepository(
        TDbContext db,
        ILogger<BufferedDataRepository<TDbContext, TEntity>> logger) : base(db)
    {
        this.logger = logger;
        operationQueue = new ConcurrentQueue<(TEntity, OperationType)>();
        semaphore = entityLocks.GetOrAdd(typeof(TEntity), _ => new SemaphoreSlim(1, 1));
    }

    private async Task<TEntity> ReloadEntityAsync(TEntity entity, CancellationToken cancellationToken)
    {
        // Detach current entity to avoid tracking conflicts
        Data.Entry(entity).State = EntityState.Detached;

        // Reload the entity from database
        var reloadedEntity = await Data.Set<TEntity>()
            .FirstOrDefaultAsync(e => e.Id == entity.Id, cancellationToken);

        if (reloadedEntity == null)
        {
            throw new DbUpdateConcurrencyException(
                $"Entity of type {typeof(TEntity).Name} with ID {entity.Id} was not found during concurrency resolution.");
        }

        return reloadedEntity;
    }

    private async Task<bool> TryProcessOperationWithRetriesAsync(TEntity entity, OperationType operation, CancellationToken cancellationToken)
    {
        var attempts = 0;
        var delay = TimeSpan.FromMilliseconds(100); // Start with 100ms delay

        while (attempts < MaxRetries)
        {
            try
            {
                var currentEntity = attempts == 0 ? entity : await ReloadEntityAsync(entity, cancellationToken);

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

                await Data.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
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

                await Task.Delay(delay, cancellationToken);
                delay *= 2; // Exponential backoff
            }
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

    private async Task ProcessOperationAsync(TEntity entity, OperationType operation, CancellationToken cancellationToken)
    {
        try
        {
            var success = await TryProcessOperationWithRetriesAsync(entity, operation, cancellationToken);
            if (!success)
            {
                throw new DbUpdateConcurrencyException(
                    $"Failed to process operation {operation} for entity type {typeof(TEntity).Name} after maximum retries.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing {Operation} on entity type {EntityType}", 
                operation, typeof(TEntity).Name);
            throw;
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        if (await semaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            try
            {
                while (operationQueue.TryDequeue(out var operation))
                {
                    try
                    {
                        await ProcessOperationAsync(operation.Entity, operation.Operation, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing operation {Operation} for entity type {EntityType}", 
                            operation.Operation, typeof(TEntity).Name);
                        operationQueue.Enqueue(operation);
                        throw;
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
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
            logger.LogError(ex, "Error deleting entity of type {EntityType} with id {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    private async Task EnqueueOperation(TEntity entity, OperationType operation, CancellationToken cancellationToken)
    {
        operationQueue.Enqueue((entity, operation));
        await ProcessQueueAsync(cancellationToken);
    }

    public override async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ProcessQueueAsync(cancellationToken);
            await base.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving changes for entity type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }
}
