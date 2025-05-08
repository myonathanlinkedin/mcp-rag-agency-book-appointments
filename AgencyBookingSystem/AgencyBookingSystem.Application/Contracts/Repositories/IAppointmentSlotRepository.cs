public interface IAppointmentSlotRepository : IDomainRepository<AppointmentSlot>
{
    Task<List<AppointmentSlot>> GetSlotsByAgencyAsync(Guid agencyId, DateTime date);
    Task<AppointmentSlot?> GetAvailableSlotAsync(Guid agencyId, DateTime date);
    Task<List<AppointmentSlot>> GetUpcomingSlotsAsync(Guid agencyId, DateTime fromDate);
    Task Save(AppointmentSlot entity, CancellationToken cancellationToken = default);
}
