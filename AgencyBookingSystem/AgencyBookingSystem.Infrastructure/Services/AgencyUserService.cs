using Microsoft.Extensions.Logging;

public class AgencyUserService : IAgencyUserService
{
    private readonly IAgencyUserRepository agencyUserRepository;
    private readonly ILogger<AgencyUserService> logger;

    public AgencyUserService(
        IAgencyUserRepository agencyUserRepository,
        ILogger<AgencyUserService> logger)
    {
        this.agencyUserRepository = agencyUserRepository;
        this.logger = logger;
    }

    public async Task<AgencyUser?> GetByIdAsync(Guid id)
    {
        logger.LogInformation("Fetching agency user with ID: {Id}", id);
        return await agencyUserRepository.GetByIdAsync(id);
    }

    public async Task<List<AgencyUser>> GetAllAsync()
    {
        logger.LogInformation("Fetching all agency users");
        return await agencyUserRepository.GetAllAsync();
    }

    public async Task UpsertAsync(AgencyUser entity, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving agency user: {Email}", entity.Email);
        await agencyUserRepository.UpsertAsync(entity, cancellationToken);
    }

    public async Task<AgencyUser?> GetByEmailAsync(string email)
    {
        logger.LogInformation("Fetching agency user by email: {Email}", email);
        return await agencyUserRepository.GetByEmailAsync(email);
    }

    public async Task AddAsync(AgencyUser entity, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding new agency user: {Email}", entity.Email);
        await agencyUserRepository.AddAsync(entity, cancellationToken);
    }

    public void Update(AgencyUser entity)
    {
        logger.LogInformation("Updating agency user: {Email}", entity.Email);
        agencyUserRepository.Update(entity);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving changes for agency users");
        await agencyUserRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result> UpdateUserDetailsAsync(
        Guid userId,
        string fullName,
        List<string> roles,
        CancellationToken cancellationToken = default)
    {
        var user = await agencyUserRepository.GetByIdAsync(userId);
        if (user == null)
        {
            logger.LogWarning("Update failed. User {UserId} not found.", userId);
            return Result.Failure(new[] { "User not found." });
        }

        var updateResult = user.UpdateDetails(fullName, roles);
        if (!updateResult.Succeeded)
        {
            return updateResult;
        }

        await agencyUserRepository.UpsertAsync(user, cancellationToken);
        logger.LogInformation("User {UserId} details updated successfully.", userId);

        return Result.Success;
    }

    public async Task<Result> AddUserRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var user = await agencyUserRepository.GetByIdAsync(userId);
        if (user == null)
        {
            logger.LogWarning("Role addition failed. User {UserId} not found.", userId);
            return Result.Failure(new[] { "User not found." });
        }

        var addRoleResult = user.AddRoles(new List<string> { role });
        if (!addRoleResult.Succeeded)
        {
            return addRoleResult;
        }

        await agencyUserRepository.UpsertAsync(user, cancellationToken);
        logger.LogInformation("Role {Role} added to user {UserId} successfully.", role, userId);

        return Result.Success;
    }

    public async Task<Result> RemoveUserRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var user = await agencyUserRepository.GetByIdAsync(userId);
        if (user == null)
        {
            logger.LogWarning("Role removal failed. User {UserId} not found.", userId);
            return Result.Failure(new[] { "User not found." });
        }

        var removeRoleResult = user.RemoveRole(role);
        if (!removeRoleResult.Succeeded)
        {
            return removeRoleResult;
        }

        await agencyUserRepository.UpsertAsync(user, cancellationToken);
        logger.LogInformation("Role {Role} removed from user {UserId} successfully.", role, userId);

        return Result.Success;
    }
}
