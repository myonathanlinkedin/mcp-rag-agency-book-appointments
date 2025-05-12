using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

public class EventDispatcher : IEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypesCache = new();
    private static readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task>> HandlersCache = new();
    private static readonly Type HandlerType = typeof(IEventHandler<>);
    private static readonly MethodInfo MakeDelegateMethod = typeof(EventDispatcher)
        .GetMethod(nameof(MakeDelegate), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly Type EventHandlerFuncType = typeof(Func<object, object, CancellationToken, Task>);

    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<EventDispatcher>? logger;
    private readonly IEventRepository eventStore;

    public EventDispatcher(IServiceProvider serviceProvider, IEventRepository eventStore, ILogger<EventDispatcher>? logger = null)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        this.logger = logger;
    }

    public async Task Dispatch(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        var eventType = domainEvent.GetType();

        // Persist event to event store
        await eventStore.AppendToStream(domainEvent.AggregateId, new[] { domainEvent }, cancellationToken);

        var handlerInterface = HandlerTypesCache.GetOrAdd(eventType, t => HandlerType.MakeGenericType(t));
        var handlers = serviceProvider.GetServices(handlerInterface);

        var exceptions = new List<Exception>();

        foreach (var handler in handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var handlerType = handler.GetType();

            var handlerDelegate = HandlersCache.GetOrAdd(handlerType, _ =>
            {
                var method = MakeDelegateMethod.MakeGenericMethod(eventType, handlerType);
                var untyped = method.Invoke(null, null)!;
                return (Func<object, object, CancellationToken, Task>)Convert.ChangeType(untyped, EventHandlerFuncType)!;
            });

            try
            {
                await Policy
                    .Handle<Exception>()
                    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)))
                    .ExecuteAsync(() => handlerDelegate(domainEvent, handler, cancellationToken));

                logger?.LogInformation("Dispatched {EventType} to {HandlerType}", eventType.Name, handlerType.Name);
            }
            catch (Exception ex)
            {
                await StoreFailedEvent(domainEvent, ex, cancellationToken);
                logger?.LogError(ex, "Error handling {EventType} with {HandlerType}", eventType.Name, handlerType.Name);
                exceptions.Add(ex);
            }
        }

        if (exceptions.Any())
            throw new AggregateException("One or more event handlers failed", exceptions);
    }

    private static Func<object, object, CancellationToken, Task> MakeDelegate<TEvent, THandler>()
        where TEvent : IDomainEvent
        where THandler : IEventHandler<TEvent>
    {
        return (domainEvent, handler, token) =>
            ((THandler)handler).Handle((TEvent)domainEvent, token);
    }

    private async Task StoreFailedEvent(IDomainEvent domainEvent, Exception ex, CancellationToken cancellationToken = default)
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = domainEvent.AggregateId,
            EventType = domainEvent.GetType().FullName!,
            Payload = JsonSerializer.Serialize(domainEvent),
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            Error = ex.Message
        };

        using var session = await eventStore.OpenSession(cancellationToken);
        session.Store(outboxMessage);
        await session.SaveChangesAsync();
    }
}
