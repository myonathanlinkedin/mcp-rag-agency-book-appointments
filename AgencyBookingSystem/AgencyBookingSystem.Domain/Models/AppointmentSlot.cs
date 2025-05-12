public class AppointmentSlot : Entity, IAggregateRoot
{
    public Guid Id { get; private set; }
    public Guid AgencyId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public int Capacity { get; private set; }
    public bool HasCapacity => Capacity > 0;

    public AppointmentSlot() : base() { } // Parameterless constructor for EF Core  

    public AppointmentSlot(Guid id, Guid agencyId, DateTime startTime, DateTime endTime, int capacity) : base(id)
    {
        Id = id;
        AgencyId = agencyId;
        StartTime = startTime;
        EndTime = endTime;
        Capacity = capacity;
    }

    // Factory method with validation
    public static AppointmentSlot Create(Guid agencyId, DateTime startTime, DateTime endTime, int capacity)
    {
        var slot = new AppointmentSlot(Guid.NewGuid(), agencyId, startTime, endTime, capacity);
        slot.Validate();
        return slot;
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
}
