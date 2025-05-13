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
    {
        Expression<Func<Agency, object>> includeUsers = a => a.AgencyUsers;
        return await GetAllWithIncludesAsync(includeUsers);
    }

    public async Task<Agency?> GetByEmailAsync(string email)
    {
        Expression<Func<Agency, bool>> predicate = a => a.Email == email;
        Expression<Func<Agency, object>> includeUsers = a => a.AgencyUsers;
        var agencies = await FindWithIncludesAsync(predicate, includeUsers);
        return agencies.FirstOrDefault();
    }

    public async Task<List<Agency>> GetApprovedAgenciesAsync()
    {
        Expression<Func<Agency, bool>> predicate = a => !a.RequiresApproval;
        Expression<Func<Agency, object>> includeUsers = a => a.AgencyUsers;
        return await FindWithIncludesAsync(predicate, includeUsers);
    }

    public async Task<bool> ExistsAsync(Guid agencyId)
    {
        Expression<Func<Agency, bool>> predicate = a => a.Id == agencyId;
        var agencies = await FindAsync(predicate);
        return agencies.Any();
    }
}
