using System.ComponentModel.DataAnnotations;

public class Agency : Entity, IAggregateRoot
{
    [Required]
    [StringLength(100)]
    public string Name { get; private set; }
    
    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; private set; }
    
    public bool RequiresApproval { get; private set; }
    
    private readonly List<AppointmentSlot> slots = new();
    public IReadOnlyCollection<AppointmentSlot> Slots => slots.AsReadOnly();
    
    private readonly List<Holiday> holidays = new();
    public IReadOnlyCollection<Holiday> Holidays => holidays.AsReadOnly();
    
    [Range(1, 50)]
    public int MaxAppointmentsPerDay { get; private set; }
    
    private readonly List<AgencyUser> agencyUsers = new();
    public IReadOnlyCollection<AgencyUser> AgencyUsers => agencyUsers.AsReadOnly();

    public bool IsApproved { get; private set; }

    public Agency() : base()
    {
        // Default constructor for EF Core
    }

    // Main constructor
    public Agency(
        Guid id,
        string name,
        string email,
        bool requiresApproval,
        int maxAppointmentsPerDay) : base(id)
    {
        Name = name;
        Email = email;
        RequiresApproval = requiresApproval;
        MaxAppointmentsPerDay = maxAppointmentsPerDay;
        IsApproved = !requiresApproval;
    }

    // Factory method for creating a new agency
    public static Result<Agency> Create(
        string name,
        string email,
        bool requiresApproval,
        int maxAppointmentsPerDay)
    {
        var agency = new Agency(
            id: Guid.NewGuid(),
            name: name,
            email: email,
            requiresApproval: requiresApproval,
            maxAppointmentsPerDay: maxAppointmentsPerDay);

        var validationResult = agency.Validate();
        if (!validationResult.Succeeded)
        {
            return Result<Agency>.Failure(validationResult.Errors);
        }

        return Result<Agency>.SuccessWith(agency);
    }

    // Domain methods
    public Result Approve()
    {
        if (IsApproved)
        {
            return Result.Failure(new[] { "Agency is already approved." });
        }

        IsApproved = true;
        return Result.Success;
    }

    public Result AddHoliday(DateTime date, string reason)
    {
        if (date.Date < DateTime.Today)
        {
            return Result.Failure(new[] { "Cannot add holidays in the past." });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(new[] { "Holiday reason is required." });
        }

        if (holidays.Any(h => h.Date.Date == date.Date))
        {
            return Result.Failure(new[] { "A holiday already exists for this date." });
        }

        var holiday = new Holiday(Guid.NewGuid(), Id, date, reason);

        holidays.Add(holiday);
        return Result.Success;
    }

    public Result RemoveHoliday(Guid holidayId)
    {
        var holiday = holidays.FirstOrDefault(h => h.Id == holidayId);
        if (holiday == null)
        {
            return Result.Failure(new[] { "Holiday not found." });
        }

        if (holiday.Date.Date < DateTime.Today)
        {
            return Result.Failure(new[] { "Cannot remove past holidays." });
        }

        holidays.Remove(holiday);
        return Result.Success;
    }

    public Result AddAppointmentSlot(DateTime startTime, int capacity)
    {
        if (startTime < DateTime.Now)
        {
            return Result.Failure(new[] { "Cannot add slots in the past." });
        }

        if (capacity <= 0)
        {
            return Result.Failure(new[] { "Slot capacity must be greater than 0." });
        }

        if (slots.Any(s => s.StartTime == startTime))
        {
            return Result.Failure(new[] { "A slot already exists for this time." });
        }

        var slot =  AppointmentSlot.Create(Id, startTime, startTime.AddHours(1), capacity); // Assuming 1 hour duration

        slots.Add(slot);
        return Result.Success;
    }

    public Result RemoveAppointmentSlot(Guid slotId)
    {
        var slot = slots.FirstOrDefault(s => s.Id == slotId);
        if (slot == null)
        {
            return Result.Failure(new[] { "Slot not found." });
        }

        if (slot.StartTime < DateTime.Now)
        {
            return Result.Failure(new[] { "Cannot remove past slots." });
        }

        slots.Remove(slot);
        return Result.Success;
    }

    public Result AssignUser(AgencyUser user)
    {
        if (user == null)
        {
            return Result.Failure(new[] { "User cannot be null." });
        }

        if (agencyUsers.Any(u => u.Email == user.Email))
        {
            return Result.Failure(new[] { "User is already assigned to this agency." });
        }

        agencyUsers.Add(user);
        return Result.Success;
    }

    public Result RemoveUser(Guid userId)
    {
        var user = agencyUsers.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            return Result.Failure(new[] { "User not found." });
        }

        agencyUsers.Remove(user);
        return Result.Success;
    }

    public Result UpdateDetails(string name, string email, int maxAppointmentsPerDay)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(new[] { "Name is required." });
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Failure(new[] { "Email is required." });
        }

        if (!new EmailAddressAttribute().IsValid(email))
        {
            return Result.Failure(new[] { "Invalid email format." });
        }

        if (maxAppointmentsPerDay < 1 || maxAppointmentsPerDay > 50)
        {
            return Result.Failure(new[] { "Max appointments per day must be between 1 and 50." });
        }

        Name = name;
        Email = email;
        MaxAppointmentsPerDay = maxAppointmentsPerDay;

        return Result.Success;
    }

    public Result ClearHolidays()
    {
        if (holidays.Any(h => h.Date.Date < DateTime.Today))
        {
            return Result.Failure(new[] { "Cannot clear holidays that include past dates." });
        }

        holidays.Clear();
        return Result.Success;
    }

    public Result Unapprove()
    {
        if (!IsApproved)
        {
            return Result.Failure(new[] { "Agency is already unapproved." });
        }

        if (agencyUsers.Any())
        {
            return Result.Failure(new[] { "Cannot unapprove an agency that has assigned users." });
        }

        if (slots.Any(s => s.StartTime > DateTime.Now))
        {
            return Result.Failure(new[] { "Cannot unapprove an agency that has future appointment slots." });
        }

        IsApproved = false;
        RequiresApproval = true;
        return Result.Success;
    }

    private Result Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Name is required.");
        }
        else if (Name.Length > 100)
        {
            errors.Add("Name must not exceed 100 characters.");
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            errors.Add("Email is required.");
        }
        else if (!new EmailAddressAttribute().IsValid(Email))
        {
            errors.Add("Invalid email format.");
        }
        else if (Email.Length > 150)
        {
            errors.Add("Email must not exceed 150 characters.");
        }

        if (MaxAppointmentsPerDay < 1 || MaxAppointmentsPerDay > 50)
        {
            errors.Add("Max appointments per day must be between 1 and 50.");
        }

        return errors.Any() ? Result.Failure(errors) : Result.Success;
    }
}
