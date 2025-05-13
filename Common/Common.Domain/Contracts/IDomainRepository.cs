using System.Linq.Expressions;

public interface IDomainRepository<TEntity>
    where TEntity : IAggregateRoot
{
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<TEntity?> GetByIdWithIncludesAsync(Guid id, params Expression<Func<TEntity, object>>[] includes);
    Task<TEntity?> GetByIdWithAllIncludesAsync(Guid id);

    Task<List<TEntity>> GetAllAsync();
    Task<List<TEntity>> GetAllWithIncludesAsync(params Expression<Func<TEntity, object>>[] includes);
    Task<List<TEntity>> GetAllWithAllIncludesAsync();

    Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    Task<List<TEntity>> FindWithIncludesAsync(Expression<Func<TEntity, bool>> predicate, params Expression<Func<TEntity, object>>[] includes);
    Task<List<TEntity>> FindWithAllIncludesAsync(Expression<Func<TEntity, bool>> predicate);

    Task UpsertAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // New unit of work methods
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
