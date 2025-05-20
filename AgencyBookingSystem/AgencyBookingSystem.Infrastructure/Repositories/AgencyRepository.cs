using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

internal class AgencyRepository : BufferedDataRepository<AgencyBookingDbContext, Agency>, IAgencyRepository
{
    private readonly ILogger<AgencyRepository> _logger;

    public AgencyRepository(
        AgencyBookingDbContext db,
        ILogger<AgencyRepository> logger)
        : base(db, logger)
    {
        _logger = logger;
    }

    public async Task<List<Agency>> GetAgenciesWithUsersAsync()
    {
        try
        {
            return await Data.Agencies
                .Include(a => a.AgencyUsers)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agencies with users");
            throw;
        }
    }

    public async Task<Agency?> GetByEmailAsync(string email)
    {
        try
        {
            return await Data.Agencies
                .Include(a => a.AgencyUsers)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Email == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agency by email {Email}", email);
            throw;
        }
    }

    public async Task<List<Agency>> GetApprovedAgenciesAsync()
    {
        try
        {
            return await Data.Agencies
                .Where(a => a.IsApproved)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving approved agencies");
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid agencyId)
    {
        try
        {
            return await Data.Agencies
                .AnyAsync(a => a.Id == agencyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if agency exists {AgencyId}", agencyId);
            throw;
        }
    }
}
