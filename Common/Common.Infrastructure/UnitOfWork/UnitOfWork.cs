using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

public abstract class UnitOfWork<TDbContext> : IUnitOfWork where TDbContext : DbContext
{
    protected readonly TDbContext dbContext;
    protected readonly ILogger logger;

    protected UnitOfWork(TDbContext dbContext, ILogger logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving changes");
            throw;
        }
    }

    public virtual async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error beginning transaction");
            throw;
        }
    }

    public virtual async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var transaction = dbContext.Database.CurrentTransaction;
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error committing transaction");
            throw;
        }
    }

    public virtual async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var transaction = dbContext.Database.CurrentTransaction;
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rolling back transaction");
            throw;
        }
    }

    public void Dispose()
    {
        dbContext?.Dispose();
    }
} 