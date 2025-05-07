public interface IAgencyService : IBaseService<Agency>
{
    Task<List<Agency>> GetAgenciesWithUsersAsync();
    Task<Agency?> GetByEmailAsync(string email);
    Task<List<Agency>> GetApprovedAgenciesAsync();
    Task<bool> ExistsAsync(Guid agencyId);

    // Assign a user to an agency with validation & notification support
    Task<Result> AssignUserToAgencyAsync(Guid agencyId, string email, string fullName, List<string> roles, CancellationToken cancellationToken = default);

    // Register an agency with approval logic, notifying admins if required
    Task<Result> RegisterAgencyAsync(string name, string email, bool requiresApproval, int maxAppointmentsPerDay, CancellationToken cancellationToken = default);

    // Approve an agency, transitioning it to an active state & notifying relevant parties
    Task<Result> ApproveAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default);

    // Retrieve an agency user's email dynamically for notifications
    Task<string?> GetUserEmailAsync(Guid userId);
    Task<AgencyUser?> GetAgencyUserByEmailAsync(string email);
}
