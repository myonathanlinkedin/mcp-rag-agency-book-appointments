public class JobStatus : Entity, IAggregateRoot
{
    public string JobId { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
    public List<string> Urls { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}