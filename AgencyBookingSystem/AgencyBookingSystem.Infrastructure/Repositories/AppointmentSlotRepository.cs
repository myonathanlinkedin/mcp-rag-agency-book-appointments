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

    public async Task Save(AppointmentSlot entity, CancellationToken cancellationToken = default)
    {
        var existingSlot = await Data.AppointmentSlots.FindAsync(entity.Id);
        if (existingSlot == null)
        {
            await Data.AppointmentSlots.AddAsync(entity, cancellationToken);
        }
        else
        {
            Data.AppointmentSlots.Update(entity);
        }

        await Data.SaveChangesAsync(cancellationToken);
    }

    public async Task<Appointment?> GetByIdAsync(Guid id)
    {
        return await Data.Appointments.FindAsync(id);
    }

    public async Task<List<Appointment>> GetAllAsync()
    {
        return await Data.Appointments.ToListAsync();
    }

    public async Task<List<Appointment>> FindAsync(Expression<Func<Appointment, bool>> predicate)
    {
        return await Data.Appointments.Where(predicate).ToListAsync();
    }

    public async Task Save(Appointment entity, CancellationToken cancellationToken = default)
    {
        var existingAppointment = await Data.Appointments.FindAsync(entity.Id);
        if (existingAppointment == null)
        {
            await Data.Appointments.AddAsync(entity, cancellationToken);
        }
        else
        {
            Data.Appointments.Update(entity);
        }

        await Data.SaveChangesAsync(cancellationToken);
    }
}
