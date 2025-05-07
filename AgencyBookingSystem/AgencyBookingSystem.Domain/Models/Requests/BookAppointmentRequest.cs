public class BookAppointmentRequest
{
    public Guid CustomerId { get; set; }
    public Guid AgencyId { get; set; }
    public DateTime Date { get; set; }
}
