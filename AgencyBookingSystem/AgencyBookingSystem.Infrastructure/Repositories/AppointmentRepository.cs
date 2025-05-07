using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

internal class AppointmentRepository : DataRepository<AgencyBookingDbContext, Appointment>, IAppointmentRepository
{
    public AppointmentRepository(AgencyBookingDbContext db) : base(db) { }

    public async Task<List<Appointment>> GetUpcomingAppointmentsAsync(Guid agencyId, DateTime fromDate)
        => await All().Where(a => a.AgencyId == agencyId && a.Date >= fromDate).ToListAsync();

    public async Task<List<Appointment>> GetAppointmentsByAgencyAsync(Guid agencyId)
         => await All().Where(a => a.AgencyId == agencyId).ToListAsync();

    public async Task<List<Appointment>> GetByDateAsync(DateTime date)
         => await All().Where(a => a.Date.Date == date.Date).ToListAsync();
}
