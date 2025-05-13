using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

internal class AgencyUserRepository : DataRepository<AgencyBookingDbContext, AgencyUser>, IAgencyUserRepository
{
    private readonly ILogger<AgencyUserRepository> logger;

    public AgencyUserRepository(AgencyBookingDbContext db, ILogger<AgencyUserRepository> logger) : base(db)
    {
        this.logger = logger;
    }

    public async Task<AgencyUser?> GetByEmailAsync(string email)
    {
        Expression<Func<AgencyUser, bool>> predicate = u => u.Email == email;
        var users = await FindAsync(predicate);
        return users.FirstOrDefault();
    }

    public override async Task UpsertAsync(AgencyUser entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingEntity = await GetByIdAsync(entity.Id);
            if (existingEntity == null)
            {
                await Data.Set<AgencyUser>().AddAsync(entity, cancellationToken);
            }
            else
            {
                Data.Entry(existingEntity).CurrentValues.SetValues(entity);
            }

            await Data.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict detected while updating AgencyUser {Id}. Retrying operation.", entity.Id);

            // Get the current values in the database
            var entry = ex.Entries.Single();
            var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);

            if (databaseValues == null)
            {
                logger.LogError("AgencyUser {Id} was deleted by another process.", entity.Id);
                throw new InvalidOperationException($"AgencyUser {entity.Id} was deleted by another process.");
            }

            throw; // Let the caller handle the concurrency conflict
        }
    }
}
