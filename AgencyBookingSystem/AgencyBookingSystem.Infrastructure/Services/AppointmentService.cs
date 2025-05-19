using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Threading;

public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository appointmentRepository;
    private readonly IAppointmentSlotRepository appointmentSlotRepository;
    private readonly IAgencyService agencyService;
    private readonly IAgencyUserService agencyUserService;
    private readonly IEventDispatcher eventDispatcher; 
    private readonly ILogger<AppointmentService> logger;
    private readonly IProducer<Null, string> kafkaProducer;
    private readonly ApplicationSettings settings;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IAgencyUserService agencyUserService,
        IAgencyService agencyService,
        IEventDispatcher eventDispatcher,
        ILogger<AppointmentService> logger,
        IProducer<Null, string> kafkaProducer,
        IAppointmentSlotRepository appointmentSlotRepository,
        ApplicationSettings settings)
    {
        this.appointmentRepository = appointmentRepository;
        this.appointmentSlotRepository = appointmentSlotRepository;
        this.agencyService = agencyService;
        this.agencyUserService = agencyUserService;
        this.eventDispatcher = eventDispatcher;
        this.logger = logger;
        this.kafkaProducer = kafkaProducer;
        this.settings = settings;
    }

    private async Task PublishToKafkaAsync(string action, Appointment appointment, Agency agency, AgencyUser agencyUser)
    {
        // Validate the action is a known domain action
        if (!new[]
        {
            CommonModelConstants.KafkaOperation.Created,
            CommonModelConstants.KafkaOperation.Rescheduled,
            CommonModelConstants.KafkaOperation.NoShow,
            CommonModelConstants.KafkaOperation.Cancelled
        }.Contains(action))
        {
            throw new ArgumentException($"Unsupported action: {action}", nameof(action));
        }

        // Map domain actions to CRUD operations
        var crudAction = action switch
        {
            CommonModelConstants.KafkaOperation.Created => CommonModelConstants.KafkaOperation.Insert,
            CommonModelConstants.KafkaOperation.Rescheduled => CommonModelConstants.KafkaOperation.Update,
            CommonModelConstants.KafkaOperation.NoShow => CommonModelConstants.KafkaOperation.Update,
            CommonModelConstants.KafkaOperation.Cancelled => CommonModelConstants.KafkaOperation.Update,
            _ => action // If it's already a CRUD operation, use it as is
        };

        var message = new
        {
            Action = crudAction,
            Id = appointment.Id,
            Name = appointment.Name,
            Date = appointment.Date,
            Status = appointment.Status,
            AgencyId = agency.Id,
            AgencyName = agency.Name,
            AgencyEmail = agency.Email,
            UserEmail = agencyUser.Email
        };

        var messageJson = JsonConvert.SerializeObject(message);
        await kafkaProducer.ProduceAsync(settings.Kafka.Topic, new Message<Null, string> { Value = messageJson });

        logger.LogInformation("Published {Action} event for Appointment ID {AppointmentId} to Kafka topic {KafkaTopic}.", action, appointment.Id, settings.Kafka.Topic);
    }

    public async Task<Appointment?> GetByIdAsync(Guid id)
    {
        logger.LogInformation("Fetching appointment with ID: {Id}", id);
        return await appointmentRepository.GetByIdAsync(id);
    }

    public async Task<List<Appointment>> GetAllAsync()
    {
        logger.LogInformation("Fetching all appointments.");
        return await appointmentRepository.GetAllAsync();
    }

    public async Task<List<Appointment>> GetAppointmentsByAgencyAsync(Guid agencyId)
    {
        logger.LogInformation("Fetching appointments for Agency ID: {AgencyId}", agencyId);
        return await appointmentRepository.GetAppointmentsByAgencyAsync(agencyId);
    }

    public async Task<bool> HasAvailableSlotAsync(Guid agencyId, DateTime date)
    {
        var agency = await agencyService.GetByIdAsync(agencyId);
        if (agency == null)
        {
            logger.LogWarning("Agency {AgencyId} not found when checking slot availability.", agencyId);
            return false;
        }

        // Check if the date is a holiday
        if (agency.Holidays != null && agency.Holidays.Any(h => h.Date.Date == date.Date))
        {
            logger.LogInformation("Date {Date} is a holiday for Agency {AgencyId}.", date, agencyId);
            return false;
        }

        // Get available slot for the date
        var availableSlot = await appointmentSlotRepository.GetAvailableSlotAsync(agencyId, date);
        if (availableSlot == null || availableSlot.Capacity <= 0)
        {
            logger.LogInformation("No available slots for Agency {AgencyId} on {Date}.", agencyId, date);
            return false;
        }

        // Check against max appointments per day
        var appointmentsOnDate = await appointmentRepository.GetByDateAsync(date);
        if (appointmentsOnDate == null)
        {
            appointmentsOnDate = new List<Appointment>();
        }

        var appointmentsCount = appointmentsOnDate.Count(a => a.AgencyId == agencyId);
        
        if (appointmentsCount >= agency.MaxAppointmentsPerDay)
        {
            logger.LogInformation("Maximum appointments reached for Agency {AgencyId} on {Date}.", agencyId, date);
            return false;
        }

        return true;
    }

    public async Task HandleNoShowAsync(Guid appointmentId)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId);
        if (appointment == null)
        {
            throw new InvalidOperationException("Appointment not found.");
        }

        // Get agency and user before marking as no-show
        var agency = await agencyService.GetByIdAsync(appointment.AgencyId);
        if (agency == null)
        {
            throw new InvalidOperationException("Agency not found.");
        }

        var agencyUser = await agencyUserService.GetByIdAsync(appointment.AgencyUserId);
        if (agencyUser == null)
        {
            throw new InvalidOperationException("Agency user not found.");
        }

        // Use domain method to mark as no-show
        var noShowResult = appointment.MarkAsNoShow();
        if (!noShowResult.Succeeded)
        {
            throw new InvalidOperationException(noShowResult.Errors.First());
        }

        // Save changes
        await appointmentRepository.UpsertAsync(appointment);

        // ✅ Dispatch event
        await eventDispatcher.Dispatch(new AppointmentEvent(
            appointment.Id, appointment.Name, appointment.Date, appointment.Status, agency.Name, agency.Email, agencyUser.Email
        ), default);

        // ✅ Publish event  
        await PublishToKafkaAsync(CommonModelConstants.KafkaOperation.NoShow, appointment, agency, agencyUser);

        logger.LogInformation("Appointment {AppointmentId} marked as no-show.", appointmentId);
    }

    public async Task<DateTime?> GetNextAvailableDateAsync(Guid agencyId, DateTime preferredDate)
    {
        var maxAttempts = 30; // Try up to 30 days in the future
        var attempts = 0;
        var currentDate = preferredDate;

        while (attempts < maxAttempts)
        {
            if (await HasAvailableSlotAsync(agencyId, currentDate))
            {
                return currentDate;
            }

            currentDate = currentDate.AddDays(1);
            attempts++;
        }

        logger.LogWarning("No available dates found for Agency {AgencyId} within {MaxAttempts} days of {PreferredDate}.", 
            agencyId, maxAttempts, preferredDate);
        return null;
    }

    private async Task<Result> ValidateRescheduleAsync(Guid appointmentId, DateTime newDate)
    {
        // Validate appointment exists
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId);
        if (appointment == null)
        {
            logger.LogWarning("Reschedule failed. Appointment {AppointmentId} not found.", appointmentId);
            return Result.Failure(new[] { "Appointment not found." });
        }

        // Validate appointment is not already cancelled or no-show
        if (appointment.Status == AppointmentStatus.Cancelled || 
            appointment.Status == AppointmentStatus.NoShow)
        {
            logger.LogWarning("Reschedule failed. Appointment {AppointmentId} is {Status}.", appointmentId, appointment.Status);
            return Result.Failure(new[] { $"Cannot reschedule an appointment that is {appointment.Status.ToLower()}." });
        }

        // Validate new date is in future
        if (newDate.Date < DateTime.UtcNow.Date)
        {
            logger.LogWarning("Reschedule failed. New date {Date} is in the past.", newDate);
            return Result.Failure(new[] { "New appointment date cannot be in the past." });
        }

        // Validate new date is not too far in future
        if (newDate.Date > DateTime.UtcNow.AddMonths(6).Date)
        {
            logger.LogWarning("Reschedule failed. New date {Date} is too far in future.", newDate);
            return Result.Failure(new[] { "Appointments cannot be rescheduled more than 6 months in advance." });
        }

        // Get agency for additional validation
        var agency = await agencyService.GetByIdAsync(appointment.AgencyId);
        if (agency == null)
        {
            logger.LogWarning("Reschedule failed. Agency {AgencyId} not found.", appointment.AgencyId);
            return Result.Failure(new[] { "Agency not found." });
        }

        // Validate against agency holidays
        if (agency.Holidays.Any(h => h.Date.Date == newDate.Date))
        {
            var holidayReason = agency.Holidays.First(h => h.Date.Date == newDate.Date).Reason;
            logger.LogWarning("Reschedule failed. {Date} is a holiday for Agency {AgencyName} ({Reason}).", newDate, agency.Name, holidayReason);
            return Result.Failure(new[] { $"Selected date is a holiday: {holidayReason}. Please choose another date." });
        }

        // Validate business hours and slot availability
        var availableSlot = await appointmentSlotRepository.GetAvailableSlotAsync(appointment.AgencyId, newDate);
        if (availableSlot == null)
        {
            logger.LogWarning("Reschedule failed. No available slots for {Date} at Agency {AgencyName}.", newDate, agency.Name);
            return Result.Failure(new[] { "No available slots for the selected date." });
        }

        // Validate appointment capacity (excluding current appointment)
        var existingAppointments = await appointmentRepository.GetByDateAsync(newDate);
        var appointmentsCount = existingAppointments.Count(a => 
            a.AgencyId == appointment.AgencyId && 
            a.Id != appointmentId); // Exclude current appointment from count

        if (appointmentsCount >= agency.MaxAppointmentsPerDay)
        {
            logger.LogWarning("Reschedule failed. Maximum appointments reached for {Date} at Agency {AgencyName}.", newDate, agency.Name);
            return Result.Failure(new[] { "Maximum appointments reached for this date. Please choose another date." });
        }

        return Result.Success;
    }

    public async Task<Result> RescheduleAppointmentAsync(
        Guid appointmentId,
        DateTime newDate,
        CancellationToken cancellationToken = default)
    {
        Result result = null;
        await TransactionHelper.ExecuteInTransactionAsync(async () =>
        {
            try
            {
                // Get appointment
                var appointment = await appointmentRepository.GetByIdAsync(appointmentId);
                if (appointment == null)
                {
                    logger.LogWarning("Rescheduling failed. Appointment {AppointmentId} not found.", appointmentId);
                    result = Result.Failure(new[] { "Appointment not found." });
                    return;
                }

                // Get agency
                var agency = await agencyService.GetByIdAsync(appointment.AgencyId);
                if (agency == null)
                {
                    logger.LogWarning("Rescheduling failed. Agency {AgencyId} not found.", appointment.AgencyId);
                    result = Result.Failure(new[] { "Agency not found." });
                    return;
                }

                // Check if the new date is a holiday
                if (agency.Holidays.Any(h => h.Date.Date == newDate.Date))
                {
                    logger.LogWarning("Rescheduling failed. {Date} is a holiday for Agency {AgencyName}.", newDate, agency.Name);
                    result = Result.Failure(new[] { "Cannot reschedule to a holiday." });
                    return;
                }

                // Get available slot for new date
                var availableSlot = await appointmentSlotRepository.GetAvailableSlotAsync(agency.Id, newDate);
                if (availableSlot == null)
                {
                    logger.LogWarning("Rescheduling failed. No available slots for {Date} at Agency {AgencyName}.", newDate, agency.Name);
                    result = Result.Failure(new[] { "No available slots for the selected date." });
                    return;
                }

                // Get old slot to increase its capacity
                var oldSlots = await appointmentSlotRepository.GetSlotsByAgencyAsync(agency.Id, appointment.Date);
                var oldSlot = oldSlots?.FirstOrDefault(s => s.StartTime == appointment.Date);
                if (oldSlot != null)
                {
                    oldSlot.IncreaseCapacity();
                    appointmentSlotRepository.Update(oldSlot);
                    await appointmentSlotRepository.SaveChangesAsync(cancellationToken);
                }

                // Update appointment
                var rescheduleResult = appointment.Reschedule(newDate);
                if (!rescheduleResult.Succeeded)
                {
                    result = rescheduleResult;
                    return;
                }

                // Update new slot capacity
                availableSlot.DecreaseCapacity();
                appointmentSlotRepository.Update(availableSlot);
                await appointmentSlotRepository.SaveChangesAsync(cancellationToken);

                // Save appointment changes
                appointmentRepository.Update(appointment);
                await appointmentRepository.SaveChangesAsync(cancellationToken);

                // Get agency user for event
                var agencyUser = await agencyUserService.GetByIdAsync(appointment.AgencyUserId);
                if (agencyUser != null)
                {
                    // ✅ Dispatch event
                    await eventDispatcher.Dispatch(new AppointmentEvent(
                        appointment.Id, appointment.Name, appointment.Date, appointment.Status, agency.Name, agency.Email, agencyUser.Email
                    ), cancellationToken);

                    // ✅ Publish event to Kafka
                    await PublishToKafkaAsync(CommonModelConstants.KafkaOperation.Rescheduled, appointment, agency, agencyUser);
                }
                else
                {
                    logger.LogWarning("Agency user not found for appointment {AppointmentId}.", appointmentId);
                }

                logger.LogInformation("Appointment {AppointmentId} rescheduled successfully to {NewDate}.", appointmentId, newDate);
                result = Result.Success;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error rescheduling appointment {AppointmentId}", appointmentId);
                result = Result.Failure(new[] { "An error occurred while rescheduling the appointment." });
            }
        }, System.Transactions.IsolationLevel.RepeatableRead);

        return result;
    }

    public async Task<List<Appointment>> GetUpcomingAppointmentsAsync(Guid agencyId, DateTime fromDate)
    {
        logger.LogInformation("Fetching upcoming appointments for Agency {AgencyId} from {FromDate}.", agencyId, fromDate);

        var appointments = await appointmentRepository.GetAllAsync(); // Fixed method call

        return appointments
            .Where(a => a.AgencyId == agencyId && a.Date >= fromDate)
            .ToList();
    }

    private async Task<Result> ValidateAppointmentCreationAsync(Guid agencyId, string email, string appointmentName, DateTime date)
    {
        // Validate email
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("Appointment creation failed. Email is required.");
            return Result.Failure(new[] { "Email is required." });
        }

        if (!new EmailAddressAttribute().IsValid(email))
        {
            logger.LogWarning("Appointment creation failed. Invalid email format: {Email}", email);
            return Result.Failure(new[] { "Invalid email format." });
        }

        // Get or validate existing agency user
        var agencyUser = await agencyUserService.GetByEmailAsync(email);
        if (agencyUser != null)
        {
            // Get all appointments for this user on the same date
            var existingAppointments = await appointmentRepository.GetByDateAndUserAsync(date, email);
            if (existingAppointments != null)
            {
                // Check for active appointments in the same time slot
                var sameTimeSlotAppointment = existingAppointments.FirstOrDefault(a => 
                    a.Date == date && 
                    a.Status != AppointmentStatus.Cancelled && 
                    a.Status != AppointmentStatus.NoShow);

                if (sameTimeSlotAppointment != null)
                {
                    logger.LogWarning("Appointment creation failed. User {Email} already has an appointment at {Date}.", email, date);
                    return Result.Failure(new[] { "You already have an appointment scheduled for this time slot." });
                }

                // Check for maximum appointments per day
                var activeAppointmentsOnDate = existingAppointments.Count(a => 
                    a.Date.Date == date.Date && 
                    a.Status != AppointmentStatus.Cancelled && 
                    a.Status != AppointmentStatus.NoShow);

                const int MaxAppointmentsPerUserPerDay = 3; // You might want to move this to configuration
                if (activeAppointmentsOnDate >= MaxAppointmentsPerUserPerDay)
                {
                    logger.LogWarning("Appointment creation failed. User {Email} has reached maximum appointments for {Date}.", email, date.Date);
                    return Result.Failure(new[] { $"You cannot book more than {MaxAppointmentsPerUserPerDay} appointments per day." });
                }

                // Check for overlapping appointments (within 2 hours before or after)
                var overlappingAppointment = existingAppointments.FirstOrDefault(a => 
                    a.Status != AppointmentStatus.Cancelled && 
                    a.Status != AppointmentStatus.NoShow &&
                    Math.Abs((a.Date - date).TotalHours) <= 2);

                if (overlappingAppointment != null)
                {
                    logger.LogWarning("Appointment creation failed. User {Email} has an overlapping appointment at {ExistingDate}.", 
                        email, overlappingAppointment.Date);
                    return Result.Failure(new[] { "You have another appointment scheduled within 2 hours of this time slot." });
                }
            }
        }

        // Validate appointment must start at the hour
        if (date.Minute != 0 || date.Second != 0)
        {
            logger.LogWarning("Appointment creation failed. Appointments must start at the hour.");
            return Result.Failure(new[] { "Appointments must start at the hour (e.g., 9:00, 10:00)." });
        }

        // Validate agency exists and is approved
        var agency = await agencyService.GetByIdAsync(agencyId);
        if (agency == null)
        {
            logger.LogWarning("Appointment creation failed. Agency {AgencyId} not found.", agencyId);
            return Result.Failure(new[] { "Invalid agency." });
        }

        if (!agency.IsApproved)
        {
            logger.LogWarning("Appointment creation failed. Agency {AgencyId} is not approved.", agencyId);
            return Result.Failure(new[] { "Agency is not approved for bookings." });
        }

        // Validate appointment name
        if (string.IsNullOrWhiteSpace(appointmentName) || appointmentName.Length < 3 || appointmentName.Length > 100)
        {
            logger.LogWarning("Appointment creation failed. Invalid appointment name: {Name}", appointmentName);
            return Result.Failure(new[] { "Appointment name must be between 3 and 100 characters." });
        }

        // Validate date is in future
        if (date.Date < DateTime.UtcNow.Date)
        {
            logger.LogWarning("Appointment creation failed. Date {Date} is in the past.", date);
            return Result.Failure(new[] { "Appointment date cannot be in the past." });
        }

        // Validate date is not too far in future (e.g., max 6 months)
        if (date.Date > DateTime.UtcNow.AddMonths(6).Date)
        {
            logger.LogWarning("Appointment creation failed. Date {Date} is too far in future.", date);
            return Result.Failure(new[] { "Appointments cannot be booked more than 6 months in advance." });
        }

        // Check if the requested time falls within any existing slot
        var overlappingSlots = await appointmentSlotRepository.GetSlotsByAgencyAsync(agencyId, date);
        if (!overlappingSlots.Any())
        {
            logger.LogWarning("No available slots found for {Date} at Agency {AgencyName}.", date, agency.Name);
            return Result.Failure(new[] { "The requested time is not within any available appointment slot." });
        }

        // Get the specific hour slot for booking
        var availableSlot = await appointmentSlotRepository.GetAvailableSlotAsync(agencyId, date);
        if (availableSlot == null || availableSlot.Capacity <= 0)
        {
            logger.LogWarning("No available capacity for {Date} at Agency {AgencyName}.", date, agency.Name);
            return Result.Failure(new[] { "This time slot is fully booked. Please choose another time." });
        }

        return Result.Success;
    }

    public async Task<Result> CreateAppointmentAsync(
        Guid agencyId,
        string email,
        string appointmentName,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        // Validate input parameters outside transaction
        var validationResult = await ValidateAppointmentCreationAsync(agencyId, email, appointmentName, date);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        Result result = null;
        await TransactionHelper.ExecuteInTransactionAsync(async () =>
        {
            try
            {
                // Get agency
                var agency = await agencyService.GetByIdAsync(agencyId);
                if (agency == null)
                {
                    logger.LogWarning("Appointment creation failed. Agency {AgencyId} not found.", agencyId);
                    result = Result.Failure(new[] { "Agency not found." });
                    return;
                }

                // Get or create agency user with proper validation
                var agencyUser = await agencyUserService.GetByEmailAsync(email);
                if (agencyUser == null)
                {
                    // Create new agency user with customer role
                    var userResult = AgencyUser.Create(
                        agencyId,
                        email,
                        email.Split('@')[0], // Use part before @ as temporary name
                        new[] { CommonModelConstants.AgencyRole.Customer }
                    );

                    if (!userResult.Succeeded)
                    {
                        logger.LogWarning("Failed to create agency user for email {Email}. Errors: {Errors}", 
                            email, string.Join(", ", userResult.Errors));
                        result = Result.Failure(userResult.Errors);
                        return;
                    }

                    agencyUser = userResult.Data;
                    
                    // Set up back-reference for new user
                    var assignResult = agency.AssignUser(agencyUser);
                    if (!assignResult.Succeeded)
                    {
                        result = assignResult;
                        return;
                    }

                    // Save the new user
                    await agencyUserService.AddAsync(agencyUser, cancellationToken);
                    await agencyUserService.SaveChangesAsync(cancellationToken);
                }

                // Get available slot
                var slots = await appointmentSlotRepository.GetSlotsByAgencyAsync(agencyId, date);
                if (slots == null || !slots.Any())
                {
                    logger.LogWarning("Appointment creation failed. No slots available for agency {AgencyId} on {Date}.", 
                        agencyId, date.ToString("yyyy-MM-dd"));
                    result = Result.Failure(new[] { "No appointment slots available for the selected date." });
                    return;
                }

                var availableSlot = slots.FirstOrDefault(s => s.Capacity > 0 && s.StartTime == date);
                if (availableSlot == null)
                {
                    logger.LogWarning("No available slots for {Date} at Agency {AgencyName}.", date, agency.Name);
                    result = Result.Failure(new[] { "No available slots for the selected date." });
                    return;
                }

                // Create appointment using domain factory method
                var appointmentResult = Appointment.Create(
                    agencyId,
                    agencyUser.Id,
                    appointmentName,
                    date,
                    agencyUser);

                if (!appointmentResult.Succeeded)
                {
                    result = appointmentResult;
                    return;
                }

                var appointment = appointmentResult.Data;

                // Update slot capacity
                availableSlot.DecreaseCapacity();

                // Save all changes
                appointmentSlotRepository.Update(availableSlot);
                await appointmentSlotRepository.SaveChangesAsync(cancellationToken);

                await appointmentRepository.AddAsync(appointment, cancellationToken);
                await appointmentRepository.SaveChangesAsync(cancellationToken);

                // ✅ Dispatch event
                await eventDispatcher.Dispatch(new AppointmentEvent(
                    appointment.Id, appointment.Name, appointment.Date, appointment.Status, agency.Name, agency.Email, agencyUser.Email
                ), cancellationToken);

                // ✅ Publish event
                await PublishToKafkaAsync(CommonModelConstants.KafkaOperation.Created, appointment, agency, agencyUser);

                logger.LogInformation("Appointment created successfully for user {Email} at agency {AgencyId}.", email, agencyId);
                result = Result.Success;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating appointment for user {Email} at agency {AgencyId}", email, agencyId);
                result = Result.Failure(new[] { "An error occurred while creating the appointment." });
            }
        }, System.Transactions.IsolationLevel.RepeatableRead);

        return result;
    }

    public async Task<bool> ExistsAsync(Guid appointmentId)
    {
        logger.LogInformation("Checking existence of appointment {AppointmentId}.", appointmentId);

        var appointments = await appointmentRepository.GetAllAsync(); // Fixed method call

        return appointments.Any(a => a.Id == appointmentId);
    }

    private async Task<Result> ValidateCancellationAsync(Guid appointmentId)
    {
        // Validate appointment exists
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId);
        if (appointment == null)
        {
            logger.LogWarning("Cancellation failed. Appointment {AppointmentId} not found.", appointmentId);
            return Result.Failure(new[] { "Appointment not found." });
        }

        // Validate appointment is not already cancelled or no-show
        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            logger.LogWarning("Cancellation failed. Appointment {AppointmentId} is already cancelled.", appointmentId);
            return Result.Failure(new[] { "Appointment is already cancelled." });
        }

        if (appointment.Status == AppointmentStatus.NoShow)
        {
            logger.LogWarning("Cancellation failed. Appointment {AppointmentId} is marked as no-show.", appointmentId);
            return Result.Failure(new[] { "Cannot cancel an appointment that is marked as no-show." });
        }

        // Validate appointment is not in the past
        if (appointment.Date.Date < DateTime.UtcNow.Date)
        {
            logger.LogWarning("Cancellation failed. Appointment {AppointmentId} is in the past.", appointmentId);
            return Result.Failure(new[] { "Cannot cancel an appointment that is in the past." });
        }

        return Result.Success;
    }

    public async Task CancelAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        // Validate cancellation
        var validationResult = await ValidateCancellationAsync(appointmentId);
        if (!validationResult.Succeeded)
        {
            throw new InvalidOperationException(validationResult.Errors.First());
        }

        var appointment = await appointmentRepository.GetByIdAsync(appointmentId);
        if (appointment == null)
        {
            throw new InvalidOperationException("Appointment not found.");
        }

        // Get agency and user before cancellation
        var agency = await agencyService.GetByIdAsync(appointment.AgencyId);
        if (agency == null)
        {
            throw new InvalidOperationException("Agency not found.");
        }

        var agencyUser = await agencyUserService.GetByIdAsync(appointment.AgencyUserId);
        if (agencyUser == null)
        {
            throw new InvalidOperationException("Agency user not found.");
        }

        // Use domain method to cancel
        var cancelResult = appointment.Cancel();
        if (!cancelResult.Succeeded)
        {
            throw new InvalidOperationException(cancelResult.Errors.First());
        }

        // Update slot capacity
        var slots = await appointmentSlotRepository.GetSlotsByAgencyAsync(appointment.AgencyId, appointment.Date);
        var slot = slots?.FirstOrDefault(s => s.StartTime == appointment.Date);
        
        if (slot != null)
        {
            slot.IncreaseCapacity(); // Increase capacity of slot
            await appointmentSlotRepository.UpsertAsync(slot, cancellationToken);
        }

        // Save appointment
        await appointmentRepository.UpsertAsync(appointment, cancellationToken);

        // ✅ Dispatch event
        await eventDispatcher.Dispatch(new AppointmentEvent(
            appointment.Id, appointment.Name, appointment.Date, appointment.Status, agency.Name, agency.Email, agencyUser.Email
        ), cancellationToken);

        // ✅ Publish event
        await PublishToKafkaAsync(CommonModelConstants.KafkaOperation.Cancelled, appointment, agency, agencyUser);

        logger.LogInformation("Appointment {AppointmentId} cancelled successfully.", appointmentId);
    }

    public async Task<bool> IsBookingAllowedAsync(Guid agencyId)
    {
        var agency = await agencyService.GetByIdAsync(agencyId);
        if (agency == null || !agency.IsApproved)
        {
            logger.LogWarning("Booking is not allowed for Agency {AgencyId}. Agency is either not found or not approved.", agencyId);
            return false;
        }

        // Check if the date is a holiday
        if (agency.Holidays.Any(h => h.Date.Date == DateTime.UtcNow.Date))
        {
            logger.LogInformation("Today is a holiday for Agency {AgencyId}.", agencyId);
            return false;
        }

        // Get available slot for today
        var availableSlot = await appointmentSlotRepository.GetAvailableSlotAsync(agencyId, DateTime.UtcNow);
        if (availableSlot == null || availableSlot.Capacity <= 0)
        {
            logger.LogInformation("No available slots for Agency {AgencyId} today.", agencyId);
            return false;
        }

        // Check against max appointments per day
        var appointmentsToday = await appointmentRepository.GetByDateAsync(DateTime.UtcNow);
        var appointmentsCount = appointmentsToday.Count(a => a.AgencyId == agencyId);
        
        if (appointmentsCount >= agency.MaxAppointmentsPerDay)
        {
            logger.LogInformation("Maximum appointments reached for Agency {AgencyId} today.", agencyId);
            return false;
        }

        return true;
    }

    public async Task UpsertAsync(Appointment entity, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving appointment '{AppointmentName}' for Agency {AgencyId}.", entity.Name, entity.AgencyId);
        await appointmentRepository.UpsertAsync(entity, cancellationToken);
    }

    public async Task<List<AppointmentDto>> GetAppointmentsByDateAsync(DateTime date)
    {
        logger.LogInformation("Fetching appointments for date {Date}.", date);

        var appointments = await appointmentRepository.GetByDateAsync(date);
        if (appointments == null || !appointments.Any())
        {
            logger.LogWarning("No appointments found for date {Date}.", date);
            return new List<AppointmentDto>();
        }

        var appointmentDtos = new List<AppointmentDto>();
        foreach (var appointment in appointments)
        {
            var agency = await agencyService.GetByIdAsync(appointment.AgencyId);
            var agencyUser = await agencyUserService.GetByIdAsync(appointment.AgencyUserId);

            if (agency == null || agencyUser == null)
            {
                logger.LogWarning("Incomplete data for appointment {AppointmentId}. Skipping.", appointment.Id);
                continue;
            }

            appointmentDtos.Add(new AppointmentDto
            {
                AppointmentId = appointment.Id,
                AppointmentName = appointment.Name,
                Date = appointment.Date,
                Status = appointment.Status,
                AgencyName = agency.Name,
                AgencyEmail = agency.Email,
                UserEmail = agencyUser.Email
            });
        }

        logger.LogInformation("Successfully fetched {AppointmentCount} appointments for date {AppointmentDate}.", appointmentDtos.Count, date);
        return appointmentDtos;
    }

    public async Task<List<AppointmentDto>> GetAppointmentsByDateForUserAsync(DateTime date, string userEmail)
    {
        logger.LogInformation("Fetching appointments for user {UserEmail} on date {Date}.", userEmail, date);

        var appointments = await appointmentRepository.GetByDateAndUserAsync(date, userEmail);
        if (appointments == null || !appointments.Any())
        {
            logger.LogWarning("No appointments found for user {UserEmail} on date {Date}.", userEmail, date);
            return new List<AppointmentDto>();
        }

        var appointmentDtos = new List<AppointmentDto>();
        foreach (var appointment in appointments)
        {
            var agency = await agencyService.GetByIdAsync(appointment.AgencyId);
            var agencyUser = await agencyUserService.GetByIdAsync(appointment.AgencyUserId);

            if (agency == null || agencyUser == null)
            {
                logger.LogWarning("Incomplete data for appointment {AppointmentId}. Skipping.", appointment.Id);
                continue;
            }

            appointmentDtos.Add(new AppointmentDto
            {
                AppointmentId = appointment.Id,
                AppointmentName = appointment.Name,
                Date = appointment.Date,
                Status = appointment.Status,
                AgencyName = agency.Name,
                AgencyEmail = agency.Email,
                UserEmail = agencyUser.Email
            });
        }

        logger.LogInformation("Successfully fetched {AppointmentCount} appointments for user {UserEmail} on date {Date}.", appointmentDtos.Count, userEmail, date);
        return appointmentDtos;
    }
}