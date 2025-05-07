public class Customer : Entity, IAggregateRoot
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime RegisteredDate { get; set; }
    public List<Appointment> Appointments { get; set; } = new();
}
