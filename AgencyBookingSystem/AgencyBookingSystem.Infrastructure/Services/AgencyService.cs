using Microsoft.Extensions.Logging;

public class AgencyService : IAgencyService
{
    private readonly IAgencyRepository agencyRepository;
    private readonly IAgencyUserRepository agencyUserRepository;
    private readonly IAppointmentSlotRepository appointmentSlotRepository;
    private readonly ILogger<AgencyService> logger;
    private readonly IEventDispatcher eventDispatcher;

    public AgencyService(
        IAgencyRepository agencyRepository,
        IAgencyUserRepository agencyUserRepository,
        IAppointmentSlotRepository appointmentSlotRepository,
        IEventDispatcher eventDispatcher,
        ILogger<AgencyService> logger)
    {
        this.agencyRepository = agencyRepository;
        this.agencyUserRepository = agencyUserRepository;
        this.appointmentSlotRepository = appointmentSlotRepository;
        this.eventDispatcher = eventDispatcher;
        this.logger = logger;
    }

    // Unit of work methods
    public async Task AddAsync(Agency entity, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding new agency: {AgencyName}", entity.Name);
        await agencyRepository.AddAsync(entity, cancellationToken);
    }

    public void Update(Agency entity)
    {
        logger.LogInformation("Updating agency: {AgencyName}", entity.Name);
        agencyRepository.Update(entity);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving changes for agencies");
        await agencyRepository.SaveChangesAsync(cancellationToken);
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

    public async Task UpsertAsync(Agency entity, CancellationToken cancellationToken = default)
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
        return await agencyRepository.ExistsAsync(agencyId);
    }

    public async Task<Result> RegisterAgencyAsync(
        string name,
        string email,
        bool requiresApproval,
        int maxAppointmentsPerDay,
        CancellationToken cancellationToken = default)
    {
        if (await agencyRepository.GetByEmailAsync(email) != null)
        {
            logger.LogWarning("Agency registration failed. Email {Email} is already in use.", email);
            return Result.Failure(new[] { "An agency with this email already exists." });
        }

        var agencyResult = Agency.Create(name, email, requiresApproval, maxAppointmentsPerDay);
        if (!agencyResult.Succeeded)
        {
            return agencyResult;
        }

        var agency = agencyResult.Data;
        await agencyRepository.UpsertAsync(agency, cancellationToken);

        await eventDispatcher.Dispatch(new AgencyRegisteredEvent(
            agency.Id,
            agency.Name,
            agency.Email,
            agency.RequiresApproval
        ), cancellationToken);

        logger.LogInformation("Agency '{AgencyName}' registered successfully. Approval required: {RequiresApproval}.", 
            agency.Name, agency.RequiresApproval);

        return Result.Success;
    }

    public async Task<Result> ApproveAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        var agency = await agencyRepository.GetByIdAsync(agencyId);
        if (agency == null)
        {
            logger.LogWarning("Approval failed. Agency {AgencyId} not found.", agencyId);
            return Result.Failure(new[] { "Agency not found." });
        }

        var approveResult = agency.Approve();
        if (!approveResult.Succeeded)
        {
            return approveResult;
        }

        await agencyRepository.UpsertAsync(agency, cancellationToken);
        logger.LogInformation("Agency {AgencyId} approved successfully.", agencyId);

        return Result.Success;
    }

    public async Task<Result> AssignUserToAgencyAsync(
        Guid agencyId,
        string email,
        string fullName,
        List<string> roles,
        CancellationToken cancellationToken = default)
    {
        // Validate agency exists and is approved
        var agency = await agencyRepository.GetByIdAsync(agencyId);
        if (agency == null)
        {
            logger.LogWarning("User assignment failed. Agency {AgencyId} not found.", agencyId);
            return Result.Failure(new[] { "Agency not found." });
        }

        if (!agency.IsApproved)
        {
            logger.LogWarning("User assignment failed. Agency {AgencyId} is not approved.", agencyId);
            return Result.Failure(new[] { "Cannot assign users to an unapproved agency." });
        }

        // Check if user already exists
        var existingUser = await agencyUserRepository.GetByEmailAsync(email);
        if (existingUser != null)
        {
            logger.LogWarning("User assignment failed. User with email {Email} already exists.", email);
            return Result.Failure(new[] { "A user with this email already exists." });
        }

        // Create new user
        var userResult = AgencyUser.Create(agencyId, email, fullName, roles);
        if (!userResult.Succeeded)
        {
            logger.LogWarning("User creation failed. Errors: {Errors}", string.Join(", ", userResult.Errors));
            return userResult;
        }

        var user = userResult.Data;

        // Set up the back-reference
        var assignResult = agency.AssignUser(user);
        if (!assignResult.Succeeded)
        {
            return assignResult;
        }

        // Track all changes
        await agencyUserRepository.AddAsync(user, cancellationToken);
        agencyRepository.Update(agency);

        // Save all changes in one transaction
        await agencyRepository.SaveChangesAsync(cancellationToken);

        // Dispatch event after successful save
        await eventDispatcher.Dispatch(new AgencyUserAssignedEvent(
            user.Id,
            agency.Id,
            user.Email,
            user.FullName,
            user.Roles.ToList()
        ), cancellationToken);

        logger.LogInformation("User {Email} assigned to agency {AgencyId} successfully.", email, agencyId);
        return Result.Success;
    }

    public async Task<string?> GetUserEmailAsync(Guid userId)
    {
        var user = await agencyUserRepository.GetByIdAsync(userId);
        return user?.Email;
    }

    public async Task<AgencyUser?> GetAgencyUserByEmailAsync(string email)
    {
        return await agencyUserRepository.GetByEmailAsync(email);
    }

    public async Task<Result> InitializeAppointmentSlotsAsync(
        Guid agencyId,
        DateTime startDate,
        DateTime endDate,
        TimeSpan slotDuration,
        int slotsPerDay,
        int capacityPerSlot,
        CancellationToken cancellationToken = default)
    {
        var agency = await agencyRepository.GetByIdAsync(agencyId);
        if (agency == null)
        {
            logger.LogWarning("Failed to initialize slots. Agency {AgencyId} not found.", agencyId);
            return Result.Failure(new[] { "Agency not found." });
        }

        if (!agency.IsApproved)
        {
            logger.LogWarning("Failed to initialize slots. Agency {AgencyId} is not approved.", agencyId);
            return Result.Failure(new[] { "Agency is not approved." });
        }

        // Validate date range
        if (startDate.Date < DateTime.UtcNow.Date)
        {
            return Result.Failure(new[] { "Start date cannot be in the past." });
        }

        if (endDate.Date < startDate.Date)
        {
            return Result.Failure(new[] { "End date must be after start date." });
        }

        if ((endDate.Date - startDate.Date).TotalDays > 90)
        {
            return Result.Failure(new[] { "Cannot initialize slots for more than 90 days." });
        }

        // Validate slot parameters
        if (slotDuration.TotalMinutes < 15 || slotDuration.TotalHours > 8)
        {
            return Result.Failure(new[] { "Slot duration must be between 15 minutes and 8 hours." });
        }

        if (slotsPerDay < 1 || slotsPerDay > 48)
        {
            return Result.Failure(new[] { "Number of slots per day must be between 1 and 48." });
        }

        if (capacityPerSlot < 1 || capacityPerSlot > agency.MaxAppointmentsPerDay)
        {
            return Result.Failure(new[] { $"Capacity per slot must be between 1 and {agency.MaxAppointmentsPerDay}." });
        }

        var currentDate = startDate.Date;
        while (currentDate <= endDate.Date)
        {
            // Skip holidays
            if (agency.Holidays.Any(h => h.Date.Date == currentDate))
            {
                currentDate = currentDate.AddDays(1);
                continue;
            }

            // Calculate slot start times for the day
            var slotStartTimes = new List<DateTime>();
            var currentTime = currentDate.AddHours(9); // Start at 9 AM
            var endTime = currentDate.AddHours(17); // End at 5 PM

            while (currentTime.Add(slotDuration) <= endTime && slotStartTimes.Count < slotsPerDay)
            {
                slotStartTimes.Add(currentTime);
                currentTime = currentTime.Add(slotDuration);
            }

            // Create slots for the day
            foreach (var startTime in slotStartTimes)
            {
                var slot = AppointmentSlot.Create(agencyId, startTime, startTime.Add(slotDuration), capacityPerSlot);

                await appointmentSlotRepository.UpsertAsync(slot, cancellationToken);
            }

            currentDate = currentDate.AddDays(1);
        }

        logger.LogInformation("Successfully initialized appointment slots for Agency {AgencyId} from {StartDate} to {EndDate}.",
            agencyId, startDate.Date, endDate.Date);

        return Result.Success;
    }

    public async Task<List<AppointmentSlot>> GetAvailableSlotsAsync(Guid agencyId, DateTime date)
    {
        return await appointmentSlotRepository.GetSlotsByAgencyAsync(agencyId, date);
    }

    public async Task<Result> AddHolidayAsync(
        Guid agencyId,
        DateTime date,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var agency = await agencyRepository.GetByIdAsync(agencyId);
        if (agency == null)
        {
            logger.LogWarning("Failed to add holiday. Agency {AgencyId} not found.", agencyId);
            return Result.Failure(new[] { "Agency not found." });
        }

        // Add holiday to agency using domain method
        var addHolidayResult = agency.AddHoliday(date, reason);
        if (!addHolidayResult.Succeeded)
        {
            return addHolidayResult;
        }

        // Track changes
        agencyRepository.Update(agency);

        // Save all changes
        await agencyRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully added holiday for Agency {AgencyId} on {Date}.", agencyId, date);
        return Result.Success;
    }

    public async Task<Result> RemoveHolidayAsync(
        Guid agencyId,
        Guid holidayId,
        CancellationToken cancellationToken = default)
    {
        var agency = await agencyRepository.GetByIdAsync(agencyId);
        if (agency == null)
        {
            logger.LogWarning("Failed to remove holiday. Agency {AgencyId} not found.", agencyId);
            return Result.Failure(new[] { "Agency not found." });
        }

        // Remove holiday from agency using domain method
        var removeHolidayResult = agency.RemoveHoliday(holidayId);
        if (!removeHolidayResult.Succeeded)
        {
            return removeHolidayResult;
        }

        // Save changes - DbContext will handle the transaction
        await agencyRepository.UpsertAsync(agency, cancellationToken);
        logger.LogInformation("Successfully removed holiday {HolidayId} for Agency {AgencyId}.", holidayId, agencyId);

        return Result.Success;
    }

    public async Task<Result> AddAppointmentSlotAsync(
        Guid agencyId,
        DateTime startTime,
        int capacity,
        CancellationToken cancellationToken = default)
    {
        var agency = await agencyRepository.GetByIdAsync(agencyId);
        if (agency == null)
        {
            logger.LogWarning("Failed to add slot. Agency {AgencyId} not found.", agencyId);
            return Result.Failure(new[] { "Agency not found." });
        }

        if (!agency.IsApproved)
        {
            logger.LogWarning("Failed to add slot. Agency {AgencyId} is not approved.", agencyId);
            return Result.Failure(new[] { "Agency is not approved." });
        }

        // Create the slot
        var slot = AppointmentSlot.Create(agencyId, startTime, startTime.AddHours(1), capacity);

        // Add it to the agency using domain method
        var addSlotResult = agency.AddAppointmentSlot(startTime, capacity);
        if (!addSlotResult.Succeeded)
        {
            return addSlotResult;
        }

        // Track all changes
        await appointmentSlotRepository.AddAsync(slot, cancellationToken);
        agencyRepository.Update(agency);

        // Save all changes in one transaction
        await agencyRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully added appointment slot for Agency {AgencyId} at {StartTime}.", agencyId, startTime);
        return Result.Success;
    }

    public async Task<Result> RemoveAppointmentSlotAsync(
        Guid agencyId,
        Guid slotId,
        CancellationToken cancellationToken = default)
    {
        var agency = await agencyRepository.GetByIdAsync(agencyId);
        if (agency == null)
        {
            logger.LogWarning("Failed to remove slot. Agency {AgencyId} not found.", agencyId);
            return Result.Failure(new[] { "Agency not found." });
        }

        var removeSlotResult = agency.RemoveAppointmentSlot(slotId);
        if (!removeSlotResult.Succeeded)
        {
            return removeSlotResult;
        }

        await agencyRepository.UpsertAsync(agency, cancellationToken);
        logger.LogInformation("Successfully removed appointment slot {SlotId} for Agency {AgencyId}.", slotId, agencyId);

        return Result.Success;
    }

    public async Task<Result> UpdateAgencyDetailsAsync(
        Guid agencyId,
        string name,
        string email,
        int maxAppointmentsPerDay,
        CancellationToken cancellationToken = default)
    {
        var agency = await agencyRepository.GetByIdAsync(agencyId);
        if (agency == null)
        {
            logger.LogWarning("Failed to update details. Agency {AgencyId} not found.", agencyId);
            return Result.Failure(new[] { "Agency not found." });
        }

        var updateResult = agency.UpdateDetails(name, email, maxAppointmentsPerDay);
        if (!updateResult.Succeeded)
        {
            return updateResult;
        }

        await agencyRepository.UpsertAsync(agency, cancellationToken);
        logger.LogInformation("Successfully updated details for Agency {AgencyId}.", agencyId);

        return Result.Success;
    }

    public async Task<Result> RemoveUserAsync(
        Guid agencyId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var agency = await agencyRepository.GetByIdAsync(agencyId);
        if (agency == null)
        {
            logger.LogWarning("Failed to remove user. Agency {AgencyId} not found.", agencyId);
            return Result.Failure(new[] { "Agency not found." });
        }

        var removeUserResult = agency.RemoveUser(userId);
        if (!removeUserResult.Succeeded)
        {
            return removeUserResult;
        }

        await agencyRepository.UpsertAsync(agency, cancellationToken);
        logger.LogInformation("Successfully removed user {UserId} from Agency {AgencyId}.", userId, agencyId);

        return Result.Success;
    }
}
