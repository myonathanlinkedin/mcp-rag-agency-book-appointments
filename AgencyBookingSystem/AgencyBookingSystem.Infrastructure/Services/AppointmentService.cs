using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Data;

public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentUnitOfWork unitOfWork;
    private readonly IEventDispatcher eventDispatcher;
    private readonly ILogger<AppointmentService> logger;
    private readonly IProducer<Null, string> kafkaProducer;
    private readonly ApplicationSettings settings;

    public AppointmentService(
        IAppointmentUnitOfWork unitOfWork,
        IEventDispatcher eventDispatcher,
        ILogger<AppointmentService> logger,
        IProducer<Null, string> kafkaProducer,
        ApplicationSettings settings)
    {
        this.unitOfWork = unitOfWork;
        this.eventDispatcher = eventDispatcher;
        this.logger = logger;
        this.kafkaProducer = kafkaProducer;
        this.settings = settings;
    }

    private async Task PublishToKafkaAsync(string action, Appointment appointment, Agency agency, AgencyUser agencyUser)
    {
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

        var crudAction = action switch
        {
            CommonModelConstants.KafkaOperation.Created => CommonModelConstants.KafkaOperation.Insert,
            CommonModelConstants.KafkaOperation.Rescheduled => CommonModelConstants.KafkaOperation.Update,
            CommonModelConstants.KafkaOperation.NoShow => CommonModelConstants.KafkaOperation.Update,
            CommonModelConstants.KafkaOperation.Cancelled => CommonModelConstants.KafkaOperation.Update,
            _ => action
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

        logger.LogInformation("Published {Action} event for Appointment ID {AppointmentId} to Kafka topic {KafkaTopic}.", 
            action, appointment.Id, settings.Kafka.Topic);
    }

    public async Task<Appointment?> GetByIdAsync(Guid id)
    {
        logger.LogInformation("Fetching appointment with ID: {Id}", id);
        return await unitOfWork.Appointments.GetByIdAsync(id);
    }

    public async Task<List<Appointment>> GetAllAsync()
    {
        logger.LogInformation("Fetching all appointments.");
        return await unitOfWork.Appointments.GetAllAsync();
    }

    public async Task<List<Appointment>> GetAppointmentsByAgencyAsync(Guid agencyId)
    {
        logger.LogInformation("Fetching appointments for Agency ID: {AgencyId}", agencyId);
        return await unitOfWork.Appointments.GetAppointmentsByAgencyAsync(agencyId);
    }

    public async Task<bool> HasAvailableSlotAsync(Guid agencyId, DateTime date)
    {
        var agency = await unitOfWork.Agencies.GetByIdAsync(agencyId);
        if (agency == null)
        {
            logger.LogWarning("Agency {AgencyId} not found when checking slot availability.", agencyId);
            return false;
        }

        if (agency.Holidays != null && agency.Holidays.Any(h => h.Date.Date == date.Date))
        {
            logger.LogInformation("Date {Date} is a holiday for Agency {AgencyId}.", date, agencyId);
            return false;
        }

        var availableSlot = await unitOfWork.AppointmentSlots.GetAvailableSlotAsync(agencyId, date);
        if (availableSlot == null || availableSlot.Capacity <= 0)
        {
            logger.LogInformation("No available slots for Agency {AgencyId} on {Date}.", agencyId, date);
            return false;
        }

        var appointmentsOnDate = await unitOfWork.Appointments.GetByDateAsync(date);
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
        var appointment = await unitOfWork.Appointments.GetByIdAsync(appointmentId);
        if (appointment == null)
        {
            throw new InvalidOperationException("Appointment not found.");
        }

        var agency = await unitOfWork.Agencies.GetByIdAsync(appointment.AgencyId);
        if (agency == null)
        {
            throw new InvalidOperationException("Agency not found.");
        }

        var agencyUser = await unitOfWork.AgencyUsers.GetByIdAsync(appointment.AgencyUserId);
        if (agencyUser == null)
        {
            throw new InvalidOperationException("Agency user not found.");
        }

        var noShowResult = appointment.MarkAsNoShow();
        if (!noShowResult.Succeeded)
        {
            throw new InvalidOperationException(noShowResult.Errors.First());
        }

        await unitOfWork.Appointments.UpsertAsync(appointment);

        await eventDispatcher.Dispatch(new AppointmentEvent(
            appointment.Id, appointment.Name, appointment.Date, appointment.Status, agency.Name, agency.Email, agencyUser.Email
        ), default);

        await PublishToKafkaAsync(CommonModelConstants.KafkaOperation.NoShow, appointment, agency, agencyUser);

        logger.LogInformation("Appointment {AppointmentId} marked as no-show.", appointmentId);
    }

    public async Task<DateTime?> GetNextAvailableDateAsync(Guid agencyId, DateTime preferredDate)
    {
        var maxAttempts = 30;
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
        var appointment = await unitOfWork.Appointments.GetByIdAsync(appointmentId);
        if (appointment == null)
        {
            logger.LogWarning("Reschedule failed. Appointment {AppointmentId} not found.", appointmentId);
            return Result.Failure(new[] { "Appointment not found." });
        }

        if (appointment.Status == AppointmentStatus.Cancelled || 
            appointment.Status == AppointmentStatus.NoShow)
        {
            logger.LogWarning("Reschedule failed. Appointment {AppointmentId} is {Status}.", appointmentId, appointment.Status);
            return Result.Failure(new[] { $"Cannot reschedule an appointment that is {appointment.Status.ToLower()}." });
        }

        if (newDate.Date < DateTime.UtcNow.Date)
        {
            logger.LogWarning("Reschedule failed. New date {Date} is in the past.", newDate);
            return Result.Failure(new[] { "New appointment date cannot be in the past." });
        }

        if (newDate.Date > DateTime.UtcNow.AddMonths(6).Date)
        {
            logger.LogWarning("Reschedule failed. New date {Date} is too far in future.", newDate);
            return Result.Failure(new[] { "Appointments cannot be rescheduled more than 6 months in advance." });
        }

        var agency = await unitOfWork.Agencies.GetByIdAsync(appointment.AgencyId);
        if (agency == null)
        {
            logger.LogWarning("Reschedule failed. Agency {AgencyId} not found.", appointment.AgencyId);
            return Result.Failure(new[] { "Agency not found." });
        }

        if (agency.Holidays.Any(h => h.Date.Date == newDate.Date))
        {
            var holidayReason = agency.Holidays.First(h => h.Date.Date == newDate.Date).Reason;
            logger.LogWarning("Reschedule failed. {Date} is a holiday for Agency {AgencyName} ({Reason}).", newDate, agency.Name, holidayReason);
            return Result.Failure(new[] { $"Cannot reschedule to {newDate.Date:d} as it is a holiday ({holidayReason})." });
        }

        var hasAvailableSlot = await HasAvailableSlotAsync(appointment.AgencyId, newDate);
        if (!hasAvailableSlot)
        {
            logger.LogWarning("Reschedule failed. No available slots for Agency {AgencyId} on {Date}.", appointment.AgencyId, newDate);
            return Result.Failure(new[] { "No available slots for the selected date." });
        }

        var isBookingAllowed = await IsBookingAllowedAsync(appointment.AgencyId);
        if (!isBookingAllowed)
        {
            logger.LogWarning("Reschedule failed. Agency {AgencyId} does not allow bookings.", appointment.AgencyId);
            return Result.Failure(new[] { "This agency is not currently accepting appointments." });
        }

        return Result.Success;
    }

    public async Task<Result> RescheduleAppointmentAsync(
        Guid appointmentId,
        DateTime newDate,
        CancellationToken cancellationToken = default)
    {
        Result result = null;
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var validationResult = await ValidateRescheduleAsync(appointmentId, newDate);
            if (!validationResult.Succeeded)
            {
                result = validationResult;
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return result;
            }

            var appointment = await unitOfWork.Appointments.GetByIdAsync(appointmentId);
            var agency = await unitOfWork.Agencies.GetByIdAsync(appointment.AgencyId);

            var availableSlot = await unitOfWork.AppointmentSlots.GetAvailableSlotAsync(agency.Id, newDate);
            if (availableSlot == null)
            {
                logger.LogWarning("Rescheduling failed. No available slots for {Date} at Agency {AgencyName}.", newDate, agency.Name);
                result = Result.Failure(new[] { "No available slots for the selected date." });
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return result;
            }

            var oldSlots = await unitOfWork.AppointmentSlots.GetSlotsByAgencyAsync(agency.Id, appointment.Date);
            var oldSlot = oldSlots?.FirstOrDefault(s => s.StartTime == appointment.Date);
            if (oldSlot != null)
            {
                oldSlot.IncreaseCapacity();
                unitOfWork.AppointmentSlots.Update(oldSlot);
            }

            var rescheduleResult = appointment.Reschedule(newDate);
            if (!rescheduleResult.Succeeded)
            {
                result = rescheduleResult;
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return result;
            }

            availableSlot.DecreaseCapacity();
            unitOfWork.AppointmentSlots.Update(availableSlot);
            unitOfWork.Appointments.Update(appointment);

            var agencyUser = await unitOfWork.AgencyUsers.GetByIdAsync(appointment.AgencyUserId);
            if (agencyUser != null)
            {
                await eventDispatcher.Dispatch(new AppointmentEvent(
                    appointment.Id, appointment.Name, appointment.Date, appointment.Status, agency.Name, agency.Email, agencyUser.Email
                ), cancellationToken);

                await PublishToKafkaAsync(CommonModelConstants.KafkaOperation.Rescheduled, appointment, agency, agencyUser);
            }
            else
            {
                logger.LogWarning("Agency user not found for appointment {AppointmentId}.", appointmentId);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation("Appointment {AppointmentId} rescheduled successfully to {NewDate}.", appointmentId, newDate);
            result = Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error rescheduling appointment {AppointmentId}", appointmentId);
            result = Result.Failure(new[] { "An error occurred while rescheduling the appointment." });
        }

        return result;
    }

    public async Task<List<Appointment>> GetUpcomingAppointmentsAsync(Guid agencyId, DateTime fromDate)
    {
        logger.LogInformation("Fetching upcoming appointments for Agency {AgencyId} from {FromDate}.", agencyId, fromDate);

        var appointments = await unitOfWork.Appointments.GetAllAsync();

        return appointments
            .Where(a => a.AgencyId == agencyId && a.Date >= fromDate)
            .ToList();
    }

    private async Task<Result> ValidateAppointmentCreationAsync(Guid agencyId, string email, string appointmentName, DateTime date)
    {
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

        var agencyUser = await unitOfWork.AgencyUsers.GetByEmailAsync(email);
        if (agencyUser != null)
        {
            var existingAppointments = await unitOfWork.Appointments.GetByDateAndUserAsync(date, email);
            if (existingAppointments != null)
            {
                var sameTimeSlotAppointment = existingAppointments.FirstOrDefault(a => 
                    a.Date == date && 
                    a.Status != AppointmentStatus.Cancelled && 
                    a.Status != AppointmentStatus.NoShow);

                if (sameTimeSlotAppointment != null)
                {
                    logger.LogWarning("Appointment creation failed. User {Email} already has an appointment at {Date}.", email, date);
                    return Result.Failure(new[] { "You already have an appointment scheduled for this time slot." });
                }

                var activeAppointmentsOnDate = existingAppointments.Count(a => 
                    a.Date.Date == date.Date && 
                    a.Status != AppointmentStatus.Cancelled && 
                    a.Status != AppointmentStatus.NoShow);

                const int MaxAppointmentsPerUserPerDay = 3;
                if (activeAppointmentsOnDate >= MaxAppointmentsPerUserPerDay)
                {
                    logger.LogWarning("Appointment creation failed. User {Email} has reached maximum appointments for {Date}.", email, date.Date);
                    return Result.Failure(new[] { $"You cannot book more than {MaxAppointmentsPerUserPerDay} appointments per day." });
                }

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

        if (date.Minute != 0 || date.Second != 0)
        {
            logger.LogWarning("Appointment creation failed. Appointments must start at the hour.");
            return Result.Failure(new[] { "Appointments must start at the hour (e.g., 9:00, 10:00)." });
        }

        var agency = await unitOfWork.Agencies.GetByIdAsync(agencyId);
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

        if (string.IsNullOrWhiteSpace(appointmentName) || appointmentName.Length < 3 || appointmentName.Length > 100)
        {
            logger.LogWarning("Appointment creation failed. Invalid appointment name: {Name}", appointmentName);
            return Result.Failure(new[] { "Appointment name must be between 3 and 100 characters." });
        }

        if (date.Date < DateTime.UtcNow.Date)
        {
            logger.LogWarning("Appointment creation failed. Date {Date} is in the past.", date);
            return Result.Failure(new[] { "Appointment date cannot be in the past." });
        }

        if (date.Date > DateTime.UtcNow.AddMonths(6).Date)
        {
            logger.LogWarning("Appointment creation failed. Date {Date} is too far in future.", date);
            return Result.Failure(new[] { "Appointments cannot be booked more than 6 months in advance." });
        }

        var overlappingSlots = await unitOfWork.AppointmentSlots.GetSlotsByAgencyAsync(agencyId, date);
        if (!overlappingSlots.Any())
        {
            logger.LogWarning("No available slots found for {Date} at Agency {AgencyName}.", date, agency.Name);
            return Result.Failure(new[] { "The requested time is not within any available appointment slot." });
        }

        var availableSlot = await unitOfWork.AppointmentSlots.GetAvailableSlotAsync(agencyId, date);
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
        var validationResult = await ValidateAppointmentCreationAsync(agencyId, email, appointmentName, date);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        Result result = null;
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var agency = await unitOfWork.Agencies.GetByIdAsync(agencyId);
            if (agency == null)
            {
                logger.LogWarning("Appointment creation failed. Agency {AgencyId} not found.", agencyId);
                result = Result.Failure(new[] { "Agency not found." });
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return result;
            }

            var agencyUser = await unitOfWork.AgencyUsers.GetByEmailAsync(email);
            if (agencyUser == null)
            {
                var userResult = AgencyUser.Create(
                    agencyId,
                    email,
                    email.Split('@')[0],
                    new[] { CommonModelConstants.AgencyRole.Customer }
                );

                if (!userResult.Succeeded)
                {
                    logger.LogWarning("Failed to create agency user for email {Email}. Errors: {Errors}", 
                        email, string.Join(", ", userResult.Errors));
                    result = Result.Failure(userResult.Errors);
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return result;
                }

                agencyUser = userResult.Data;
                
                var assignResult = agency.AssignUser(agencyUser);
                if (!assignResult.Succeeded)
                {
                    result = assignResult;
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return result;
                }

                await unitOfWork.AgencyUsers.AddAsync(agencyUser, cancellationToken);
            }

            var slots = await unitOfWork.AppointmentSlots.GetSlotsByAgencyAsync(agencyId, date);
            if (slots == null || !slots.Any())
            {
                logger.LogWarning("Appointment creation failed. No slots available for agency {AgencyId} on {Date}.", 
                    agencyId, date.ToString("yyyy-MM-dd"));
                result = Result.Failure(new[] { "No appointment slots available for the selected date." });
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return result;
            }

            var availableSlot = slots.FirstOrDefault(s => s.Capacity > 0 && s.StartTime == date);
            if (availableSlot == null)
            {
                logger.LogWarning("No available slots for {Date} at Agency {AgencyName}.", date, agency.Name);
                result = Result.Failure(new[] { "No available slots for the selected date." });
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return result;
            }

            var appointmentResult = Appointment.Create(
                agencyId,
                agencyUser.Id,
                appointmentName,
                date,
                agencyUser);

            if (!appointmentResult.Succeeded)
            {
                result = appointmentResult;
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return result;
            }

            var appointment = appointmentResult.Data;

            availableSlot.DecreaseCapacity();
            unitOfWork.AppointmentSlots.Update(availableSlot);
            await unitOfWork.Appointments.AddAsync(appointment, cancellationToken);

            await eventDispatcher.Dispatch(new AppointmentEvent(
                appointment.Id, appointment.Name, appointment.Date, appointment.Status, agency.Name, agency.Email, agencyUser.Email
            ), cancellationToken);

            await PublishToKafkaAsync(CommonModelConstants.KafkaOperation.Created, appointment, agency, agencyUser);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation("Appointment created successfully for user {Email} at agency {AgencyId}.", email, agencyId);
            result = Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error creating appointment for user {Email} at agency {AgencyId}", email, agencyId);
            result = Result.Failure(new[] { "An error occurred while creating the appointment." });
        }

        return result;
    }

    public async Task<bool> ExistsAsync(Guid appointmentId)
    {
        logger.LogInformation("Checking existence of appointment {AppointmentId}.", appointmentId);
        var appointments = await unitOfWork.Appointments.GetAllAsync();
        return appointments.Any(a => a.Id == appointmentId);
    }

    private async Task<Result> ValidateCancellationAsync(Guid appointmentId)
    {
        var appointment = await unitOfWork.Appointments.GetByIdAsync(appointmentId);
        if (appointment == null)
        {
            logger.LogWarning("Cancellation failed. Appointment {AppointmentId} not found.", appointmentId);
            return Result.Failure(new[] { "Appointment not found." });
        }

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

        if (appointment.Date.Date < DateTime.UtcNow.Date)
        {
            logger.LogWarning("Cancellation failed. Appointment {AppointmentId} is in the past.", appointmentId);
            return Result.Failure(new[] { "Cannot cancel an appointment that is in the past." });
        }

        return Result.Success;
    }

    public async Task CancelAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var validationResult = await ValidateCancellationAsync(appointmentId);
        if (!validationResult.Succeeded)
        {
            throw new InvalidOperationException(validationResult.Errors.First());
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var appointment = await unitOfWork.Appointments.GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                throw new InvalidOperationException("Appointment not found.");
            }

            var agency = await unitOfWork.Agencies.GetByIdAsync(appointment.AgencyId);
            if (agency == null)
            {
                throw new InvalidOperationException("Agency not found.");
            }

            var agencyUser = await unitOfWork.AgencyUsers.GetByIdAsync(appointment.AgencyUserId);
            if (agencyUser == null)
            {
                throw new InvalidOperationException("Agency user not found.");
            }

            var cancelResult = appointment.Cancel();
            if (!cancelResult.Succeeded)
            {
                throw new InvalidOperationException(cancelResult.Errors.First());
            }

            var slots = await unitOfWork.AppointmentSlots.GetSlotsByAgencyAsync(appointment.AgencyId, appointment.Date);
            var slot = slots?.FirstOrDefault(s => s.StartTime == appointment.Date);
            
            if (slot != null)
            {
                slot.IncreaseCapacity();
                unitOfWork.AppointmentSlots.Update(slot);
            }

            await unitOfWork.Appointments.UpsertAsync(appointment, cancellationToken);

            await eventDispatcher.Dispatch(new AppointmentEvent(
                appointment.Id, appointment.Name, appointment.Date, appointment.Status, agency.Name, agency.Email, agencyUser.Email
            ), cancellationToken);

            await PublishToKafkaAsync(CommonModelConstants.KafkaOperation.Cancelled, appointment, agency, agencyUser);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation("Appointment {AppointmentId} cancelled successfully.", appointmentId);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error cancelling appointment {AppointmentId}", appointmentId);
            throw;
        }
    }

    public async Task<bool> IsBookingAllowedAsync(Guid agencyId)
    {
        var agency = await unitOfWork.Agencies.GetByIdAsync(agencyId);
        if (agency == null || !agency.IsApproved)
        {
            logger.LogWarning("Booking is not allowed for Agency {AgencyId}. Agency is either not found or not approved.", agencyId);
            return false;
        }

        if (agency.Holidays.Any(h => h.Date.Date == DateTime.UtcNow.Date))
        {
            logger.LogInformation("Today is a holiday for Agency {AgencyId}.", agencyId);
            return false;
        }

        var availableSlot = await unitOfWork.AppointmentSlots.GetAvailableSlotAsync(agencyId, DateTime.UtcNow);
        if (availableSlot == null || availableSlot.Capacity <= 0)
        {
            logger.LogInformation("No available slots for Agency {AgencyId} today.", agencyId);
            return false;
        }

        var appointmentsToday = await unitOfWork.Appointments.GetByDateAsync(DateTime.UtcNow);
        var appointmentsCount = appointmentsToday.Count(a => a.AgencyId == agencyId);
        
        if (appointmentsCount >= agency.MaxAppointmentsPerDay)
        {
            logger.LogInformation("Maximum appointments reached for Agency {AgencyId} today.", agencyId);
            return false;
        }

        return true;
    }

    public async Task<List<AppointmentDto>> GetAppointmentsByDateAsync(DateTime date)
    {
        logger.LogInformation("Fetching appointments for date {Date}.", date);

        var appointments = await unitOfWork.Appointments.GetByDateAsync(date);
        if (appointments == null || !appointments.Any())
        {
            logger.LogWarning("No appointments found for date {Date}.", date);
            return new List<AppointmentDto>();
        }

        var appointmentDtos = new List<AppointmentDto>();
        foreach (var appointment in appointments)
        {
            var agency = await unitOfWork.Agencies.GetByIdAsync(appointment.AgencyId);
            var agencyUser = await unitOfWork.AgencyUsers.GetByIdAsync(appointment.AgencyUserId);

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

        var appointments = await unitOfWork.Appointments.GetByDateAndUserAsync(date, userEmail);
        if (appointments == null || !appointments.Any())
        {
            logger.LogWarning("No appointments found for user {UserEmail} on date {Date}.", userEmail, date);
            return new List<AppointmentDto>();
        }

        var appointmentDtos = new List<AppointmentDto>();
        foreach (var appointment in appointments)
        {
            var agency = await unitOfWork.Agencies.GetByIdAsync(appointment.AgencyId);
            var agencyUser = await unitOfWork.AgencyUsers.GetByIdAsync(appointment.AgencyUserId);

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

    public async Task UpsertAsync(Appointment entity, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving appointment '{AppointmentName}' for Agency {AgencyId}.", entity.Name, entity.AgencyId);
        
        try
        {
            await unitOfWork.Appointments.UpsertAsync(entity, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error upserting appointment {AppointmentId}", entity.Id);
            throw;
        }
    }
}