public interface IBaseService<TEntity>
{
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<List<TEntity>> GetAllAsync();
    Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default);
}
