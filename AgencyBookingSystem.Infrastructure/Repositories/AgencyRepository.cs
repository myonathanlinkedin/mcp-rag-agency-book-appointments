using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

internal class AgencyRepository : DataRepository<AgencyBookingDbContext, Agency>, IAgencyRepository
{
    public AgencyRepository(AgencyBookingDbContext db) : base(db) { }

    public async Task<List<Agency>> GetAgenciesWithUsersAsync()
        => await All().Include(a => a.AgencyUsers).ToListAsync();

    public async Task<Agency?> GetByEmailAsync(string email)
        => await All().FirstOrDefaultAsync(a => a.Email == email);

    public async Task<List<Agency>> GetApprovedAgenciesAsync()
        => await All().Where(a => !a.RequiresApproval).ToListAsync();

    public async Task<bool> ExistsAsync(Guid agencyId)
        => await All().AnyAsync(a => a.Id == agencyId);
}
