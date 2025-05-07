using System.Threading.Tasks;
using System.Collections.Generic;

public interface IHolidayRepository : IDomainRepository<Holiday>
{
    Task<List<Holiday>> GetHolidaysByAgencyAsync(Guid agencyId);
}
