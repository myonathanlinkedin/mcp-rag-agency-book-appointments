using System.ComponentModel.DataAnnotations;

public class AppointmentSlot : Entity, IAggregateRoot
{
    public Guid Id { get; private set; }
    public Guid AgencyId { get; private set; }
    
    [Required]
    public DateTime StartTime { get; private set; }
    
    [Required]
    public DateTime EndTime { get; private set; }
    
    [Required]
    [Range(1, 50)]
    public int Capacity { get; private set; }

    public bool HasCapacity => Capacity > 0;

    public AppointmentSlot() : base()
    {
        // Default constructor for EF Core
    }

    // Main constructor
    public AppointmentSlot(
        Guid id,
        Guid agencyId,
        DateTime startTime,
        DateTime endTime,
        int capacity) : base(id)
    {
        Id = id;
        AgencyId = agencyId;
        StartTime = startTime;
        EndTime = endTime;
        Capacity = capacity;
    }

    // Factory method
    public static AppointmentSlot Create(
        Guid agencyId,
        DateTime startTime,
        DateTime endTime,
        int capacity)
    {
        return new AppointmentSlot(
            id: Guid.NewGuid(),
            agencyId: agencyId,
            startTime: startTime,
            endTime: endTime,
            capacity: capacity);
    }

    // Validation for appointment slot
    private void Validate()
    {
        if (EndTime <= StartTime)
        {
            throw new ArgumentException("EndTime must be after StartTime.");
        }

        if (EndTime - StartTime > TimeSpan.FromHours(1))
        {
            throw new ArgumentException("Appointment duration cannot exceed 1 hour.");
        }
    }

    // Increase capacity by 1
    public void IncreaseCapacity()
    {
        Capacity++;
    }

    // Decrease capacity by 1
    public void DecreaseCapacity()
    {
        if (Capacity > 0)
        {
            Capacity--;
        }
        else
        {
            throw new InvalidOperationException("Capacity cannot be less than 0.");
        }
    }

    // Domain methods
    public Result UpdateCapacity(int newCapacity)
    {
        if (newCapacity < 1 || newCapacity > 50)
        {
            return Result.Failure(new[] { "Capacity must be between 1 and 50." });
        }

        Capacity = newCapacity;
        return Result.Success;
    }

    public Result UpdateTimes(DateTime newStartTime, DateTime newEndTime)
    {
        if (newStartTime >= newEndTime)
        {
            return Result.Failure(new[] { "Start time must be before end time." });
        }

        if (newStartTime < DateTime.Now)
        {
            return Result.Failure(new[] { "Cannot set slot time in the past." });
        }

        StartTime = newStartTime;
        EndTime = newEndTime;
        return Result.Success;
    }
}
