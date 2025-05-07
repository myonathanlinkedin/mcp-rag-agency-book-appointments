using Microsoft.Extensions.Logging;

public class AgencyService : IAgencyService
{
    private readonly IAgencyRepository agencyRepository;
    private readonly ILogger<AgencyService> logger;

    public AgencyService(IAgencyRepository agencyRepository, ILogger<AgencyService> logger)
    {
        this.agencyRepository = agencyRepository;
        this.logger = logger;
    }

    public async Task<Agency?> GetByIdAsync(Guid id)
    {
        logger.LogInformation("Fetching agency with ID: {Id}", id);
        return await agencyRepository.GetByIdAsync(id);
    }

    public async Task<List<Agency>> GetAllAsync()
    {
        logger.LogInformation("Fetching all agencies.");
        return await agencyRepository.GetAllAsync();
    }

    public async Task SaveAsync(Agency entity, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving agency: {AgencyName}", entity.Name);
        await agencyRepository.Save(entity, cancellationToken);
    }

    public async Task<List<Agency>> GetAgenciesWithUsersAsync()
    {
        logger.LogInformation("Fetching agencies with users.");
        return await agencyRepository.GetAgenciesWithUsersAsync();
    }

    public async Task<Agency?> GetByEmailAsync(string email)
    {
        logger.LogInformation("Fetching agency by email: {Email}", email);
        return await agencyRepository.GetByEmailAsync(email);
    }

    public async Task<List<Agency>> GetApprovedAgenciesAsync()
    {
        logger.LogInformation("Fetching all approved agencies.");
        return await agencyRepository.GetApprovedAgenciesAsync();
    }

    public async Task<bool> ExistsAsync(Guid agencyId)
    {
        logger.LogInformation("Checking existence of agency with ID: {AgencyId}", agencyId);
        return await agencyRepository.ExistsAsync(agencyId);
    }
}
