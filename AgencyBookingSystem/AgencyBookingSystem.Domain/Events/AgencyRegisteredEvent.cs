public class AgencyRegisteredEvent : IDomainEvent
{
    public Guid AgencyId { get; }
    public string AgencyName { get; }
    public string AgencyEmail { get; }
    public bool RequiresApproval { get; }

    public AgencyRegisteredEvent(Guid agencyId, string agencyName, string agencyEmail, bool requiresApproval)
    {
        AgencyId = ValidateGuid(agencyId, nameof(agencyId));
        AgencyName = ValidateString(agencyName, nameof(agencyName));
        AgencyEmail = ValidateString(agencyEmail, nameof(agencyEmail));
        RequiresApproval = requiresApproval;
    }

    private static Guid ValidateGuid(Guid value, string paramName)
        => value == Guid.Empty ? throw new ArgumentNullException(paramName) : value;

    private static string ValidateString(string value, string paramName)
        => string.IsNullOrWhiteSpace(value) ? throw new ArgumentNullException(paramName) : value;
}
