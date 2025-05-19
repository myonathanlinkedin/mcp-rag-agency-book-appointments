

public class JobStatusEntityEvent : IDomainEvent
{
    private Guid id;
    public JobStatusEntityEvent(Guid id)
    {
        this.id = id;
    }

    public Guid AggregateId => id;
}
