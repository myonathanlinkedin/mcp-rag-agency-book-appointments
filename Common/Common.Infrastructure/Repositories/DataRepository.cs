using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

public abstract class DataRepository<TDbContext, TEntity> : IDomainRepository<TEntity>
    where TDbContext : DbContext
    where TEntity : Entity, IAggregateRoot
{
    public DataRepository(TDbContext db) => Data = db;

    protected TDbContext Data { get; }

    protected IQueryable<TEntity> All() => Data.Set<TEntity>();

    protected IQueryable<TEntity> AllAsNoTracking() => All().AsNoTracking();

    public async Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await UpsertAsync(entity, cancellationToken);

    public async Task UpsertAsync(TEntity entity, CancellationToken cancellationToken) =>
        await TryExecuteAsync(async () =>
        {
            if (Data.Entry(entity).State == EntityState.Detached)
                await Data.Set<TEntity>().AddAsync(entity, cancellationToken);
            else
                Data.Update(entity);

            await Data.SaveChangesAsync(cancellationToken);
        }, nameof(UpsertAsync));

    public async Task<TEntity?> GetByIdAsync(Guid id) =>
        await TryExecuteAsync(() => All().FirstOrDefaultAsync(e => e.Id == id), nameof(GetByIdAsync));

    public async Task<List<TEntity>> GetAllAsync() =>
        await TryExecuteAsync(() => All().ToListAsync(), nameof(GetAllAsync));

    public async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate) =>
        await TryExecuteAsync(() => All().Where(predicate).ToListAsync(), nameof(FindAsync));

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await TryExecuteAsync(async () =>
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                Data.Remove(entity);
                await Data.SaveChangesAsync(cancellationToken);
            }
        }, nameof(DeleteAsync));

    private async Task<T> TryExecuteAsync<T>(Func<Task<T>> action, string methodName) => await ExecuteAsync(action);

    private async Task TryExecuteAsync(Func<Task> action, string methodName) => await ExecuteAsync(action);

    private async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch
        {
            throw;
        }
    }

    private async Task ExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch
        {
            throw;
        }
    }
}
