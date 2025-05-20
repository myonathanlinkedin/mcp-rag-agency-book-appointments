using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

internal class AppointmentSlotRepository : BufferedDataRepository<AgencyBookingDbContext, AppointmentSlot>, IAppointmentSlotRepository
{
    public AppointmentSlotRepository(
        AgencyBookingDbContext db,
        ILogger<AppointmentSlotRepository> logger)
        : base(db, logger)
    {
    }

    public async Task<List<AppointmentSlot>> GetSlotsByAgencyAsync(Guid agencyId, DateTime date)
    {
        return await FindAsync(s => 
            s.AgencyId == agencyId && 
            s.StartTime.Date == date.Date);
    }

    public async Task<AppointmentSlot?> GetAvailableSlotAsync(Guid agencyId, DateTime date)
    {
        return await FindAsync(s => 
            s.AgencyId == agencyId && 
            s.StartTime.Date == date.Date && 
            s.Capacity > 0)
            .ContinueWith(t => t.Result.FirstOrDefault());
    }

    public async Task<List<AppointmentSlot>> GetUpcomingSlotsAsync(Guid agencyId, DateTime fromDate)
    {
        return await FindAsync(s => 
            s.AgencyId == agencyId && 
            s.StartTime >= fromDate);
    }
}
