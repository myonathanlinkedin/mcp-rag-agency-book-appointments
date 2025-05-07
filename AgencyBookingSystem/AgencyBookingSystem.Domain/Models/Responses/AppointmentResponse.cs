public class AppointmentResponse
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
