public class Holiday : Entity, IAggregateRoot
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public DateTime Date { get; set; }
    public string Reason { get; set; } = string.Empty;
}
