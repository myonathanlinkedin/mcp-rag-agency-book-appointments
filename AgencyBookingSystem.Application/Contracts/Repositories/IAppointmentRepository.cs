using System.Threading.Tasks;
using System.Collections.Generic;

public interface IAppointmentRepository : IDomainRepository<Appointment>
{
    Task<List<Appointment>> GetAppointmentsByAgencyAsync(Guid agencyId);
    Task<List<Appointment>> GetUpcomingAppointmentsAsync(Guid agencyId, DateTime fromDate);
}
