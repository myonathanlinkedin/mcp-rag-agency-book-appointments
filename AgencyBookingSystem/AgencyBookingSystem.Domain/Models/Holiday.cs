public class Holiday
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public DateTime Date { get; set; }
    public string Reason { get; set; } = string.Empty;
}
