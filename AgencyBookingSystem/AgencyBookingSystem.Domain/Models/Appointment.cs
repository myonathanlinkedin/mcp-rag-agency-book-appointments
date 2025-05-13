using System.ComponentModel.DataAnnotations;
public class Appointment : Entity, IAggregateRoot
{
    [Required]
    public Guid AgencyUserId { get; private set; }

    [Required]
    public Guid AgencyId { get; private set; }

    [Required]
    [FutureDate(ErrorMessage = "Appointment date must be in the future")]
    public DateTime Date { get; private set; }

    [Required]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Appointment name must be between 3 and 100 characters")]
    public string Name { get; private set; }

    [Required]
    [StringLength(20)]
    public string Status { get; private set; }

    [Required]
    [StringLength(50)]
    public string Token { get; private set; }

    public AgencyUser AgencyUser { get; private set; }

    public Appointment() : base()
    {
        // Default constructor for EF Core
    }

    // Main constructor
    public Appointment(
        Guid id,
        Guid agencyId,
        Guid agencyUserId,
        string name,
        DateTime date,
        string status,
        string token,
        AgencyUser agencyUser) : base(id)
    {
        AgencyId = agencyId;
        AgencyUserId = agencyUserId;
        Name = name;
        Date = date;
        Status = status;
        Token = token;
        AgencyUser = agencyUser;
    }

    // Factory method to create a new appointment
    public static Result<Appointment> Create(
        Guid agencyId,
        Guid agencyUserId,
        string name,
        DateTime date,
        AgencyUser agencyUser)
    {
        var appointment = new Appointment(
            id: Guid.NewGuid(),
            agencyId: agencyId,
            agencyUserId: agencyUserId,
            name: name,
            date: date,
            status: AppointmentStatus.Initiated,
            token: Guid.NewGuid().ToString("N"),
            agencyUser: agencyUser);

        var validationResult = appointment.Validate();
        if (!validationResult.Succeeded)
        {
            return Result<Appointment>.Failure(validationResult.Errors);
        }

        return Result<Appointment>.SuccessWith(appointment);
    }

    // Business methods
    public Result Reschedule(DateTime newDate)
    {
        if (Status == AppointmentStatus.Cancelled)
            return Result.Failure(new[] { "Cannot reschedule a cancelled appointment." });

        if (Status == AppointmentStatus.NoShow)
            return Result.Failure(new[] { "Cannot reschedule a no-show appointment." });

        if (newDate.Date < DateTime.UtcNow.Date)
            return Result.Failure(new[] { "New appointment date cannot be in the past." });

        if (newDate.Date > DateTime.UtcNow.AddMonths(6).Date)
            return Result.Failure(new[] { "Appointments cannot be rescheduled more than 6 months in advance." });

        Date = newDate;
        Status = AppointmentStatus.Initiated;
        Token = Guid.NewGuid().ToString("N"); // Generate new token for security

        return Result.Success;
    }

    public Result Cancel()
    {
        if (Status == AppointmentStatus.Cancelled)
            return Result.Failure(new[] { "Appointment is already cancelled." });

        if (Status == AppointmentStatus.NoShow)
            return Result.Failure(new[] { "Cannot cancel an appointment that is marked as no-show." });

        if (Date.Date < DateTime.UtcNow.Date)
            return Result.Failure(new[] { "Cannot cancel an appointment that is in the past." });

        Status = AppointmentStatus.Cancelled;
        return Result.Success;
    }

    public Result MarkAsNoShow()
    {
        if (Status != AppointmentStatus.Initiated && Status != AppointmentStatus.Confirmed)
            return Result.Failure(new[] { "Only initiated or confirmed appointments can be marked as no-show." });

        Status = AppointmentStatus.NoShow;
        return Result.Success;
    }

    public Result Confirm()
    {
        if (Status != AppointmentStatus.Initiated)
            return Result.Failure(new[] { "Only initiated appointments can be confirmed." });

        Status = AppointmentStatus.Confirmed;
        return Result.Success;
    }

    // Validation method
    private Result Validate()
    {
        var validationContext = new ValidationContext(this);
        var validationResults = new List<ValidationResult>();
        
        if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
        {
            return Result.Failure(validationResults.Select(r => r.ErrorMessage!).ToArray());
        }

        if (Date.Date < DateTime.UtcNow.Date)
            return Result.Failure(new[] { "Appointment date cannot be in the past." });

        if (Date.Date > DateTime.UtcNow.AddMonths(6).Date)
            return Result.Failure(new[] { "Appointments cannot be booked more than 6 months in advance." });

        if (string.IsNullOrWhiteSpace(Name) || Name.Length < 3 || Name.Length > 100)
            return Result.Failure(new[] { "Appointment name must be between 3 and 100 characters." });

        return Result.Success;
    }
}

// Custom validation attribute for future dates
public class FutureDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateTime date)
        {
            if (date.Date < DateTime.UtcNow.Date)
            {
                return new ValidationResult("Appointment date cannot be in the past");
            }
        }
        return ValidationResult.Success;
    }
}
