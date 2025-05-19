
public class HolidayEntityEvent : IDomainEvent
{
    private Guid id;

    public HolidayEntityEvent(Guid id)
    {
        this.id = id == Guid.Empty
            ? throw new ArgumentNullException(nameof(id))
            : id;
    }

    public Guid AggregateId => id;
}
