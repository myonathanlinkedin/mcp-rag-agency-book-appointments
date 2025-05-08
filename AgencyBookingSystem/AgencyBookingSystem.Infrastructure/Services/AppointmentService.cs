using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository appointmentRepository;
    private readonly IAgencyService agencyService;
    private readonly IAgencyUserService agencyUserService;
    private readonly IEventDispatcher eventDispatcher; // ✅ Keeping event dispatcher
    private readonly ILogger<AppointmentService> logger;
    private readonly IProducer<Null, string> kafkaProducer;
    private readonly string kafkaTopic;

    public AppointmentService(IAppointmentRepository appointmentRepository, IAgencyUserService agencyUserService,
                              IAgencyService agencyService, IEventDispatcher eventDispatcher,
                              ILogger<AppointmentService> logger, IProducer<Null, string> kafkaProducer,
                              string kafkaTopic)
    {
        this.appointmentRepository = appointmentRepository;
        this.agencyService = agencyService;
        this.agencyUserService = agencyUserService;
        this.eventDispatcher = eventDispatcher;
        this.logger = logger;
        this.kafkaProducer = kafkaProducer;
        this.kafkaTopic = kafkaTopic;
    }

    private async Task PublishToKafkaAsync(string action, Appointment appointment, Agency agency, AgencyUser agencyUser)
    {
        var message = new
        {
            Action = action,
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
        await kafkaProducer.ProduceAsync(kafkaTopic, new Message<Null, string> { Value = messageJson });

        Console.WriteLine($"Published {action} event for Appointment ID {appointment.Id} to Kafka topic {kafkaTopic}.");
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
        var appointmentsOnDate = await GetAppointmentsByAgencyAsync(agencyId);
        return appointmentsOnDate.Count < agency.MaxAppointmentsPerDay;
    }

    public async Task HandleNoShowAsync(Guid appointmentId)
    {
        var appointment = await GetByIdAsync(appointmentId);
        if (appointment == null || appointment.Status != AppointmentStatus.Pending) return;

        appointment.Status = AppointmentStatus.Expired;
        await appointmentRepository.Save(appointment);
        logger.LogWarning("Appointment {AppointmentId} marked as Expired due to no-show.", appointmentId);
    }

    public async Task<DateTime?> GetNextAvailableDateAsync(Guid agencyId, DateTime preferredDate)
    {
        while (!await HasAvailableSlotAsync(agencyId, preferredDate))
        {
            preferredDate = preferredDate.AddDays(1);
        }

        return preferredDate;
    }

    public async Task<Result> RescheduleAppointmentAsync(Guid appointmentId, DateTime newDate, CancellationToken cancellationToken = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId);
        if (appointment == null)
        {
            logger.LogWarning("Reschedule failed. Appointment {AppointmentId} does not exist.", appointmentId);
            return Result.Failure(new[] { "Appointment does not exist." });
        }

        if (!await HasAvailableSlotAsync(appointment.AgencyId, newDate))
        {
            logger.LogWarning("Reschedule failed. No available slots for {NewDate}.", newDate);
            return Result.Failure(new[] { "No available slots for the new date." });
        }

        appointment.Date = newDate;
        await appointmentRepository.Save(appointment, cancellationToken);

        var agencyUser = await agencyUserService.GetByIdAsync(appointment.AgencyUserId);
        var agency = await agencyService.GetByIdAsync(appointment.AgencyId);

        if (agency == null || agencyUser == null)
        {
            logger.LogWarning("Reschedule failed. No agency/user found for appointment {AppointmentId}.", appointmentId);
            return Result.Failure(new[] { "Invalid agency or user." });
        }

        // ✅ Dispatch event
        await eventDispatcher.Dispatch(new AppointmentEvent(
            appointment.Id, appointment.Name, appointment.Date, appointment.Status, agency.Name, agency.Email, agencyUser.Email
        ));

        // ✅ Publish reschedule event to Kafka
        await PublishToKafkaAsync("UPDATE", appointment, agency, agencyUser);

        logger.LogInformation("Appointment '{AppointmentName}' successfully rescheduled for agency {AgencyName}, user {UserEmail}.",
            appointment.Name, agency.Name, agencyUser.Email);

        return Result.Success;
    }

    public async Task<List<Appointment>> GetUpcomingAppointmentsAsync(Guid agencyId, DateTime fromDate)
    {
        logger.LogInformation("Fetching upcoming appointments for Agency {AgencyId} from {FromDate}.", agencyId, fromDate);

        var appointments = await appointmentRepository.GetAllAsync(); // Fixed method call

        return appointments
            .Where(a => a.AgencyId == agencyId && a.Date >= fromDate)
            .ToList();
    }

    public async Task<Result> CreateAppointmentAsync(Guid agencyId, string email, string appointmentName, DateTime date, CancellationToken cancellationToken = default)
    {
        var agency = await agencyService.GetByIdAsync(agencyId);
        if (agency == null)
        {
            logger.LogWarning("Appointment creation failed. Agency {AgencyId} does not exist.", agencyId);
            return Result.Failure(new[] { "Agency does not exist." });
        }

        var agencyUser = await agencyUserService.GetByEmailAsync(email);
        if (agencyUser == null)
        {
            logger.LogWarning("Appointment creation failed. No agency user found for email {Email}.", email);
            return Result.Failure(new[] { "No agency user found for this email." });
        }

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            AgencyId = agencyId,
            AgencyUserId = agencyUser.Id,
            Date = date,
            Name = appointmentName,
            Status = AppointmentStatus.Pending,
            Token = Guid.NewGuid().ToString()
        };

        await appointmentRepository.Save(appointment, cancellationToken);

        // Dispatch event instead of direct notification
        await eventDispatcher.Dispatch(new AppointmentEvent(
            appointment.Id,
            appointment.Name,
            appointment.Date,
            appointment.Status,
            agency.Name,
            agency.Email,
            agencyUser.Email
        ));

        logger.LogInformation("Appointment '{AppointmentName}' created successfully for Agency {AgencyName}, User {UserEmail}.",
            appointment.Name, agency.Name, agencyUser.Email);

        return Result.Success;
    }


    public async Task<bool> ExistsAsync(Guid appointmentId)
    {
        logger.LogInformation("Checking existence of appointment {AppointmentId}.", appointmentId);

        var appointments = await appointmentRepository.GetAllAsync(); // Fixed method call

        return appointments.Any(a => a.Id == appointmentId);
    }

    public async Task CancelAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId);
        if (appointment == null)
        {
            logger.LogWarning("Cancellation failed. Appointment {AppointmentId} does not exist.", appointmentId);
            return;
        }

        appointment.Status = "Canceled";
        await SaveAsync(appointment, cancellationToken);

        var agencyUser = await agencyUserService.GetByIdAsync(appointment.AgencyUserId);
        var agency = await agencyService.GetByIdAsync(appointment.AgencyId);

        if (agency == null || agencyUser == null)
        {
            logger.LogWarning("Cancellation failed. No agency/user found for appointment {AppointmentId}.", appointmentId);
            return;
        }

        // ✅ Dispatch event
        await eventDispatcher.Dispatch(new AppointmentEvent(
            appointment.Id, appointment.Name, appointment.Date, appointment.Status, agency.Name, agency.Email, agencyUser.Email
        ));

        // ✅ Publish cancellation event to Kafka
        await PublishToKafkaAsync("DELETE", appointment, agency, agencyUser);

        logger.LogInformation("Appointment '{AppointmentName}' canceled successfully for Agency {AgencyName}, User {UserEmail}.",
            appointment.Name, agency.Name, agencyUser.Email);
    }

    public async Task<bool> IsBookingAllowedAsync(Guid agencyId)
    {
        var agency = await agencyService.GetByIdAsync(agencyId);
        if (agency == null || !agency.IsApproved)
        {
            logger.LogWarning("Booking is not allowed for Agency {AgencyId}.", agencyId);
            return false;
        }

        var appointments = await appointmentRepository.GetAllAsync(); // Fixed method call

        var appointmentsCount = appointments
            .Where(a => a.AgencyId == agencyId && a.Date.Date == DateTime.UtcNow.Date)
            .Count();

        return appointmentsCount < agency.MaxAppointmentsPerDay;
    }

    public async Task SaveAsync(Appointment entity, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving appointment '{AppointmentName}' for Agency {AgencyId}.", entity.Name, entity.AgencyId);
        await appointmentRepository.Save(entity, cancellationToken);
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