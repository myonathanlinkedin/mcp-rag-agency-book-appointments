using System.ComponentModel.DataAnnotations;

public class Holiday : Entity, IAggregateRoot
{
    public Guid AgencyId { get; private set; }
    
    [Required]
    public DateTime Date { get; private set; }
    
    [Required]
    [StringLength(200)]
    public string Reason { get; private set; }

    public Holiday() : base() { } // Parameterless constructor for EF Core

    // Main constructor
    public Holiday(
        Guid id,
        Guid agencyId,
        DateTime date,
        string reason) : base(id)
    {
        AgencyId = agencyId;
        Date = date.Date; // Ensure we only store the date part
        Reason = reason;
    }

    // Factory method for creating a new holiday
    public static Result<Holiday> Create(
        Guid agencyId,
        DateTime date,
        string reason)
    {
        var holiday = new Holiday(
            id: Guid.NewGuid(),
            agencyId: agencyId,
            date: date,
            reason: reason);

        var validationResult = holiday.Validate();
        if (!validationResult.Succeeded)
        {
            return Result<Holiday>.Failure(validationResult.Errors);
        }

        return Result<Holiday>.SuccessWith(holiday);
    }

    // Domain methods
    public Result UpdateDetails(DateTime newDate, string newReason)
    {
        if (newDate.Date < DateTime.Today)
        {
            return Result.Failure(new[] { "Cannot set holiday date in the past." });
        }

        if (string.IsNullOrWhiteSpace(newReason))
        {
            return Result.Failure(new[] { "Holiday reason is required." });
        }

        if (newReason.Length > 200)
        {
            return Result.Failure(new[] { "Holiday reason must not exceed 200 characters." });
        }

        Date = newDate.Date;
        Reason = newReason;

        return Result.Success;
    }

    private Result Validate()
    {
        var errors = new List<string>();

        if (AgencyId == Guid.Empty)
        {
            errors.Add("Agency ID is required.");
        }

        if (Date.Date < DateTime.Today)
        {
            errors.Add("Holiday date cannot be in the past.");
        }

        if (string.IsNullOrWhiteSpace(Reason))
        {
            errors.Add("Holiday reason is required.");
        }
        else if (Reason.Length > 200)
        {
            errors.Add("Holiday reason must not exceed 200 characters.");
        }

        return errors.Any() ? Result.Failure(errors) : Result.Success;
    }
}
