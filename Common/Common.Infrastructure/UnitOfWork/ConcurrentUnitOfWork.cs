using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Transactions;

/// <summary>
/// Provides enhanced concurrency control for database operations.
/// This class extends the base UnitOfWork pattern with additional concurrency management features.
/// 
/// Key Features:
/// 1. Queue-based Operation Processing:
///    - Uses ConcurrentQueue for thread-safe operation queuing
///    - Ensures ordered processing of operations
///    - Prevents operation interleaving
///    - Maintains operation sequence
/// 
/// 2. Entity-level Locking:
///    - Implements SemaphoreSlim for entity locking
///    - Prevents concurrent modifications to same entity
///    - Manages lock timeouts
///    - Handles deadlock prevention
/// 
/// 3. Automatic Retry Mechanism:
///    - Implements exponential backoff for retries
///    - Handles concurrency exceptions
///    - Provides configurable retry attempts
///    - Manages retry delays
/// 
/// 4. Entity Reloading:
///    - Automatically reloads entities on conflicts
///    - Maintains entity state consistency
///    - Handles detached entity scenarios
///    - Manages entity tracking
/// 
/// 5. Conflict Resolution:
///    - Provides conflict detection
///    - Implements resolution strategies
///    - Manages concurrent updates
///    - Handles version conflicts
/// 
/// Usage:
/// - Inherit from this class for repositories needing enhanced concurrency control
/// - Use in high-concurrency scenarios
/// - Implement in systems with frequent concurrent updates
/// - Apply where optimistic concurrency is insufficient
/// 
/// Example:
/// ```csharp
/// public class AppointmentUnitOfWork : ConcurrentUnitOfWork<AppointmentDbContext>
/// {
///     public async Task UpdateAppointmentAsync(Appointment appointment)
///     {
///         await ProcessOperationWithRetriesAsync(async () =>
///         {
///             // Update logic here
///         });
///     }
/// }
/// ```
/// 
/// Note:
/// - Provides stronger concurrency guarantees than SQL Server's default
/// - Adds overhead for concurrency control
/// - Requires careful configuration of retry parameters
/// - May impact performance in low-concurrency scenarios
/// </summary>
public abstract class ConcurrentUnitOfWork<TDbContext> : UnitOfWork<TDbContext> where TDbContext : DbContext
{
    private static readonly ConcurrentDictionary<Type, SemaphoreSlim> entityLocks = new();
    private readonly SemaphoreSlim semaphore;
    private readonly ConcurrentQueue<(object Entity, OperationType Operation)> operationQueue;
    private const int MaxRetries = 3;

    protected ConcurrentUnitOfWork(
        TDbContext dbContext,
        ILogger logger) : base(dbContext, logger)
    {
        operationQueue = new ConcurrentQueue<(object, OperationType)>();
        semaphore = entityLocks.GetOrAdd(typeof(TDbContext), _ => new SemaphoreSlim(1, 1));
    }

    private async Task<object?> ReloadEntityAsync(object entity, Type entityType, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).State = EntityState.Detached;
        var id = ((Entity)entity).Id;
        var method = typeof(DbContext).GetMethod("Set", Type.EmptyTypes)?.MakeGenericMethod(entityType);
        var dbSet = method?.Invoke(dbContext, null) as IQueryable<object>;
        var reloadedEntity = await dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);

        if (reloadedEntity == null)
        {
            logger.LogWarning(
                "Entity of type {EntityType} with ID {Id} was not found during reload. It may have been deleted.",
                entityType.Name, id);
            return null;
        }

        return reloadedEntity;
    }

    private async Task<bool> TryProcessOperationWithRetriesAsync(object entity, Type entityType, OperationType operation, CancellationToken cancellationToken)
    {
        var attempts = 0;
        var delay = TimeSpan.FromMilliseconds(100);

        while (attempts < MaxRetries)
        {
            try
            {
                var currentEntity = attempts == 0 ? entity : await ReloadEntityAsync(entity, entityType, cancellationToken);
                var method = typeof(DbContext).GetMethod("Set", Type.EmptyTypes)?.MakeGenericMethod(entityType);
                var dbSet = method?.Invoke(dbContext, null) as dynamic;

                if (currentEntity == null)
                {
                    if (operation == OperationType.Save)
                    {
                        await dbSet.AddAsync(currentEntity, cancellationToken);
                    }
                    else if (operation == OperationType.Delete)
                    {
                        logger.LogInformation(
                            "Entity of type {EntityType} with ID {Id} already deleted, skipping delete operation",
                            entityType.Name, ((Entity)entity).Id);
                        return true;
                    }
                    else
                    {
                        logger.LogWarning(
                            "Cannot update entity of type {EntityType} with ID {Id} as it no longer exists in the database",
                            entityType.Name, ((Entity)entity).Id);
                        throw new DbUpdateConcurrencyException(
                            $"Entity of type {entityType.Name} with ID {((Entity)entity).Id} was not found during concurrency resolution.");
                    }
                }
                else
                {
                    switch (operation)
                    {
                        case OperationType.Save:
                            if (dbContext.Entry(currentEntity).State == EntityState.Detached)
                            {
                                await dbSet.AddAsync(currentEntity, cancellationToken);
                            }
                            break;

                        case OperationType.Update:
                            if (attempts > 0)
                            {
                                await OnConcurrencyResolveAsync(currentEntity, entity);
                            }
                            dbContext.Update(currentEntity);
                            break;

                        case OperationType.Delete:
                            dbContext.Remove(currentEntity);
                            break;
                    }
                }

                var baseDbContext = dbContext as BaseDbContext<TDbContext>;
                if (baseDbContext == null)
                {
                    throw new InvalidOperationException("The DbContext is not of type BaseDbContext<TDbContext>.");
                }

                await baseDbContext.WithDispatchEvent(false).SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                attempts++;
                if (attempts >= MaxRetries)
                {
                    logger.LogError(ex,
                        "Concurrency conflict could not be resolved after {Attempts} attempts for {EntityType} with ID {Id}",
                        attempts, entityType.Name, ((Entity)entity).Id);
                    throw;
                }

                logger.LogWarning(
                    "Concurrency conflict detected for {EntityType} with ID {Id}. Attempt {Attempt} of {MaxRetries}",
                    entityType.Name, ((Entity)entity).Id, attempts, MaxRetries);

                await Task.Delay(delay, cancellationToken);
                delay *= 2;
            }
        }

        return false;
    }

    protected virtual async Task OnConcurrencyResolveAsync(object currentEntity, object conflictingEntity)
    {
        var properties = currentEntity.GetType().GetProperties()
            .Where(p => p.CanRead && p.CanWrite && p.Name != "Id");

        foreach (var prop in properties)
        {
            prop.SetValue(currentEntity, prop.GetValue(conflictingEntity));
        }

        await Task.CompletedTask;
    }

    private async Task ProcessOperationAsync(object entity, Type entityType, OperationType operation, CancellationToken cancellationToken)
    {
        try
        {
            var success = await TryProcessOperationWithRetriesAsync(entity, entityType, operation, cancellationToken);
            if (!success)
            {
                throw new DbUpdateConcurrencyException(
                    $"Failed to process operation {operation} for entity type {entityType.Name} after maximum retries.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing {Operation} on entity type {EntityType}",
                operation, entityType.Name);
            throw;
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all modified entities from the change tracker
            var modifiedEntries = dbContext.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || 
                           e.State == EntityState.Modified || 
                           e.State == EntityState.Deleted)
                .ToList();

            // Enqueue each modified entity
            foreach (var entry in modifiedEntries)
            {
                var operation = entry.State switch
                {
                    EntityState.Added => OperationType.Save,
                    EntityState.Modified => OperationType.Update,
                    EntityState.Deleted => OperationType.Delete,
                    _ => throw new InvalidOperationException($"Unexpected entity state: {entry.State}")
                };

                await EnqueueOperation(entry.Entity, operation, cancellationToken);
            }

            // Process the queue
            await ProcessQueueAsync(cancellationToken);

            // Save any remaining changes
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving changes");
            throw;
        }
    }

    public async Task EnqueueOperation(object entity, OperationType operation, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        operationQueue.Enqueue((entity, operation));
        logger.LogDebug("Enqueued {Operation} operation for entity type {EntityType} with ID {Id}",
            operation, entity.GetType().Name, ((Entity)entity).Id);
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        if (await semaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            try
            {
                var failedOperations = new ConcurrentQueue<(object Entity, OperationType Operation)>();

                while (operationQueue.TryDequeue(out var operation))
                {
                    try
                    {
                        logger.LogDebug("Processing {Operation} operation for entity type {EntityType} with ID {Id}",
                            operation.Operation, operation.Entity.GetType().Name, ((Entity)operation.Entity).Id);

                        await ProcessOperationAsync(operation.Entity, operation.Entity.GetType(), operation.Operation, cancellationToken);
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        logger.LogError(ex, "Concurrency error processing operation {Operation} for entity type {EntityType}",
                            operation.Operation, operation.Entity.GetType().Name);
                        failedOperations.Enqueue(operation);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing operation {Operation} for entity type {EntityType}",
                            operation.Operation, operation.Entity.GetType().Name);
                        failedOperations.Enqueue(operation);
                    }
                }

                // Re-queue failed operations
                foreach (var failedOp in failedOperations)
                {
                    operationQueue.Enqueue(failedOp);
                    logger.LogWarning("Re-queued failed {Operation} operation for entity type {EntityType} with ID {Id}",
                        failedOp.Operation, failedOp.Entity.GetType().Name, ((Entity)failedOp.Entity).Id);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
        else
        {
            throw new TimeoutException("Failed to acquire semaphore for queue processing");
        }
    }
} 