using System.Threading;
using System.Threading.Tasks;
 
public interface IRAGUnitOfWork : IUnitOfWork
{
    IJobStatusRepository JobStatuses { get; }
} 