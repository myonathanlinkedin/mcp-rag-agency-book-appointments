using Microsoft.Extensions.Logging;

public class AgencyService : IAgencyService
{
    private readonly IAgencyRepository agencyRepository;
    private readonly IAgencyUserRepository agencyUserRepository;
    private readonly ILogger<AgencyService> logger;
    private readonly IEventDispatcher eventDispatcher;

    public AgencyService(IAgencyRepository agencyRepository, IAgencyUserRepository agencyUserRepository, IEventDispatcher eventDispatcher, ILogger<AgencyService> logger)
    {
        this.agencyRepository = agencyRepository;
        this.agencyUserRepository = agencyUserRepository;
        this.eventDispatcher = eventDispatcher;
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
        await agencyRepository.UpsertAsync(entity, cancellationToken);
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

    public async Task<string?> GetUserEmailAsync(Guid userId)
    {
        var user = await agencyUserRepository.GetByIdAsync(userId);
        return user?.Email;
    }

    public async Task<Result> AssignUserToAgencyAsync(Guid agencyId, string email, string fullName, List<string> roles, CancellationToken cancellationToken = default)
    {
        var agency = await agencyRepository.GetByIdAsync(agencyId);
        if (agency == null)
        {
            logger.LogWarning("Failed to assign user. Agency {AgencyId} does not exist.", agencyId);
            return Result.Failure(new[] { "Agency does not exist." });
        }

        var existingUser = await agencyUserRepository.GetByEmailAsync(email);
        if (existingUser != null)
        {
            logger.LogWarning("User {Email} is already assigned to an agency.", email);
            return Result.Failure(new[] { "User is already assigned to an agency." });
        }

        var agencyUser = new AgencyUser
        {
            Id = Guid.NewGuid(),
            AgencyId = agencyId,
            Email = email,
            FullName = fullName,
            Roles = roles
        };

        await agencyUserRepository.UpsertAsync(agencyUser, cancellationToken);

        // Dispatch event instead of direct notification
        await eventDispatcher.Dispatch(new AgencyUserAssignedEvent(
            agencyUser.Id,
            agencyId,
            email,
            fullName,
            roles
        ), cancellationToken);

        logger.LogInformation("User '{FullName}' ({Email}) successfully assigned to agency {AgencyName}.", fullName, email, agency.Name);

        return Result.Success;
    }

    public async Task<Result> RegisterAgencyAsync(string name, string email, bool requiresApproval, int maxAppointmentsPerDay, CancellationToken cancellationToken = default)
    {
        if (await agencyRepository.GetByEmailAsync(email) != null)
        {
            logger.LogWarning("Agency registration failed. Email {Email} is already in use.", email);
            return Result.Failure(new[] { "An agency with this email already exists." });
        }

        var agency = new Agency
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            RequiresApproval = requiresApproval,
            MaxAppointmentsPerDay = maxAppointmentsPerDay,
            IsApproved = !requiresApproval // Auto-approve if `RequiresApproval = false`
        };

        await agencyRepository.UpsertAsync(agency, cancellationToken);

        // Dispatch event instead of direct notification
        await eventDispatcher.Dispatch(new AgencyRegisteredEvent(
            agency.Id,
            agency.Name,
            agency.Email,
            agency.RequiresApproval
        ), cancellationToken);

        logger.LogInformation("Agency '{AgencyName}' registered successfully. Approval required: {RequiresApproval}.", agency.Name, agency.RequiresApproval);

        return Result.Success;
    }

    public async Task<Result> ApproveAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        var agency = await agencyRepository.GetByIdAsync(agencyId);
        if (agency == null || agency.IsApproved)
        {
            logger.LogWarning("Approval failed. Agency {AgencyId} does not exist or is already approved.", agencyId);
            return Result.Failure(new[] { "Agency already approved or does not exist." });
        }

        agency.IsApproved = true;
        await agencyRepository.UpsertAsync(agency, cancellationToken);

        // Dispatch event instead of direct notification
        await eventDispatcher.Dispatch(new AgencyUserAssignedEvent(
            agency.Id,
            agency.Name,
            agency.Email
        ), cancellationToken);

        logger.LogInformation("Agency '{AgencyName}' has been successfully approved.", agency.Name);

        return Result.Success;
    }

    public async Task<AgencyUser?> GetAgencyUserByEmailAsync(string email)
    {
        logger.LogInformation("Fetching agency user by email: {Email}", email);
        return await agencyUserRepository.GetByEmailAsync(email);
    }

}
