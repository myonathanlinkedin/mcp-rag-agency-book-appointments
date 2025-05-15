using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Transactions;

public abstract class DataRepository<TDbContext, TEntity> : IDomainRepository<TEntity>
    where TDbContext : DbContext
    where TEntity : Entity, IAggregateRoot
{
    protected readonly TDbContext Data;
    protected readonly ILogger? Logger;

    protected DataRepository(TDbContext db, ILogger? logger = null)
    {
        Data = db;
        Logger = logger;
    }

    protected virtual IQueryable<TEntity> All() => Data.Set<TEntity>();

    protected virtual IQueryable<TEntity> AllAsNoTracking() => All().AsNoTracking();

    public virtual async Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await TransactionHelper.ExecuteInTransactionAsync(async () =>
            {
                if (Data.Entry(entity).State == EntityState.Detached)
                    await Data.Set<TEntity>().AddAsync(entity, cancellationToken);
                else
                    Data.Update(entity);

                await Data.SaveChangesAsync(cancellationToken);
            }, IsolationLevel.RepeatableRead);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Logger?.LogError(ex, "Concurrency conflict detected while saving entity of type {EntityType} with ID {Id}",
                typeof(TEntity).Name, entity.Id);
            throw;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error saving entity of type {EntityType} with ID {Id}",
                typeof(TEntity).Name, entity.Id);
            throw;
        }
    }

    public virtual async Task UpsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await TransactionHelper.ExecuteInTransactionAsync(async () =>
            {
                if (Data.Entry(entity).State == EntityState.Detached)
                {
                    // Check if entity exists
                    var existingEntity = await All().FirstOrDefaultAsync(x => x.Id == entity.Id, cancellationToken);
                    if (existingEntity != null)
                    {
                        // Detach the entity loaded from DB
                        Data.Entry(existingEntity).State = EntityState.Detached;
                        // Update with new values
                        Data.Update(entity);
                    }
                    else
                    {
                        await Data.Set<TEntity>().AddAsync(entity, cancellationToken);
                    }
                }
                else
                {
                    Data.Update(entity);
                }

                await Data.SaveChangesAsync(cancellationToken);
            }, IsolationLevel.RepeatableRead);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Logger?.LogError(ex, "Concurrency conflict detected while upserting entity of type {EntityType} with ID {Id}",
                typeof(TEntity).Name, entity.Id);
            throw;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error upserting entity of type {EntityType} with ID {Id}",
                typeof(TEntity).Name, entity.Id);
            throw;
        }
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id)
    {
        try
        {
            return await All().FirstOrDefaultAsync(e => e.Id == id);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error retrieving entity of type {EntityType} with ID {Id}",
                typeof(TEntity).Name, id);
            throw;
        }
    }

    public virtual async Task<TEntity?> GetByIdWithIncludesAsync(Guid id, params Expression<Func<TEntity, object>>[] includes)
    {
        try
        {
            return await WithIncludes(includes).FirstOrDefaultAsync(e => e.Id == id);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error retrieving entity with includes of type {EntityType} with ID {Id}",
                typeof(TEntity).Name, id);
            throw;
        }
    }

    public virtual async Task<TEntity?> GetByIdWithAllIncludesAsync(Guid id)
    {
        try
        {
            return await WithAllIncludes().FirstOrDefaultAsync(e => e.Id == id);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error retrieving entity with all includes of type {EntityType} with ID {Id}",
                typeof(TEntity).Name, id);
            throw;
        }
    }

    public virtual async Task<List<TEntity>> GetAllAsync()
    {
        try
        {
            return await All().ToListAsync();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error retrieving all entities of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task<List<TEntity>> GetAllWithIncludesAsync(params Expression<Func<TEntity, object>>[] includes)
    {
        try
        {
            return await WithIncludes(includes).ToListAsync();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error retrieving all entities with includes of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task<List<TEntity>> GetAllWithAllIncludesAsync()
    {
        try
        {
            return await WithAllIncludes().ToListAsync();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error retrieving all entities with all includes of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            return await All().Where(predicate).ToListAsync();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error finding entities of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task<List<TEntity>> FindWithIncludesAsync(Expression<Func<TEntity, bool>> predicate, params Expression<Func<TEntity, object>>[] includes)
    {
        try
        {
            return await WithIncludes(includes).Where(predicate).ToListAsync();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error finding entities with includes of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task<List<TEntity>> FindWithAllIncludesAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            return await WithAllIncludes().Where(predicate).ToListAsync();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error finding entities with all includes of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await TransactionHelper.ExecuteInTransactionAsync(async () =>
            {
                var entity = await GetByIdAsync(id);
                if (entity != null)
                {
                    Data.Remove(entity);
                    await Data.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    Logger?.LogInformation("Entity of type {EntityType} with ID {Id} not found for deletion, skipping",
                        typeof(TEntity).Name, id);
                }
            }, IsolationLevel.RepeatableRead);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Logger?.LogError(ex, "Concurrency conflict detected while deleting entity of type {EntityType} with ID {Id}",
                typeof(TEntity).Name, id);
            throw;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error deleting entity of type {EntityType} with ID {Id}",
                typeof(TEntity).Name, id);
            throw;
        }
    }

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await TransactionHelper.ExecuteInTransactionAsync(async () =>
            {
                await Data.Set<TEntity>().AddAsync(entity, cancellationToken);
            }, IsolationLevel.RepeatableRead);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error adding entity of type {EntityType}",
                typeof(TEntity).Name);
            throw;
        }
    }

    public virtual void Update(TEntity entity)
    {
        try
        {
            TransactionHelper.ExecuteInTransactionAsync(async () =>
            {
                Data.Update(entity);
                await Task.CompletedTask;
            }, IsolationLevel.RepeatableRead).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error updating entity of type {EntityType} with ID {Id}",
                typeof(TEntity).Name, entity.Id);
            throw;
        }
    }

    public virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TransactionHelper.ExecuteInTransactionAsync(async () =>
            {
                await Data.SaveChangesAsync(cancellationToken);
            }, IsolationLevel.RepeatableRead);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Logger?.LogError(ex, "Concurrency conflict detected while saving changes for entity type {EntityType}",
                typeof(TEntity).Name);
            throw;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error saving changes for entity type {EntityType}",
                typeof(TEntity).Name);
            throw;
        }
    }

    protected virtual IQueryable<TEntity> WithIncludes(params Expression<Func<TEntity, object>>[] includes)
    {
        var query = All();
        foreach (var include in includes)
            query = query.Include(include);
        return query;
    }

    protected virtual IQueryable<TEntity> WithAllIncludes()
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
}