
public class AgencyEntityEvent : IDomainEvent
{
    private Guid userId;

    public AgencyEntityEvent(Guid userId)
    {
        this.userId = userId == Guid.Empty
            ? throw new ArgumentNullException(nameof(userId))
            : userId;
    }

    public Guid AggregateId => userId;
}
