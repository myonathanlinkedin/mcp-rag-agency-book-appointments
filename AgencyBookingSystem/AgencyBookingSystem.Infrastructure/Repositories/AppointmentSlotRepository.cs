using Microsoft.EntityFrameworkCore;

internal class AppointmentSlotRepository : DataRepository<AgencyBookingDbContext, AppointmentSlot>, IAppointmentSlotRepository
{
    public AppointmentSlotRepository(AgencyBookingDbContext db) : base(db) { }

    public async Task<List<AppointmentSlot>> GetSlotsByAgencyAsync(Guid agencyId, DateTime date)
    {
        // Find which hour slot this time falls into
        var slotStartTime = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0);
        var slotEndTime = slotStartTime.AddHours(1);

        // Check if the requested time falls within any existing slot's hour
        return await All()
            .Where(slot => 
                slot.AgencyId == agencyId && 
                slot.StartTime <= date &&  // Slot starts before or at requested time
                slot.EndTime > date)       // Slot ends after requested time
            .ToListAsync();
    }

    public async Task<AppointmentSlot?> GetAvailableSlotAsync(Guid agencyId, DateTime date)
    {
        // Find which hour slot this time falls into
        var slotStartTime = new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0);
        var slotEndTime = slotStartTime.AddHours(1);

        // Check if there's an available slot for this hour
        return await All()
            .Where(slot => 
                slot.AgencyId == agencyId && 
                slot.StartTime == slotStartTime &&  // Must start at the hour
                slot.EndTime == slotEndTime &&      // Must be exactly one hour
                slot.Capacity > 0)                  // Must have capacity
            .OrderBy(slot => slot.StartTime)
            .FirstOrDefaultAsync();
    }

    public async Task<List<AppointmentSlot>> GetUpcomingSlotsAsync(Guid agencyId, DateTime fromDate)
    {
        // Round up to the next hour if we're in the middle of an hour
        var nextHourStart = new DateTime(
            fromDate.Year, fromDate.Month, fromDate.Day, 
            fromDate.Hour, 0, 0).AddHours(fromDate.Minute > 0 ? 1 : 0);

        return await All()
            .Where(slot => 
                slot.AgencyId == agencyId && 
                slot.StartTime >= nextHourStart)
            .OrderBy(slot => slot.StartTime)
            .ToListAsync();
    }
}
