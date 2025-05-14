using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

internal class AgencyUserRepository : BufferedDataRepository<AgencyBookingDbContext, AgencyUser>, IAgencyUserRepository
{
    private readonly ILogger<AgencyUserRepository> logger;

    public AgencyUserRepository(AgencyBookingDbContext db, ILogger<AgencyUserRepository> logger) : base(db, logger)
    {
        this.logger = logger;
    }

    public async Task<AgencyUser?> GetByEmailAsync(string email)
    {
        Expression<Func<AgencyUser, bool>> predicate = u => u.Email == email;
        var users = await FindAsync(predicate);
        return users.FirstOrDefault();
    }
}
