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
    [Range(0, 50)]
    public int Capacity { get; private set; }

    [Timestamp]
    public byte[] RowVersion { get; private set; }

    private AppointmentSlot() : base()
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
        Validate();

        RaiseEvent(new AppointmentSlotEntityEvent(id));
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

    public Result DecreaseCapacity()
    {
        if (Capacity <= 0)
        {
            return Result.Failure(new[] { "No capacity available." });
        }

        Capacity--;
        return Result.Success;
    }

    public Result IncreaseCapacity()
    {
        if (Capacity >= 50)
        {
            return Result.Failure(new[] { "Maximum capacity reached." });
        }

        Capacity++;
        return Result.Success;
    }

    public Result UpdateCapacity(int newCapacity)
    {
        if (newCapacity < 0 || newCapacity > 50)
        {
            return Result.Failure(new[] { "Capacity must be between 0 and 50." });
        }

        Capacity = newCapacity;
        return Result.Success;
    }

    public void UpdateTimes(DateTime startTime, DateTime endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
        Validate();
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

        if (Capacity < 0 || Capacity > 50)
        {
            throw new ArgumentException("Capacity must be between 0 and 50.");
        }
    }
}
