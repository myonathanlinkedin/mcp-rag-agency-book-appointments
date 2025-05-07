public interface IAppointmentService : IBaseService<Appointment>
{
    Task<List<Appointment>> GetAppointmentsByAgencyAsync(Guid agencyId);
    Task<List<Appointment>> GetUpcomingAppointmentsAsync(Guid agencyId, DateTime fromDate);
}
