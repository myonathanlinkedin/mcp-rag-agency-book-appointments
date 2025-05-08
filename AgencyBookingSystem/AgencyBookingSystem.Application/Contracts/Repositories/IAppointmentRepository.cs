using System.Threading.Tasks;
using System.Collections.Generic;

public interface IAppointmentRepository : IDomainRepository<Appointment>
{
    Task<List<Appointment>> GetAppointmentsByAgencyAsync(Guid agencyId);
    Task<List<Appointment>> GetUpcomingAppointmentsAsync(Guid agencyId, DateTime fromDate);
    Task<List<Appointment>> GetByDateAsync(DateTime date);
    Task<List<Appointment>> GetByDateAndUserAsync(DateTime date, string userEmail);
}
