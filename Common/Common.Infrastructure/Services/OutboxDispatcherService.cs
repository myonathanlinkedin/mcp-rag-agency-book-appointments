using Marten;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

public class OutboxDispatcherService : BackgroundService
{
    private readonly IDocumentStore store;
    private readonly IEventDispatcher dispatcher;

    public OutboxDispatcherService(IDocumentStore store, IEventDispatcher dispatcher)
    {
        this.store = store;
        this.dispatcher = dispatcher;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var session = store.LightweightSession();

            var pending = session.Query<OutboxMessage>()
                .Where(x => x.ProcessedAt == null && x.RetryCount < 5)
                .OrderBy(x => x.CreatedAt)
                .Take(10)
                .ToList();

            foreach (var message in pending)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var eventType = Type.GetType(message.EventType)!;
                    var domainEvent = (IDomainEvent)JsonSerializer.Deserialize(message.Payload, eventType)!;

                    await dispatcher.Dispatch(domainEvent, cancellationToken);

                    message.ProcessedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    message.RetryCount++;
                    message.Error = ex.Message;
                }
            }

            await session.SaveChangesAsync();
            await Task.Delay(1000, cancellationToken);
        }
    }
}