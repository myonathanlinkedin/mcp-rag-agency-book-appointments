using System.Collections.Generic;
using System.Threading.Tasks;

public interface IAppointmentUnitOfWork : IUnitOfWork
{
    IAppointmentRepository Appointments { get; }
    IAppointmentSlotRepository AppointmentSlots { get; }
    IAgencyRepository Agencies { get; }
    IAgencyUserRepository AgencyUsers { get; }
    IHolidayRepository Holidays { get; }
} 