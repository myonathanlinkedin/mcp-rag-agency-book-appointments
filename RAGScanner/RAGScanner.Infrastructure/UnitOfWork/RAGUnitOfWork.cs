using Microsoft.Extensions.Logging;

public class RAGUnitOfWork : UnitOfWork<RAGDbContext>, IRAGUnitOfWork
{
    private readonly IJobStatusRepository jobStatuses;

    public RAGUnitOfWork(
        RAGDbContext dbContext,
        ILogger<RAGUnitOfWork> logger,
        IJobStatusRepository jobStatuses) : base(dbContext, logger)
    {
        this.jobStatuses = jobStatuses;
    }

    public IJobStatusRepository JobStatuses => jobStatuses;

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
} 