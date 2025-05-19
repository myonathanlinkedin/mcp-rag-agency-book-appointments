using System.Threading;
using System.Threading.Tasks;

public interface IUnitOfWork : IDisposable
{
    IJobStatusRepository JobStatuses { get; }
    
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
} 