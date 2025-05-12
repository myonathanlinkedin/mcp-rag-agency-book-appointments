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

    // Initialize appointment slots for an agency for a given date range
    Task<Result> InitializeAppointmentSlotsAsync(
        Guid agencyId,
        DateTime startDate,
        DateTime endDate,
        TimeSpan slotDuration,
        int slotsPerDay,
        int capacityPerSlot,
        CancellationToken cancellationToken = default);

    // Get available slots for an agency on a specific date
    Task<List<AppointmentSlot>> GetAvailableSlotsAsync(Guid agencyId, DateTime date);

    // Add a single appointment slot
    Task<Result> AddAppointmentSlotAsync(
        Guid agencyId,
        DateTime startTime,
        int capacity,
        CancellationToken cancellationToken = default);

    // Remove an appointment slot
    Task<Result> RemoveAppointmentSlotAsync(
        Guid agencyId,
        Guid slotId,
        CancellationToken cancellationToken = default);

    Task<Result> AddHolidayAsync(
        Guid agencyId,
        DateTime date,
        string reason,
        CancellationToken cancellationToken = default);

    Task<Result> RemoveHolidayAsync(
        Guid agencyId,
        Guid holidayId,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateAgencyDetailsAsync(
        Guid agencyId,
        string name,
        string email,
        int maxAppointmentsPerDay,
        CancellationToken cancellationToken = default);

    Task<Result> RemoveUserAsync(
        Guid agencyId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
