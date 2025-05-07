
public class AppointmentDto
{
    public Guid AppointmentId { get; set; }
    public string AppointmentName { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; }
    public string AgencyName { get; set; }
    public string AgencyEmail { get; set; }
    public string UserEmail { get; set; }
}