using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Transactions;

public class AppointmentUnitOfWork : ConcurrentUnitOfWork<AgencyBookingDbContext>, IAppointmentUnitOfWork
{
    private readonly IAppointmentRepository appointments;
    private readonly IAppointmentSlotRepository appointmentSlots;
    private readonly IAgencyRepository agencies;
    private readonly IAgencyUserRepository agencyUsers;
    private readonly IHolidayRepository holidays;

    public AppointmentUnitOfWork(
        AgencyBookingDbContext dbContext,
        ILogger<AppointmentUnitOfWork> logger,
        IAppointmentRepository appointments,
        IAppointmentSlotRepository appointmentSlots,
        IAgencyRepository agencies,
        IAgencyUserRepository agencyUsers,
        IHolidayRepository holidays) : base(dbContext, logger)
    {
        this.appointments = appointments;
        this.appointmentSlots = appointmentSlots;
        this.agencies = agencies;
        this.agencyUsers = agencyUsers;
        this.holidays = holidays;
    }

    public IAppointmentRepository Appointments => appointments;
    public IAppointmentSlotRepository AppointmentSlots => appointmentSlots;
    public IAgencyRepository Agencies => agencies;
    public IAgencyUserRepository AgencyUsers => agencyUsers;
    public IHolidayRepository Holidays => holidays;
} 