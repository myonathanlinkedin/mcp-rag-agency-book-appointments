using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

public interface IAppointmentRepository : IDomainRepository<Appointment>
{
    Task<List<Appointment>> GetAppointmentsByAgencyAsync(Guid agencyId);
    Task<List<Appointment>> GetUpcomingAppointmentsAsync(Guid agencyId, DateTime fromDate);
    Task<List<Appointment>> GetByDateAsync(DateTime date);
    Task<List<Appointment>> GetByDateAndUserAsync(DateTime date, string userEmail);
    Task UpsertAsync(Appointment appointment, CancellationToken cancellationToken = default);
}
