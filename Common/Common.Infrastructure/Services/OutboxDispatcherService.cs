using Marten;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class OutboxDispatcherService : BackgroundService
{
    private readonly IDocumentStore store;
    private readonly IEventDispatcher dispatcher;
    private readonly ILogger<OutboxDispatcherService> logger;

    public OutboxDispatcherService(IDocumentStore store, IEventDispatcher dispatcher, ILogger<OutboxDispatcherService> logger)
    {
        this.store = store;
        this.dispatcher = dispatcher;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("OutboxDispatcherService started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
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
                        logger.LogInformation("Dispatched event {EventType} with ID {MessageId}", message.EventType, message.Id);
                    }
                    catch (Exception ex)
                    {
                        message.RetryCount++;
                        message.Error = ex.Message;
                        logger.LogError(ex, "Failed to dispatch event {EventType} with ID {MessageId}, retry count: {RetryCount}", message.EventType, message.Id, message.RetryCount);
                    }
                }

                await session.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in OutboxDispatcherService loop");
            }

            await Task.Delay(1000, cancellationToken);
        }

        logger.LogInformation("OutboxDispatcherService stopped");
    }
}
