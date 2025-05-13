using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

public interface IHolidayRepository : IDomainRepository<Holiday>
{
    Task<List<Holiday>> GetHolidaysByAgencyAsync(Guid agencyId);
    Task DeleteForAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default);
    Task AddAsync(Holiday holiday, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
