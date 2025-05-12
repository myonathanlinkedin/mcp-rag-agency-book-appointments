public interface IEventDispatcher
{
    Task Dispatch(IDomainEvent domainEvent, CancellationToken cancellationToken);
}