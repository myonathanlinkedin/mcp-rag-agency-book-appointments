using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

internal class AppointmentRepository : BufferedDataRepository<AgencyBookingDbContext, Appointment>, IAppointmentRepository
{
    public AppointmentRepository(
        AgencyBookingDbContext db,
        ILogger<AppointmentRepository> logger)
        : base(db, logger)
    {
    }

    public override async Task<Appointment?> GetByIdAsync(Guid id)
    {
        Expression<Func<Appointment, object>> includeUser = a => a.AgencyUser;
        return await GetByIdWithIncludesAsync(id, includeUser);
    }

    public async Task<List<Appointment>> GetUpcomingAppointmentsAsync(Guid agencyId, DateTime fromDate)
    {
        Expression<Func<Appointment, bool>> predicate = a => a.AgencyId == agencyId && a.Date >= fromDate;
        Expression<Func<Appointment, object>> includeUser = a => a.AgencyUser;
        return await FindWithIncludesAsync(predicate, includeUser);
    }

    public async Task<List<Appointment>> GetAppointmentsByAgencyAsync(Guid agencyId)
    {
        Expression<Func<Appointment, bool>> predicate = a => a.AgencyId == agencyId;
        Expression<Func<Appointment, object>> includeUser = a => a.AgencyUser;
        return await FindWithIncludesAsync(predicate, includeUser);
    }

    public async Task<List<Appointment>> GetByDateAsync(DateTime date)
    {
        Expression<Func<Appointment, bool>> predicate = a => a.Date.Date == date.Date;
        Expression<Func<Appointment, object>> includeUser = a => a.AgencyUser;
        return await FindWithIncludesAsync(predicate, includeUser);
    }

    public async Task<List<Appointment>> GetByDateAndUserAsync(DateTime date, string userEmail)
    {
        Expression<Func<Appointment, bool>> predicate = a => a.Date.Date == date.Date && a.AgencyUser.Email == userEmail;
        Expression<Func<Appointment, object>> includeUser = a => a.AgencyUser;
        return await FindWithIncludesAsync(predicate, includeUser);
    }
}
