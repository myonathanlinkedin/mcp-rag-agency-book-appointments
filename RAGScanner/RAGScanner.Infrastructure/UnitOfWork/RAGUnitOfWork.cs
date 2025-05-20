using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Transactions;

public class RAGUnitOfWork : ConcurrentUnitOfWork<RAGDbContext>, IRAGUnitOfWork
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
} 