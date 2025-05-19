using Microsoft.Extensions.Logging;
using System.Transactions;

public class AgencyService : IAgencyService
{
    private readonly IAppointmentUnitOfWork unitOfWork;
    private readonly ILogger<AgencyService> logger;
    private readonly IEventDispatcher eventDispatcher;

    public AgencyService(
        IAppointmentUnitOfWork unitOfWork,
        IEventDispatcher eventDispatcher,
        ILogger<AgencyService> logger)
    {
        this.unitOfWork = unitOfWork;
        this.eventDispatcher = eventDispatcher;
        this.logger = logger;
    }

    public async Task AddAsync(Agency entity, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding new agency: {AgencyName}", entity.Name);
        await unitOfWork.Agencies.AddAsync(entity, cancellationToken);
    }

    public void Update(Agency entity)
    {
        logger.LogInformation("Updating agency: {AgencyName}", entity.Name);
        unitOfWork.Agencies.Update(entity);
    }

    public async Task<Agency?> GetByIdAsync(Guid id)
    {
        logger.LogInformation("Fetching agency with ID: {Id}", id);
        return await unitOfWork.Agencies.GetByIdAsync(id);
    }

    public async Task<List<Agency>> GetAllAsync()
    {
        logger.LogInformation("Fetching all agencies.");
        return await unitOfWork.Agencies.GetAllAsync();
    }

    public async Task UpsertAsync(Agency entity, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving agency: {AgencyName}", entity.Name);
        await unitOfWork.Agencies.UpsertAsync(entity, cancellationToken);
    }

    public async Task<List<Agency>> GetAgenciesWithUsersAsync()
    {
        logger.LogInformation("Fetching agencies with users.");
        return await unitOfWork.Agencies.GetAgenciesWithUsersAsync();
    }

    public async Task<Agency?> GetByEmailAsync(string email)
    {
        logger.LogInformation("Fetching agency by email: {Email}", email);
        return await unitOfWork.Agencies.GetByEmailAsync(email);
    }

    public async Task<List<Agency>> GetApprovedAgenciesAsync()
    {
        logger.LogInformation("Fetching all approved agencies.");
        return await unitOfWork.Agencies.GetApprovedAgenciesAsync();
    }

    public async Task<bool> ExistsAsync(Guid agencyId)
    {
        return await unitOfWork.Agencies.ExistsAsync(agencyId);
    }

    public async Task<Result> RegisterAgencyAsync(
        string name,
        string email,
        bool requiresApproval,
        int maxAppointmentsPerDay,
        CancellationToken cancellationToken = default)
    {
        // Check for existing agency outside transaction
        if (await unitOfWork.Agencies.GetByEmailAsync(email) != null)
        {
            logger.LogWarning("Agency registration failed. Email {Email} is already in use.", email);
            return Result.Failure(new[] { "An agency with this email already exists." });
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var agencyResult = Agency.Create(name, email, requiresApproval, maxAppointmentsPerDay);
            if (!agencyResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return agencyResult;
            }

            var agency = agencyResult.Data;
            await unitOfWork.Agencies.UpsertAsync(agency, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            // Create the event outside transaction
            var registeredEvent = new AgencyRegisteredEvent(
                agency.Id,
                agency.Name,
                agency.Email,
                agency.RequiresApproval
            );

            logger.LogInformation("Agency '{AgencyName}' registered successfully. Approval required: {RequiresApproval}.", 
                agency.Name, agency.RequiresApproval);
            
            // Dispatch events outside the transaction
            try
            {
                await eventDispatcher.Dispatch(registeredEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error dispatching events for agency registration. Agency: {Email}", email);
                // Don't fail the operation if event dispatch fails
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error registering agency with name {Name} and email {Email}", name, email);
            return Result.Failure(new[] { "An error occurred while registering the agency." });
        }
    }

    public async Task<Result> ApproveAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var agency = await unitOfWork.Agencies.GetByIdAsync(agencyId);
            if (agency == null)
            {
                logger.LogWarning("Approval failed. Agency {AgencyId} not found.", agencyId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "Agency not found." });
            }

            var approveResult = agency.Approve();
            if (!approveResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return approveResult;
            }

            await unitOfWork.Agencies.UpsertAsync(agency, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            
            logger.LogInformation("Agency {AgencyId} approved successfully.", agencyId);
            return Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error approving agency {AgencyId}", agencyId);
            return Result.Failure(new[] { "An error occurred while approving the agency." });
        }
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

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var agency = await unitOfWork.Agencies.GetByIdAsync(agencyId);
            if (agency == null)
            {
                logger.LogWarning("Assignment failed. Agency {AgencyId} not found.", agencyId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "Agency not found." });
            }

            var existingUser = await unitOfWork.AgencyUsers.GetByEmailAsync(email);
            if (existingUser != null)
            {
                logger.LogWarning("Assignment failed. User with email {Email} already exists.", email);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "A user with this email already exists." });
            }

            var userResult = AgencyUser.Create(agencyId, email, fullName, roles);
            if (!userResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return userResult;
            }

            var user = userResult.Data;
            var assignResult = agency.AssignUser(user);
            if (!assignResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return assignResult;
            }

            await unitOfWork.AgencyUsers.AddAsync(user, cancellationToken);
            unitOfWork.Agencies.Update(agency);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation("User {Email} assigned to Agency {AgencyId} successfully.", email, agencyId);
            return Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error assigning user {Email} to Agency {AgencyId}", email, agencyId);
            return Result.Failure(new[] { "An error occurred while assigning the user to the agency." });
        }
    }

    public async Task<Result> UpdateAgencyDetailsAsync(
        Guid agencyId,
        string name,
        string email,
        int maxAppointmentsPerDay,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var agency = await unitOfWork.Agencies.GetByIdAsync(agencyId);
            if (agency == null)
            {
                logger.LogWarning("Update failed. Agency {AgencyId} not found.", agencyId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "Agency not found." });
            }

            var updateResult = agency.UpdateDetails(name, email, maxAppointmentsPerDay);
            if (!updateResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return updateResult;
            }

            await unitOfWork.Agencies.UpsertAsync(agency, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            
            logger.LogInformation("Agency {AgencyId} details updated successfully.", agencyId);
            return Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error updating agency {AgencyId}", agencyId);
            return Result.Failure(new[] { "An error occurred while updating the agency details." });
        }
    }

    public async Task<Result> RemoveUserAsync(
        Guid agencyId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var agency = await unitOfWork.Agencies.GetByIdAsync(agencyId);
            if (agency == null)
            {
                logger.LogWarning("Removal failed. Agency {AgencyId} not found.", agencyId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "Agency not found." });
            }

            var user = await unitOfWork.AgencyUsers.GetByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("Removal failed. User {UserId} not found.", userId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "User not found." });
            }

            var removeResult = agency.RemoveUser(userId);
            if (!removeResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return removeResult;
            }

            unitOfWork.Agencies.Update(agency);
            await unitOfWork.AgencyUsers.DeleteAsync(userId, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            
            logger.LogInformation("User {UserId} removed from Agency {AgencyId} successfully.", userId, agencyId);
            return Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error removing user {UserId} from Agency {AgencyId}", userId, agencyId);
            return Result.Failure(new[] { "An error occurred while removing the user from the agency." });
        }
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
        var agency = await unitOfWork.Agencies.GetByIdAsync(agencyId);
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
        try
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
                await unitOfWork.AppointmentSlots.AddAsync(slot, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing date range for Agency {AgencyId}", agency.Id);
            return Result.Failure(new[] { "An error occurred while processing the date range." });
        }
    }

    public async Task<List<AppointmentSlot>> GetAvailableSlotsAsync(Guid agencyId, DateTime date)
    {
        return await unitOfWork.AppointmentSlots.GetSlotsByAgencyAsync(agencyId, date);
    }

    public async Task<Result> AddHolidayAsync(
        Guid agencyId,
        DateTime date,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var agency = await unitOfWork.Agencies.GetByIdAsync(agencyId);
            if (agency == null)
            {
                logger.LogWarning("Failed to add holiday. Agency {AgencyId} not found.", agencyId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "Agency not found." });
            }

            // Add holiday to agency using domain method
            var addHolidayResult = agency.AddHoliday(date, reason);
            if (!addHolidayResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return addHolidayResult;
            }

            // Track changes
            unitOfWork.Agencies.Update(agency);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation("Successfully added holiday for Agency {AgencyId} on {Date}.", agencyId, date);
            return Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error adding holiday for Agency {AgencyId}", agencyId);
            return Result.Failure(new[] { "An error occurred while adding the holiday." });
        }
    }

    public async Task<Result> RemoveHolidayAsync(
        Guid agencyId,
        Guid holidayId,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var agency = await unitOfWork.Agencies.GetByIdAsync(agencyId);
            if (agency == null)
            {
                logger.LogWarning("Failed to remove holiday. Agency {AgencyId} not found.", agencyId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "Agency not found." });
            }

            // Remove holiday from agency using domain method
            var removeHolidayResult = agency.RemoveHoliday(holidayId);
            if (!removeHolidayResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return removeHolidayResult;
            }

            await unitOfWork.Agencies.UpsertAsync(agency, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation("Successfully removed holiday {HolidayId} for Agency {AgencyId}.", holidayId, agencyId);
            return Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error removing holiday for Agency {AgencyId}", agencyId);
            return Result.Failure(new[] { "An error occurred while removing the holiday." });
        }
    }

    public async Task<Result> AddAppointmentSlotAsync(
        Guid agencyId,
        DateTime startTime,
        int capacity,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var agency = await unitOfWork.Agencies.GetByIdAsync(agencyId);
            if (agency == null)
            {
                logger.LogWarning("Failed to add slot. Agency {AgencyId} not found.", agencyId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "Agency not found." });
            }

            if (!agency.IsApproved)
            {
                logger.LogWarning("Failed to add slot. Agency {AgencyId} is not approved.", agencyId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "Agency is not approved." });
            }

            // Create the slot
            var slot = AppointmentSlot.Create(agencyId, startTime, startTime.AddHours(1), capacity);

            // Add it to the agency using domain method
            var addSlotResult = agency.AddAppointmentSlot(startTime, capacity);
            if (!addSlotResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return addSlotResult;
            }

            // Track all changes within the transaction
            await unitOfWork.AppointmentSlots.AddAsync(slot, cancellationToken);
            unitOfWork.Agencies.Update(agency);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation("Successfully added appointment slot for Agency {AgencyId} at {StartTime}.", agencyId, startTime);
            return Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error adding appointment slot for Agency {AgencyId}", agencyId);
            return Result.Failure(new[] { "An error occurred while adding the appointment slot." });
        }
    }

    public async Task<Result> RemoveAppointmentSlotAsync(
        Guid agencyId,
        Guid slotId,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var agency = await unitOfWork.Agencies.GetByIdAsync(agencyId);
            if (agency == null)
            {
                logger.LogWarning("Failed to remove slot. Agency {AgencyId} not found.", agencyId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "Agency not found." });
            }

            var removeSlotResult = agency.RemoveAppointmentSlot(slotId);
            if (!removeSlotResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return removeSlotResult;
            }

            await unitOfWork.Agencies.UpsertAsync(agency, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation("Successfully removed appointment slot {SlotId} for Agency {AgencyId}.", slotId, agencyId);
            return Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error removing appointment slot for Agency {AgencyId}", agencyId);
            return Result.Failure(new[] { "An error occurred while removing the appointment slot." });
        }
    }
}
