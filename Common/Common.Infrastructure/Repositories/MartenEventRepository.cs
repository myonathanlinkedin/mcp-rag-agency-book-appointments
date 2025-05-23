﻿using Marten;

public class MartenEventRepository : IEventRepository
{
    private readonly IDocumentStore documentStore;

    public MartenEventRepository(IDocumentStore documentStore)
    {
        this.documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    public async Task Store(OutboxMessage outboxMessage, CancellationToken token)
    {
        if (outboxMessage == null)
            return;

        using var session = documentStore.LightweightSession();
        session.Events.StartStream<IAggregateRoot>(outboxMessage.Id, outboxMessage);
        await session.SaveChangesAsync();
    }

    public async Task AppendToStream(Guid aggregateId, IEnumerable<IDomainEvent> events, CancellationToken token)
    {
        if (events == null || !events.Any())
            return;

        using var session = documentStore.LightweightSession();
        
        // Check if stream exists
        var streamExists = await session.Events.FetchStreamStateAsync(aggregateId);
        
        if (streamExists == null)
        {
            // Start new stream if it doesn't exist
            session.Events.StartStream<IAggregateRoot>(aggregateId, events.ToArray());
        }
        else
        {
            // Append to existing stream
            session.Events.Append(aggregateId, events.ToArray());
        }
        
        await session.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<IDomainEvent>> LoadEvents(Guid aggregateId, CancellationToken token)
    {
        using var session = documentStore.QuerySession();
        var events = await session.Events.FetchStreamAsync(aggregateId);
        return events.Select(e => (IDomainEvent)e.Data).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyCollection<IDomainEvent>> LoadEventsSince(Guid aggregateId, DateTime since, CancellationToken token)
    {
        using var session = documentStore.QuerySession();
        var events = await session.Events
            .FetchStreamAsync(aggregateId, timestamp: since);
        return events.Select(e => (IDomainEvent)e.Data).ToList().AsReadOnly();
    }

    public Task<IDocumentSession> OpenSession(CancellationToken token)
    {
        return documentStore.LightweightSerializableSessionAsync(token);
    }
}