
public class AppointmentSlotEntityEvent : IDomainEvent
{
    private Guid id;

    public AppointmentSlotEntityEvent(Guid id)
    {
        this.id = id == Guid.Empty
            ? throw new ArgumentNullException(nameof(id))
            : id;
    }

    public Guid AggregateId => id;
}
