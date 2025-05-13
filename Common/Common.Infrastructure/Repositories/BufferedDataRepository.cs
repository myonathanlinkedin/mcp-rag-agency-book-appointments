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

    protected BufferedDataRepository(
        TDbContext db,
        ILogger<BufferedDataRepository<TDbContext, TEntity>> logger) : base(db)
    {
        this.logger = logger;
        operationQueue = new ConcurrentQueue<(TEntity, OperationType)>();
        semaphore = entityLocks.GetOrAdd(typeof(TEntity), _ => new SemaphoreSlim(1, 1));
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

    private async Task ProcessOperationAsync(TEntity entity, OperationType operation, CancellationToken cancellationToken)
    {
        try
        {
            switch (operation)
            {
                case OperationType.Save:
                    await Data.Set<TEntity>().AddAsync(entity, cancellationToken);
                    break;
                case OperationType.Update:
                    Data.Update(entity);
                    break;
                case OperationType.Delete:
                    Data.Remove(entity);
                    break;
            }

            await Data.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing {Operation} on entity type {EntityType}", 
                operation, typeof(TEntity).Name);
            throw;
        }
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
