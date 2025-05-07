using Microsoft.Extensions.Logging;

public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository appointmentRepository;
    private readonly ILogger<AppointmentService> logger;

    public AppointmentService(IAppointmentRepository appointmentRepository, ILogger<AppointmentService> logger)
    {
        this.appointmentRepository = appointmentRepository;
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

    public async Task SaveAsync(Appointment entity, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving appointment for Agency: {AgencyId}", entity.AgencyId);
        await appointmentRepository.Save(entity, cancellationToken);
    }

    public async Task<List<Appointment>> GetAppointmentsByAgencyAsync(Guid agencyId)
    {
        logger.LogInformation("Fetching appointments for Agency ID: {AgencyId}", agencyId);
        return await appointmentRepository.GetAppointmentsByAgencyAsync(agencyId);
    }

    public async Task<List<Appointment>> GetUpcomingAppointmentsAsync(Guid agencyId, DateTime fromDate)
    {
        logger.LogInformation("Fetching upcoming appointments after {FromDate} for Agency ID: {AgencyId}", fromDate, agencyId);
        return await appointmentRepository.GetUpcomingAppointmentsAsync(agencyId, fromDate);
    }
}
