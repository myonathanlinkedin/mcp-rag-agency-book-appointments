using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

internal class AppointmentSlotRepository : DataRepository<AgencyBookingDbContext, AppointmentSlot>, IAppointmentSlotRepository
{
    public AppointmentSlotRepository(AgencyBookingDbContext db) : base(db) { }

    public async Task<List<AppointmentSlot>> GetSlotsByAgencyAsync(Guid agencyId, DateTime date)
        => await All().Where(slot => slot.AgencyId == agencyId && slot.StartTime.Date == date.Date).ToListAsync();

    public async Task<AppointmentSlot?> GetAvailableSlotAsync(Guid agencyId, DateTime date)
        => await All().Where(slot => slot.AgencyId == agencyId && slot.StartTime.Date == date.Date && slot.Capacity > 0)
                      .OrderBy(slot => slot.StartTime)
                      .FirstOrDefaultAsync();

    public async Task<List<AppointmentSlot>> GetUpcomingSlotsAsync(Guid agencyId, DateTime fromDate)
        => await All().Where(slot => slot.AgencyId == agencyId && slot.StartTime.Date >= fromDate.Date).ToListAsync();
}
