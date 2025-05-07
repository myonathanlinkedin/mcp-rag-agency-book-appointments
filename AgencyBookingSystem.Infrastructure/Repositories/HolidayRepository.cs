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
        => await All().Where(h => h.AgencyId == agencyId).ToListAsync();
}
