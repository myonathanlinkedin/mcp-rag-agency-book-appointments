public class OutboxMessage : Entity, IAggregateRoot
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public string EventType { get; set; }
    public string Payload { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string Error { get; set; }
}