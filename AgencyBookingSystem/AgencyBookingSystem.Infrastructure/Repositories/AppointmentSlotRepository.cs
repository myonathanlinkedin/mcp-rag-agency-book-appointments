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

    protected override async Task OnConcurrencyResolveAsync(AppointmentSlot currentEntity, AppointmentSlot conflictingEntity)
    {
        // Only update capacity if it's valid
        if (conflictingEntity.Capacity >= 0 && conflictingEntity.Capacity <= 50)
        {
            // For appointment slots, we need to be careful with capacity
            // If both changes tried to decrease capacity, we need to apply both decreases
            if (currentEntity.Capacity > conflictingEntity.Capacity)
            {
                var difference = currentEntity.Capacity - conflictingEntity.Capacity;
                currentEntity.UpdateCapacity(Math.Max(0, currentEntity.Capacity - difference));
            }
            else
            {
                // If one change increased capacity, use the higher value
                currentEntity.UpdateCapacity(Math.Max(currentEntity.Capacity, conflictingEntity.Capacity));
            }
        }

        // Update other non-critical properties
        currentEntity.UpdateTimes(conflictingEntity.StartTime, conflictingEntity.EndTime);

        await Task.CompletedTask;
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
