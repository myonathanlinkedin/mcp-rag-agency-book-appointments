public class AgencyUser : Entity, IAggregateRoot
{
    public Guid Id { get; set; } // Local DB identifier
    public string IdentityUserId { get; set; } // Maps to Identity Server User ID
    public Guid AgencyId { get; set; } // Links user to a specific agency
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new(); // e.g., Admin, Staff, Customer
}
