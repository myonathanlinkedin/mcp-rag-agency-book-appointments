using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract class UnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    protected readonly TContext dbContext;
    protected readonly ILogger logger;
    private bool disposed;

    protected UnitOfWork(TContext dbContext, ILogger logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public virtual async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
            logger.LogInformation("Transaction started");
        });
    }

    public virtual async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.CommitTransactionAsync(cancellationToken);
        logger.LogInformation("Transaction committed");
    }

    public virtual async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.RollbackTransactionAsync(cancellationToken);
        logger.LogWarning("Transaction rolled back");
    }

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed && disposing)
        {
            dbContext.Dispose();
        }
        disposed = true;
    }
} 