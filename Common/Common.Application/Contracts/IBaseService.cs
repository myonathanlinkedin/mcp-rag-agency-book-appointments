public interface IBaseService<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<List<TEntity>> GetAllAsync();
    Task UpsertAsync(TEntity entity, CancellationToken cancellationToken = default);
}
