using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

internal class AgencyUserRepository : DataRepository<AgencyBookingDbContext, AgencyUser>, IAgencyUserRepository
{
    public AgencyUserRepository(AgencyBookingDbContext db) : base(db) { }

    public async Task<AgencyUser?> GetByEmailAsync(string email)
        => await All().FirstOrDefaultAsync(u => u.Email == email);
}
