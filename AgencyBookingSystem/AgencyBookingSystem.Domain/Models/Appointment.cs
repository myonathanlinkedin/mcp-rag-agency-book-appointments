public class Appointment
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid AgencyId { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Canceled, No-Show
    public string Token { get; set; } = string.Empty;
}
