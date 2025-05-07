public interface IHolidayService : IBaseService<Holiday>
{
    Task<List<Holiday>> GetHolidaysByAgencyAsync(Guid agencyId);
}
