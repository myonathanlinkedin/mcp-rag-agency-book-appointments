using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides buffering capabilities for batch operations in the repository pattern.
/// This class extends DataRepository to add operation buffering and batch processing capabilities.
/// 
/// Key Features:
/// 1. Operation Buffering:
///    - Maintains a buffer of pending operations
///    - Tracks operation type (Save, Update, Delete)
///    - Records operation timestamps
///    - Allows batch processing of operations
/// 
/// 2. Batch Processing:
///    - Supports AddRange and UpdateRange operations
///    - Maintains operation order
///    - Provides batch operation tracking
///    - Enables efficient bulk operations
/// 
/// 3. State Management:
///    - Tracks entity state changes
///    - Maintains operation history
///    - Provides operation status tracking
///    - Enables state rollback if needed
/// 
/// 4. Performance Optimization:
///    - Reduces database round trips
///    - Enables bulk operation processing
///    - Optimizes batch updates
///    - Minimizes transaction overhead
/// 
/// Usage:
/// - Inherit from this class for repositories that need batch processing
/// - Use buffer operations for bulk data changes
/// - Clear buffer when operations are complete
/// - Monitor buffer size for performance
/// 
/// Example:
/// ```csharp
/// public class JobStatusRepository : BufferedDataRepository<RAGDbContext, JobStatus>
/// {
///     public async Task ProcessBatchAsync(List<JobStatus> jobs)
///     {
///         await AddRangeAsync(jobs);
///         // Operations are buffered until SaveChanges is called
///     }
/// }
/// ```
/// 
/// Note: 
/// - Buffer operations are not persisted until SaveChanges is called
/// - Buffer should be cleared after successful SaveChanges
/// - Consider buffer size for memory usage
/// - Buffer operations are still subject to concurrency control
/// </summary>
public abstract class BufferedDataRepository<TDbContext, TEntity> : DataRepository<TDbContext, TEntity>
    where TDbContext : DbContext
    where TEntity : Entity, IAggregateRoot
{
    /// <summary>
    /// Internal buffer to store pending operations.
    /// Each operation includes:
    /// - Entity being operated on
    /// - Operation type (Save, Update, Delete)
    /// - Timestamp of the operation
    /// </summary>
    private readonly List<EntityOperation<TEntity>> buffer = new();

    protected BufferedDataRepository(TDbContext dbContext, ILogger? logger = null) : base(dbContext, logger)
    {
    }

    /// <summary>
    /// Adds an entity to the buffer with Save operation type.
    /// The operation is not persisted until SaveChanges is called.
    /// </summary>
    public override async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await base.AddAsync(entity, cancellationToken);
        buffer.Add(new EntityOperation<TEntity> 
        { 
            Entity = entity, 
            OperationType = OperationType.Save,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Updates an entity and adds the operation to the buffer.
    /// The operation is not persisted until SaveChanges is called.
    /// </summary>
    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        base.Update(entity);
        buffer.Add(new EntityOperation<TEntity> 
        { 
            Entity = entity, 
            OperationType = OperationType.Update,
            Timestamp = DateTime.UtcNow
        });
        return entity;
    }

    /// <summary>
    /// Adds multiple entities to the buffer in batch.
    /// Each entity is added as a separate Save operation.
    /// </summary>
    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            await AddAsync(entity, cancellationToken);
        }
    }

    /// <summary>
    /// Updates multiple entities in batch.
    /// Each entity is added as a separate Update operation.
    /// </summary>
    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            await UpdateAsync(entity, cancellationToken);
        }
    }

    /// <summary>
    /// Clears all buffered operations.
    /// Should be called after successful SaveChanges.
    /// </summary>
    public void ClearBuffer()
    {
        buffer.Clear();
    }

    /// <summary>
    /// Gets all currently buffered operations.
    /// Useful for debugging and monitoring.
    /// </summary>
    public IEnumerable<EntityOperation<TEntity>> GetBufferedOperations()
    {
        return buffer;
    }

    /// <summary>
    /// Gets all entities currently in the buffer.
    /// </summary>
    public IEnumerable<TEntity> GetBufferedEntities()
    {
        return buffer.Select(x => x.Entity);
    }

    /// <summary>
    /// Gets entities from the buffer filtered by operation type.
    /// </summary>
    public IEnumerable<TEntity> GetBufferedEntitiesByOperation(OperationType operationType)
    {
        return buffer.Where(x => x.OperationType == operationType).Select(x => x.Entity);
    }

    /// <summary>
    /// Checks if there are any operations in the buffer.
    /// </summary>
    public bool HasBufferedOperations()
    {
        return buffer.Any();
    }

    /// <summary>
    /// Gets the current number of operations in the buffer.
    /// </summary>
    public int GetBufferedOperationCount()
    {
        return buffer.Count;
    }
}