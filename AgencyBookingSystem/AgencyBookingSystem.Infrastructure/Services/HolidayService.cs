using Microsoft.Extensions.Logging;

public class HolidayService : IHolidayService
{
    private readonly IHolidayRepository holidayRepository;
    private readonly ILogger<HolidayService> logger;

    public HolidayService(
        IHolidayRepository holidayRepository,
        ILogger<HolidayService> logger)
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
        logger.LogInformation("Fetching all holidays");
        return await holidayRepository.GetAllAsync();
    }

    public async Task UpsertAsync(Holiday entity, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving holiday for date: {Date}", entity.Date);
        await holidayRepository.UpsertAsync(entity, cancellationToken);
    }

    public async Task<List<Holiday>> GetHolidaysByAgencyAsync(Guid agencyId)
    {
        logger.LogInformation("Fetching holidays for Agency {AgencyId}", agencyId);
        return await holidayRepository.GetHolidaysByAgencyAsync(agencyId);
    }

    public async Task<Result> CreateHolidayAsync(
        Guid agencyId,
        DateTime date,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var holidayResult = Holiday.Create(agencyId, date, reason);
        if (!holidayResult.Succeeded)
        {
            return holidayResult;
        }

        var holiday = holidayResult.Data;
        await holidayRepository.UpsertAsync(holiday, cancellationToken);
        logger.LogInformation("Holiday created successfully for Agency {AgencyId} on {Date}.", agencyId, date);

        return Result.Success;
    }

    public async Task<Result> UpdateHolidayAsync(
        Guid holidayId,
        DateTime newDate,
        string newReason,
        CancellationToken cancellationToken = default)
    {
        var holiday = await holidayRepository.GetByIdAsync(holidayId);
        if (holiday == null)
        {
            logger.LogWarning("Update failed. Holiday {HolidayId} not found.", holidayId);
            return Result.Failure(new[] { "Holiday not found." });
        }

        var updateResult = holiday.UpdateDetails(newDate, newReason);
        if (!updateResult.Succeeded)
        {
            return updateResult;
        }

        await holidayRepository.UpsertAsync(holiday, cancellationToken);
        logger.LogInformation("Holiday {HolidayId} updated successfully.", holidayId);

        return Result.Success;
    }

    public async Task<Result> DeleteHolidayAsync(
        Guid holidayId,
        CancellationToken cancellationToken = default)
    {
        var holiday = await holidayRepository.GetByIdAsync(holidayId);
        if (holiday == null)
        {
            logger.LogWarning("Delete failed. Holiday {HolidayId} not found.", holidayId);
            return Result.Failure(new[] { "Holiday not found." });
        }

        if (holiday.Date.Date < DateTime.Today)
        {
            logger.LogWarning("Delete failed. Cannot delete past holidays.");
            return Result.Failure(new[] { "Cannot delete past holidays." });
        }

        await holidayRepository.DeleteAsync(holiday.Id, cancellationToken);
        logger.LogInformation("Holiday {HolidayId} deleted successfully.", holidayId);

        return Result.Success;
    }

    public async Task DeleteHolidaysForAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting all holidays for agency: {AgencyId}", agencyId);
        await holidayRepository.DeleteForAgencyAsync(agencyId, cancellationToken);
    }

    public async Task AddHolidayAsync(Guid agencyId, Holiday holiday, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding holiday for agency {AgencyId} on date {Date}", agencyId, holiday.Date);
        
        var holidayResult = Holiday.Create(agencyId, holiday.Date, holiday.Reason);
        if (!holidayResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create holiday: {string.Join(", ", holidayResult.Errors)}");
        }

        await holidayRepository.AddAsync(holidayResult.Data, cancellationToken);
        await holidayRepository.SaveChangesAsync(cancellationToken);
    }
}
