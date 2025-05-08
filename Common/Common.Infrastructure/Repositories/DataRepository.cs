using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public abstract class DataRepository<TDbContext, TEntity> : IDomainRepository<TEntity>
    where TDbContext : DbContext
    where TEntity : Entity, IAggregateRoot
{
    protected DataRepository(TDbContext db) => Data = db;

    protected TDbContext Data { get; }

    protected IQueryable<TEntity> All() => Data.Set<TEntity>();

    protected IQueryable<TEntity> AllAsNoTracking() => All().AsNoTracking();

    public async Task Save(TEntity entity, CancellationToken cancellationToken = default)
    {
        var existingEntity = await GetByIdAsync(entity.Id);

        if (existingEntity != null)
        {
            // Entity exists, so update it
            Data.Entry(existingEntity).CurrentValues.SetValues(entity);
        }
        else
        {
            // Entity doesn't exist, so insert a new one
            await Data.Set<TEntity>().AddAsync(entity, cancellationToken);
        }

        await Data.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id)
        => await All().FirstOrDefaultAsync(e => e.Id == id);

    public virtual async Task<List<TEntity>> GetAllAsync()
        => await All().ToListAsync();

    public virtual async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        => await All().Where(predicate).ToListAsync();

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            Data.Remove(entity);
            await Data.SaveChangesAsync(cancellationToken);
        }
    }
}
