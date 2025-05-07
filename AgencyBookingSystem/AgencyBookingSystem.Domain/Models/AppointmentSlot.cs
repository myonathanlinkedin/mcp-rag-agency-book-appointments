public class AppointmentSlot
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Capacity { get; set; }
}
