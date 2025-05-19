
public class AgencyUserEntityEvent : IDomainEvent
{
    private Guid agencyId;

    public AgencyUserEntityEvent(Guid agencyId)
    {
        this.agencyId = agencyId == Guid.Empty
            ? throw new ArgumentNullException(nameof(agencyId))
            : agencyId;
    }

    public Guid AggregateId => agencyId;
}
