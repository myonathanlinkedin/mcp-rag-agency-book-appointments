public class EntityOperation<TEntity> where TEntity : Entity, IAggregateRoot
{
    public TEntity Entity { get; set; }
    public OperationType OperationType { get; set; }
    public DateTime Timestamp { get; set; }
}