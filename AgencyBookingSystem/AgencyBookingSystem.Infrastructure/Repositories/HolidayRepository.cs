using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

internal class HolidayRepository : DataRepository<AgencyBookingDbContext, Holiday>, IHolidayRepository
{
    public HolidayRepository(AgencyBookingDbContext db) : base(db) { }

    public async Task<List<Holiday>> GetHolidaysByAgencyAsync(Guid agencyId)
    {
        Expression<Func<Holiday, bool>> predicate = h => h.AgencyId == agencyId;
        return await FindAsync(predicate);
    }

    public async Task DeleteForAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        var holidays = await Data.Set<Holiday>()
            .Where(h => h.AgencyId == agencyId)
            .ToListAsync(cancellationToken);

        if (holidays.Any())
        {
            Data.Set<Holiday>().RemoveRange(holidays);
            await Data.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AddAsync(Holiday holiday, CancellationToken cancellationToken = default)
    {
        await Data.Set<Holiday>().AddAsync(holiday, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Data.SaveChangesAsync(cancellationToken);
    }
}
