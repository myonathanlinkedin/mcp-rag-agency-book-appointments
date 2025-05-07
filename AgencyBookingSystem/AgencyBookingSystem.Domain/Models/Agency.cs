public class Agency
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
    public List<AppointmentSlot> Slots { get; set; } = new();
    public List<Holiday> Holidays { get; set; } = new();
    public int MaxAppointmentsPerDay { get; set; }
}
