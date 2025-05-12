public class AgencyUserAssignedEvent : IDomainEvent
{
    public Guid AgencyId { get; }
    public string AgencyName { get; }
    public string AgencyEmail { get; }

    public Guid? UserId { get; } // Nullable for flexibility
    public string? UserEmail { get; } // Nullable for flexibility
    public string? FullName { get; } // Nullable for flexibility
    public List<string>? Roles { get; } // Nullable for flexibility

    public Guid AggregateId => AgencyId;

    // Constructor for agency-only assignments
    public AgencyUserAssignedEvent(Guid agencyId, string agencyName, string agencyEmail)
    {
        AgencyId = ValidateGuid(agencyId, nameof(agencyId));
        AgencyName = ValidateString(agencyName, nameof(agencyName));
        AgencyEmail = ValidateString(agencyEmail, nameof(agencyEmail));
    }

    // Constructor for full user assignments
    public AgencyUserAssignedEvent(Guid userId, Guid agencyId, string userEmail, string fullName, List<string> roles)
    {
        AgencyId = ValidateGuid(agencyId, nameof(agencyId));
        UserId = ValidateGuid(userId, nameof(userId));
        UserEmail = ValidateString(userEmail, nameof(userEmail));
        FullName = ValidateString(fullName, nameof(fullName));
        Roles = roles?.Count > 0 ? roles : throw new ArgumentNullException(nameof(roles));
    }

    private static Guid ValidateGuid(Guid value, string paramName)
        => value == Guid.Empty ? throw new ArgumentNullException(paramName) : value;

    private static string ValidateString(string value, string paramName)
        => string.IsNullOrWhiteSpace(value) ? throw new ArgumentNullException(paramName) : value;
}
