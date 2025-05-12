public interface IDomainEvent
{
    Guid AggregateId { get; }
}