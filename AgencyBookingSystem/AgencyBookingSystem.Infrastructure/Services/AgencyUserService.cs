using Microsoft.Extensions.Logging;

public class AgencyUserService : IAgencyUserService
{
    private readonly IAgencyUserRepository agencyUserRepository;
    private readonly ILogger<AgencyUserService> logger;

    public AgencyUserService(IAgencyUserRepository agencyUserRepository, ILogger<AgencyUserService> logger)
    {
        this.agencyUserRepository = agencyUserRepository;
        this.logger = logger;
    }

    public async Task<AgencyUser?> GetByIdAsync(Guid id) => await agencyUserRepository.GetByIdAsync(id);
    public async Task<List<AgencyUser>> GetAllAsync() => await agencyUserRepository.GetAllAsync();
    public async Task SaveAsync(AgencyUser entity, CancellationToken cancellationToken = default) => await agencyUserRepository.Save(entity, cancellationToken);
    public async Task<AgencyUser?> GetByEmailAsync(string email) => await agencyUserRepository.GetByEmailAsync(email);

}
