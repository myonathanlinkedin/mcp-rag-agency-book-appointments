using Marten;

public interface IEventRepository
{
    Task AppendToStream(Guid aggregateId, IEnumerable<IDomainEvent> events, CancellationToken token);
    Task Store(OutboxMessage outboxMessage, CancellationToken token);
    Task<IDocumentSession> OpenSession(CancellationToken token);
}