using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class AgencyService : IAgencyService
{
    private readonly IAgencyRepository agencyRepository;
    private readonly IAgencyUserRepository agencyUserRepository;
    private readonly INotificationService notificationService;
    private readonly ILogger<AgencyService> logger;

    public AgencyService(IAgencyRepository agencyRepository, IAgencyUserRepository agencyUserRepository, INotificationService notificationService, ILogger<AgencyService> logger)
    {
        this.agencyRepository = agencyRepository;
        this.agencyUserRepository = agencyUserRepository;
        this.notificationService = notificationService;
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

    public async Task<string?> GetUserEmailAsync(Guid userId)
    {
        var user = await agencyUserRepository.GetByIdAsync(userId);
        return user?.Email;
    }

    public async Task<Result> AssignUserToAgencyAsync(Guid agencyId, string email, string fullName, List<string> roles, CancellationToken cancellationToken = default)
    {
        if (!await ExistsAsync(agencyId))
        {
            logger.LogWarning("Failed to assign user with email {Email} to agency {AgencyId}. Agency does not exist.", email, agencyId);
            return Result.Failure(new[] { "Agency does not exist." });
        }

        var agencyUser = new AgencyUser
        {
            Id = Guid.NewGuid(),
            AgencyId = agencyId,
            Email = email,
            FullName = fullName,
            Roles = roles
        };

        await agencyUserRepository.Save(agencyUser, cancellationToken);

        await notificationService.SendNotificationAsync(email, "Agency Assignment", $"You have been successfully assigned to agency ID {agencyId}.");

        logger.LogInformation("Successfully assigned user with email {Email} to agency {AgencyId}.", email, agencyId);
        return Result.Success;
    }

    public async Task<Result> RegisterAgencyAsync(string name, string email, bool requiresApproval, int maxAppointmentsPerDay, CancellationToken cancellationToken = default)
    {
        if (await GetByEmailAsync(email) != null)
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

        await SaveAsync(agency, cancellationToken);

        if (requiresApproval)
        {
            await notificationService.SendNotificationAsync("admin@example.com", "New Agency Approval Request", $"Agency {name} has requested registration.");
        }
        else
        {
            await notificationService.SendNotificationAsync(email, "Agency Approved", $"Your agency {name} has been successfully registered.");
        }

        logger.LogInformation("Agency {Name} registered successfully. Approval required: {RequiresApproval}", agency.Name, agency.RequiresApproval);
        return Result.Success;
    }

    public async Task<Result> ApproveAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        var agency = await GetByIdAsync(agencyId);
        if (agency == null || agency.IsApproved) return Result.Failure(new[] { "Agency already approved or does not exist." });

        agency.IsApproved = true;
        await SaveAsync(agency, cancellationToken);
        await notificationService.SendNotificationAsync(agency.Email, "Agency Approved", $"Your agency {agency.Name} has been approved.");

        logger.LogInformation("Agency {AgencyId} has been approved by an admin.", agencyId);
        return Result.Success;
    }
    public async Task<AgencyUser?> GetAgencyUserByEmailAsync(string email)
    {
        logger.LogInformation("Fetching agency user by email: {Email}", email);
        return await agencyUserRepository.GetByEmailAsync(email);
    }

}
