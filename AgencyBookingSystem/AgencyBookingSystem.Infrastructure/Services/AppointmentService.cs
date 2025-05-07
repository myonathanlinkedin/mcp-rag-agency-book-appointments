using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository appointmentRepository;
    private readonly IAgencyService agencyService;
    private readonly INotificationService notificationService;
    private readonly ILogger<AppointmentService> logger;

    public AppointmentService(IAppointmentRepository appointmentRepository, IAgencyService agencyService, INotificationService notificationService, ILogger<AppointmentService> logger)
    {
        this.appointmentRepository = appointmentRepository;
        this.agencyService = agencyService;
        this.notificationService = notificationService;
        this.logger = logger;
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

    public async Task<Result> CreateAppointmentAsync(Guid agencyId, Guid agencyUserId, DateTime date, CancellationToken cancellationToken = default)
    {
        if (!await HasAvailableSlotAsync(agencyId, date))
        {
            logger.LogWarning("No available slots for Agency {AgencyId} on {Date}.", agencyId, date);
            return Result.Failure(new[] { "No available slots." });
        }

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            AgencyId = agencyId,
            AgencyUserId = agencyUserId,
            Date = date,
            Status = AppointmentStatus.Pending,
            Token = Guid.NewGuid().ToString()
        };

        await appointmentRepository.Save(appointment, cancellationToken);
        await notificationService.SendNotificationAsync("customer@example.com", "Appointment Confirmation", $"Your appointment on {date} is confirmed.");

        logger.LogInformation("Appointment created successfully for Agency {AgencyId}, User {AgencyUserId}.", agencyId, agencyUserId);
        return Result.Success;
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

    public async Task<Result> ForceCreateAppointmentAsync(string email, string appointmentName, DateTime date, CancellationToken cancellationToken = default)
    {
        var agency = await agencyService.GetByEmailAsync(email);
        if (agency == null)
        {
            logger.LogWarning("Admin override failed. No agency found for email {Email}.", email);
            return Result.Failure(new[] { "No agency found for this email." });
        }

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            AgencyId = agency.Id,
            AgencyUserId = agency.Id, // Assuming agency handles users internally
            Date = date,
            Name = appointmentName, // Added appointment name
            Status = AppointmentStatus.Confirmed,
            Token = Guid.NewGuid().ToString()
        };

        await appointmentRepository.Save(appointment, cancellationToken);
        await notificationService.SendNotificationAsync("admin@example.com", "Override Alert", $"An appointment '{appointmentName}' was forcibly created on {date}.");

        logger.LogInformation("Admin override: Appointment '{AppointmentName}' forced for Agency {AgencyId}.", appointmentName, agency.Id);
        return Result.Success;
    }

    public async Task<Result> RescheduleAppointmentAsync(Guid appointmentId, DateTime newDate, CancellationToken cancellationToken = default)
    {
        var appointment = await GetByIdAsync(appointmentId);
        if (appointment == null)
        {
            logger.LogWarning("Reschedule failed. Appointment {AppointmentId} does not exist.", appointmentId);
            return Result.Failure(new[] { "Appointment does not exist." });
        }

        if (!await HasAvailableSlotAsync(appointment.AgencyId, newDate))
        {
            logger.LogWarning("Reschedule failed. No available slots on {NewDate}.", newDate);
            return Result.Failure(new[] { "No available slots for the new date." });
        }

        appointment.Date = newDate;
        await appointmentRepository.Save(appointment, cancellationToken);
        await notificationService.SendNotificationAsync("customer@example.com", "Appointment Rescheduled", $"Your appointment has been rescheduled to {newDate}.");

        logger.LogInformation("Appointment {AppointmentId} successfully rescheduled to {NewDate}.", appointmentId, newDate);
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

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            AgencyId = agencyId,
            Date = date,
            Name = appointmentName, // Ensure appointment name is included
            Status = AppointmentStatus.Pending,
            Token = Guid.NewGuid().ToString()
        };

        await appointmentRepository.Save(appointment, cancellationToken);
        await notificationService.SendNotificationAsync(email, "Appointment Confirmation", $"Your appointment '{appointmentName}' is scheduled for {date}.");

        logger.LogInformation("Appointment '{AppointmentName}' created successfully for Agency {AgencyId}.", appointmentName, agencyId);
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

        appointment.Status = AppointmentStatus.Canceled;
        await SaveAsync(appointment, cancellationToken);
        await notificationService.SendNotificationAsync(appointment.Token, "Appointment Cancellation", $"Your appointment '{appointment.Name}' has been canceled.");

        logger.LogInformation("Appointment '{AppointmentName}' canceled successfully.", appointment.Name);
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
            var agencyUser = await agencyService.GetByIdAsync(appointment.AgencyUserId);

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
}