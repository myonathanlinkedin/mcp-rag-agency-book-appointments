using Microsoft.Extensions.Logging;

public class HolidayService : IHolidayService
{
    private readonly IHolidayRepository holidayRepository;
    private readonly ILogger<HolidayService> logger;

    public HolidayService(IHolidayRepository holidayRepository, ILogger<HolidayService> logger)
    {
        this.holidayRepository = holidayRepository;
        this.logger = logger;
    }

    public async Task<Holiday?> GetByIdAsync(Guid id)
    {
        logger.LogInformation("Fetching holiday with ID: {Id}", id);
        return await holidayRepository.GetByIdAsync(id);
    }

    public async Task<List<Holiday>> GetAllAsync()
    {
        logger.LogInformation("Fetching all holidays.");
        return await holidayRepository.GetAllAsync();
    }

    public async Task SaveAsync(Holiday entity, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving holiday for Agency ID: {AgencyId}", entity.AgencyId);
        await holidayRepository.UpsertAsync(entity, cancellationToken);
    }

    public async Task<List<Holiday>> GetHolidaysByAgencyAsync(Guid agencyId)
    {
        logger.LogInformation("Fetching holidays for Agency ID: {AgencyId}", agencyId);
        return await holidayRepository.GetHolidaysByAgencyAsync(agencyId);
    }
}
