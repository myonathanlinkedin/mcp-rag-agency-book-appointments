using Microsoft.Extensions.Logging;

public class AgencyUserService : IAgencyUserService
{
    private readonly IAppointmentUnitOfWork unitOfWork;
    private readonly ILogger<AgencyUserService> logger;

    public AgencyUserService(
        IAppointmentUnitOfWork unitOfWork,
        ILogger<AgencyUserService> logger)
    {
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<AgencyUser?> GetByIdAsync(Guid id)
    {
        logger.LogInformation("Fetching agency user with ID: {Id}", id);
        return await unitOfWork.AgencyUsers.GetByIdAsync(id);
    }

    public async Task<List<AgencyUser>> GetAllAsync()
    {
        logger.LogInformation("Fetching all agency users");
        return await unitOfWork.AgencyUsers.GetAllAsync();
    }

    public async Task UpsertAsync(AgencyUser entity, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving agency user: {Email}", entity.Email);
        await unitOfWork.AgencyUsers.UpsertAsync(entity, cancellationToken);
    }

    public async Task<AgencyUser?> GetByEmailAsync(string email)
    {
        logger.LogInformation("Fetching agency user by email: {Email}", email);
        return await unitOfWork.AgencyUsers.GetByEmailAsync(email);
    }

    public async Task AddAsync(AgencyUser entity, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding new agency user: {Email}", entity.Email);
        await unitOfWork.AgencyUsers.AddAsync(entity, cancellationToken);
    }

    public void Update(AgencyUser entity)
    {
        logger.LogInformation("Updating agency user: {Email}", entity.Email);
        unitOfWork.AgencyUsers.Update(entity);
    }

    public async Task<Result> UpdateUserDetailsAsync(
        Guid userId,
        string fullName,
        List<string> roles,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = await unitOfWork.AgencyUsers.GetByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("Update failed. User {UserId} not found.", userId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "User not found." });
            }

            var updateResult = user.UpdateDetails(fullName, roles);
            if (!updateResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return updateResult;
            }

            await unitOfWork.AgencyUsers.UpsertAsync(user, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            logger.LogInformation("User {UserId} details updated successfully.", userId);
            return Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error updating user details for {UserId}", userId);
            return Result.Failure(new[] { "An error occurred while updating user details." });
        }
    }

    public async Task<Result> AddUserRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = await unitOfWork.AgencyUsers.GetByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("Role addition failed. User {UserId} not found.", userId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "User not found." });
            }

            var addRoleResult = user.AddRoles(new List<string> { role });
            if (!addRoleResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return addRoleResult;
            }

            await unitOfWork.AgencyUsers.UpsertAsync(user, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            logger.LogInformation("Role {Role} added to user {UserId} successfully.", role, userId);
            return Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error adding role {Role} to user {UserId}", role, userId);
            return Result.Failure(new[] { "An error occurred while adding user role." });
        }
    }

    public async Task<Result> RemoveUserRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = await unitOfWork.AgencyUsers.GetByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("Role removal failed. User {UserId} not found.", userId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "User not found." });
            }

            var removeRoleResult = user.RemoveRole(role);
            if (!removeRoleResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return removeRoleResult;
            }

            await unitOfWork.AgencyUsers.UpsertAsync(user, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            logger.LogInformation("Role {Role} removed from user {UserId} successfully.", role, userId);
            return Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error removing role {Role} from user {UserId}", role, userId);
            return Result.Failure(new[] { "An error occurred while removing user role." });
        }
    }
}
