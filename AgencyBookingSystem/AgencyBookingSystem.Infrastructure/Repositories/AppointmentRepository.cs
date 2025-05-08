using Microsoft.EntityFrameworkCore;

internal class AppointmentRepository : DataRepository<AgencyBookingDbContext, Appointment>, IAppointmentRepository
{
    public AppointmentRepository(AgencyBookingDbContext db) : base(db) { }

    public async Task<List<Appointment>> GetUpcomingAppointmentsAsync(Guid agencyId, DateTime fromDate)
        => await All().Where(a => a.AgencyId == agencyId && a.Date >= fromDate).ToListAsync();

    public async Task<List<Appointment>> GetAppointmentsByAgencyAsync(Guid agencyId)
        => await All().Where(a => a.AgencyId == agencyId).ToListAsync();

    public async Task<List<Appointment>> GetByDateAsync(DateTime date)
        => await All().Where(a => a.Date.Date == date.Date).ToListAsync();

    public async Task<List<Appointment>> GetByDateAndUserAsync(DateTime date, string userEmail)
        => await All().Where(a => a.Date.Date == date.Date && a.AgencyUser.Email == userEmail).ToListAsync();
}
