public class Appointment : Entity, IAggregateRoot
{
    public Guid Id { get; set; }
    public Guid AgencyUserId { get; set; } // Links to AgencyUser instead of Identity User directly
    public Guid AgencyId { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Pending";
    public string Token { get; set; } = string.Empty;

    public AgencyUser AgencyUser { get; set; } = default!;
}
