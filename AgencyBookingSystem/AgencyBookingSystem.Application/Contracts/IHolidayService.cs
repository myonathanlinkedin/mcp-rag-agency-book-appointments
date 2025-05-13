public interface IHolidayService : IBaseService<Holiday>
{
    Task<List<Holiday>> GetHolidaysByAgencyAsync(Guid agencyId);
    Task DeleteHolidaysForAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default);
    Task<Result> CreateHolidayAsync(Guid agencyId, DateTime date, string reason, CancellationToken cancellationToken = default);
}
