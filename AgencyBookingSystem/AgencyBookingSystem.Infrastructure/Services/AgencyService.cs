using Microsoft.Extensions.Logging;
using System.Transactions;

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
        // Check for existing agency outside transaction
        if (await agencyRepository.GetByEmailAsync(email) != null)
        {
            logger.LogWarning("Agency registration failed. Email {Email} is already in use.", email);
            return Result.Failure(new[] { "An agency with this email already exists." });
        }

        Result result = null;
        await TransactionHelper.ExecuteInTransactionAsync(async () =>
        {
            try
            {
                var agencyResult = Agency.Create(name, email, requiresApproval, maxAppointmentsPerDay);
                if (!agencyResult.Succeeded)
                {
                    result = agencyResult;
                    return;
                }

                var agency = agencyResult.Data;
                await agencyRepository.UpsertAsync(agency, cancellationToken);
                await agencyRepository.SaveChangesAsync(cancellationToken);

                // Create the event outside transaction
                var registeredEvent = new AgencyRegisteredEvent(
                    agency.Id,
                    agency.Name,
                    agency.Email,
                    agency.RequiresApproval
                );

                logger.LogInformation("Agency '{AgencyName}' registered successfully. Approval required: {RequiresApproval}.", 
                    agency.Name, agency.RequiresApproval);
                
                result = Result.Success;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error registering agency with name {Name} and email {Email}", name, email);
                result = Result.Failure(new[] { "An error occurred while registering the agency." });
            }
        }, System.Transactions.IsolationLevel.RepeatableRead);

        // Dispatch events outside the transaction
        if (result?.Succeeded == true)
        {
            try
            {
                var agency = await agencyRepository.GetByEmailAsync(email);
                if (agency != null)
                {
                    await eventDispatcher.Dispatch(new AgencyRegisteredEvent(
                        agency.Id,
                        agency.Name,
                        agency.Email,
                        agency.RequiresApproval
                    ), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error dispatching events for agency registration. Agency: {Email}", email);
                // Don't fail the operation if event dispatch fails
            }
        }

        return result;
    }

    public async Task<Result> ApproveAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        return await TransactionHelper.ExecuteInTransactionAsync(async () =>
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
        }, IsolationLevel.RepeatableRead);
    }

    public async Task<Result> AssignUserToAgencyAsync(
        Guid agencyId,
        string email,
        string fullName,
        List<string> roles,
        CancellationToken cancellationToken = default)
    {
        // Validate inputs outside transaction
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName) || roles == null || !roles.Any())
        {
            logger.LogWarning("Invalid input parameters for user assignment");
            return Result.Failure(new[] { "Invalid input parameters" });
        }

        // Check for existing user outside transaction
        if (await agencyUserRepository.GetByEmailAsync(email) != null)
        {
            logger.LogWarning("User assignment failed. User with email {Email} already exists.", email);
            return Result.Failure(new[] { "A user with this email already exists." });
        }

        Result result = null;
        AgencyUser createdUser = null;
        await TransactionHelper.ExecuteInTransactionAsync(async () =>
        {
            try
            {
                // Validate agency exists and is approved
                var agency = await agencyRepository.GetByIdAsync(agencyId);
                if (agency == null)
                {
                    logger.LogWarning("User assignment failed. Agency {AgencyId} not found.", agencyId);
                    result = Result.Failure(new[] { "Agency not found." });
                    return;
                }

                if (!agency.IsApproved)
                {
                    logger.LogWarning("User assignment failed. Agency {AgencyId} is not approved.", agencyId);
                    result = Result.Failure(new[] { "Cannot assign users to an unapproved agency." });
                    return;
                }

                // Create new user
                var userResult = AgencyUser.Create(agencyId, email, fullName, roles);
                if (!userResult.Succeeded)
                {
                    result = userResult;
                    return;
                }

                var user = userResult.Data;

                // Set up back-reference
                var assignResult = agency.AssignUser(user);
                if (!assignResult.Succeeded)
                {
                    result = assignResult;
                    return;
                }

                // Save all changes
                await agencyUserRepository.AddAsync(user, cancellationToken);
                await agencyUserRepository.SaveChangesAsync(cancellationToken);

                agencyRepository.Update(agency);
                await agencyRepository.SaveChangesAsync(cancellationToken);

                createdUser = user;
                logger.LogInformation("User {Email} assigned to agency {AgencyId} successfully.", email, agencyId);
                result = Result.Success;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error assigning user {Email} to agency {AgencyId}", email, agencyId);
                result = Result.Failure(new[] { "An error occurred while assigning the user to the agency." });
            }
        }, System.Transactions.IsolationLevel.RepeatableRead);

        // Dispatch events outside the transaction
        if (result?.Succeeded == true && createdUser != null)
        {
            try
            {
                await eventDispatcher.Dispatch(new AgencyUserAssignedEvent(
                    createdUser.Id,
                    agencyId,
                    createdUser.Email,
                    createdUser.FullName,
                    createdUser.Roles.ToList()
                ), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error dispatching events for user assignment. User: {Email}, Agency: {AgencyId}", email, agencyId);
                // Don't fail the operation if event dispatch fails
            }
        }

        return result;
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
        // First validate all parameters outside of transaction
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

        try
        {
            // Process in smaller batches (3 days at a time) to avoid transaction timeouts
            var currentDate = startDate.Date;
            while (currentDate <= endDate.Date)
            {
                var batchEndDate = currentDate.AddDays(3);
                if (batchEndDate > endDate.Date)
                {
                    batchEndDate = endDate.Date;
                }

                var batchResult = await ProcessDateRangeAsync(
                    agency,
                    currentDate,
                    batchEndDate,
                    slotDuration,
                    slotsPerDay,
                    capacityPerSlot,
                    cancellationToken);

                if (!batchResult.Succeeded)
                {
                    return batchResult;
                }

                currentDate = batchEndDate.AddDays(1);
            }

            logger.LogInformation(
                "Successfully initialized appointment slots for Agency {AgencyId} from {StartDate} to {EndDate}.",
                agencyId,
                startDate.Date,
                endDate.Date);

            return Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing appointment slots for Agency {AgencyId}", agencyId);
            return Result.Failure(new[] { "An error occurred while initializing appointment slots." });
        }
    }

    private async Task<Result> ProcessDateRangeAsync(
        Agency agency,
        DateTime startDate,
        DateTime endDate,
        TimeSpan slotDuration,
        int slotsPerDay,
        int capacityPerSlot,
        CancellationToken cancellationToken)
    {
        return await TransactionHelper.ExecuteInTransactionAsync(async () =>
        {
            var slots = new List<AppointmentSlot>();
            var currentDate = startDate;

            while (currentDate <= endDate)
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
                    var slot = AppointmentSlot.Create(
                        agency.Id,
                        startTime,
                        startTime.Add(slotDuration),
                        capacityPerSlot);

                    slots.Add(slot);
                }

                currentDate = currentDate.AddDays(1);
            }

            // Batch insert all slots for this date range
            foreach (var slot in slots)
            {
                await appointmentSlotRepository.AddAsync(slot, cancellationToken);
            }

            await appointmentSlotRepository.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }, IsolationLevel.RepeatableRead, maxRetries: 5);
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
        return await TransactionHelper.ExecuteInTransactionAsync(async () =>
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
        }, IsolationLevel.RepeatableRead);
    }

    public async Task<Result> RemoveHolidayAsync(
        Guid agencyId,
        Guid holidayId,
        CancellationToken cancellationToken = default)
    {
        return await TransactionHelper.ExecuteInTransactionAsync(async () =>
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

            await agencyRepository.UpsertAsync(agency, cancellationToken);
            logger.LogInformation("Successfully removed holiday {HolidayId} for Agency {AgencyId}.", holidayId, agencyId);

            return Result.Success;
        }, IsolationLevel.RepeatableRead);
    }

    public async Task<Result> AddAppointmentSlotAsync(
        Guid agencyId,
        DateTime startTime,
        int capacity,
        CancellationToken cancellationToken = default)
    {
        return await TransactionHelper.ExecuteInTransactionAsync(async () =>
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

            // Track all changes within the transaction
            await appointmentSlotRepository.AddAsync(slot, cancellationToken);
            agencyRepository.Update(agency);
            await agencyRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully added appointment slot for Agency {AgencyId} at {StartTime}.", agencyId, startTime);
            return Result.Success;
        }, IsolationLevel.RepeatableRead);
    }

    public async Task<Result> RemoveAppointmentSlotAsync(
        Guid agencyId,
        Guid slotId,
        CancellationToken cancellationToken = default)
    {
        return await TransactionHelper.ExecuteInTransactionAsync(async () =>
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
        }, IsolationLevel.RepeatableRead);
    }

    public async Task<Result> UpdateAgencyDetailsAsync(
        Guid agencyId,
        string name,
        string email,
        int maxAppointmentsPerDay,
        CancellationToken cancellationToken = default)
    {
        return await TransactionHelper.ExecuteInTransactionAsync(async () =>
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
        }, IsolationLevel.RepeatableRead);
    }

    public async Task<Result> RemoveUserAsync(
        Guid agencyId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await TransactionHelper.ExecuteInTransactionAsync(async () =>
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
        }, IsolationLevel.RepeatableRead);
    }
}
