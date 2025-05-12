using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

public abstract class DataRepository<TDbContext, TEntity> : IDomainRepository<TEntity>
    where TDbContext : DbContext
    where TEntity : Entity, IAggregateRoot
{
    protected readonly TDbContext Data;

    protected DataRepository(TDbContext db) => Data = db;

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
        });

    public async Task<TEntity?> GetByIdAsync(Guid id) =>
        await TryExecuteAsync(() => All().FirstOrDefaultAsync(e => e.Id == id));

    public async Task<TEntity?> GetByIdWithIncludesAsync(Guid id, params Expression<Func<TEntity, object>>[] includes) =>
        await TryExecuteAsync(() => WithIncludes(includes).FirstOrDefaultAsync(e => e.Id == id));

    public async Task<TEntity?> GetByIdWithAllIncludesAsync(Guid id) =>
        await TryExecuteAsync(() => WithAllIncludes().FirstOrDefaultAsync(e => e.Id == id));

    public async Task<List<TEntity>> GetAllAsync() =>
        await TryExecuteAsync(() => All().ToListAsync());

    public async Task<List<TEntity>> GetAllWithIncludesAsync(params Expression<Func<TEntity, object>>[] includes) =>
        await TryExecuteAsync(() => WithIncludes(includes).ToListAsync());

    public async Task<List<TEntity>> GetAllWithAllIncludesAsync() =>
        await TryExecuteAsync(() => WithAllIncludes().ToListAsync());

    public async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate) =>
        await TryExecuteAsync(() => All().Where(predicate).ToListAsync());

    public async Task<List<TEntity>> FindWithIncludesAsync(Expression<Func<TEntity, bool>> predicate, params Expression<Func<TEntity, object>>[] includes) =>
        await TryExecuteAsync(() => WithIncludes(includes).Where(predicate).ToListAsync());

    public async Task<List<TEntity>> FindWithAllIncludesAsync(Expression<Func<TEntity, bool>> predicate) =>
        await TryExecuteAsync(() => WithAllIncludes().Where(predicate).ToListAsync());

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await TryExecuteAsync(async () =>
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                Data.Remove(entity);
                await Data.SaveChangesAsync(cancellationToken);
            }
        });

    protected IQueryable<TEntity> WithIncludes(params Expression<Func<TEntity, object>>[] includes)
    {
        var query = All();
        foreach (var include in includes)
            query = query.Include(include);
        return query;
    }

    protected IQueryable<TEntity> WithAllIncludes()
    {
        var query = All();
        var navProps = Data.Model.FindEntityType(typeof(TEntity))?.GetNavigations();

        if (navProps != null)
        {
            foreach (var nav in navProps)
            {
                query = query.Include(nav.Name);
            }
        }

        return query;
    }

    private async Task<T> TryExecuteAsync<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch { throw; }
    }

    private async Task TryExecuteAsync(Func<Task> action)
    {
        try { await action(); }
        catch { throw; }
    }
}
