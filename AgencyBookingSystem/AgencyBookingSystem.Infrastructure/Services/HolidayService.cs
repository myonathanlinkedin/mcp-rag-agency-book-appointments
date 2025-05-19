using Microsoft.Extensions.Logging;

public class HolidayService : IHolidayService
{
    private readonly IAppointmentUnitOfWork unitOfWork;
    private readonly ILogger<HolidayService> logger;

    public HolidayService(
        IAppointmentUnitOfWork unitOfWork,
        ILogger<HolidayService> logger)
    {
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<Holiday?> GetByIdAsync(Guid id)
    {
        logger.LogInformation("Fetching holiday with ID: {Id}", id);
        return await unitOfWork.Holidays.GetByIdAsync(id);
    }

    public async Task<List<Holiday>> GetAllAsync()
    {
        logger.LogInformation("Fetching all holidays");
        return await unitOfWork.Holidays.GetAllAsync();
    }

    public async Task UpsertAsync(Holiday entity, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving holiday for date: {Date}", entity.Date);
        await unitOfWork.Holidays.UpsertAsync(entity, cancellationToken);
    }

    public async Task<List<Holiday>> GetHolidaysByAgencyAsync(Guid agencyId)
    {
        logger.LogInformation("Fetching holidays for Agency {AgencyId}", agencyId);
        return await unitOfWork.Holidays.GetHolidaysByAgencyAsync(agencyId);
    }

    public async Task<Result> CreateHolidayAsync(
        Guid agencyId,
        DateTime date,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var holidayResult = Holiday.Create(agencyId, date, reason);
            if (!holidayResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return holidayResult;
            }

            var holiday = holidayResult.Data;
            await unitOfWork.Holidays.UpsertAsync(holiday, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            
            logger.LogInformation("Holiday created successfully for Agency {AgencyId} on {Date}.", agencyId, date);
            return Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error creating holiday for Agency {AgencyId} on {Date}", agencyId, date);
            return Result.Failure(new[] { "An error occurred while creating the holiday." });
        }
    }

    public async Task<Result> UpdateHolidayAsync(
        Guid holidayId,
        DateTime newDate,
        string newReason,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var holiday = await unitOfWork.Holidays.GetByIdAsync(holidayId);
            if (holiday == null)
            {
                logger.LogWarning("Update failed. Holiday {HolidayId} not found.", holidayId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "Holiday not found." });
            }

            var updateResult = holiday.UpdateDetails(newDate, newReason);
            if (!updateResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return updateResult;
            }

            await unitOfWork.Holidays.UpsertAsync(holiday, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            
            logger.LogInformation("Holiday {HolidayId} updated successfully.", holidayId);
            return Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error updating holiday {HolidayId}", holidayId);
            return Result.Failure(new[] { "An error occurred while updating the holiday." });
        }
    }

    public async Task<Result> DeleteHolidayAsync(
        Guid holidayId,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var holiday = await unitOfWork.Holidays.GetByIdAsync(holidayId);
            if (holiday == null)
            {
                logger.LogWarning("Delete failed. Holiday {HolidayId} not found.", holidayId);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "Holiday not found." });
            }

            if (holiday.Date.Date < DateTime.Today)
            {
                logger.LogWarning("Delete failed. Cannot delete past holidays.");
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure(new[] { "Cannot delete past holidays." });
            }

            await unitOfWork.Holidays.DeleteAsync(holiday.Id, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            
            logger.LogInformation("Holiday {HolidayId} deleted successfully.", holidayId);
            return Result.Success;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error deleting holiday {HolidayId}", holidayId);
            return Result.Failure(new[] { "An error occurred while deleting the holiday." });
        }
    }

    public async Task DeleteHolidaysForAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            logger.LogInformation("Deleting all holidays for agency: {AgencyId}", agencyId);
            await unitOfWork.Holidays.DeleteForAgencyAsync(agencyId, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error deleting holidays for Agency {AgencyId}", agencyId);
            throw;
        }
    }

    public async Task AddHolidayAsync(Guid agencyId, Holiday holiday, CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            logger.LogInformation("Adding holiday for agency {AgencyId} on date {Date}", agencyId, holiday.Date);
            
            var holidayResult = Holiday.Create(agencyId, holiday.Date, holiday.Reason);
            if (!holidayResult.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw new InvalidOperationException($"Failed to create holiday: {string.Join(", ", holidayResult.Errors)}");
            }

            await unitOfWork.Holidays.AddAsync(holidayResult.Data, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Error adding holiday for Agency {AgencyId}", agencyId);
            throw;
        }
    }
}
