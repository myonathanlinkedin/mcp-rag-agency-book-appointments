public class RegisterAgencyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
    public int MaxAppointmentsPerDay { get; set; }
}
