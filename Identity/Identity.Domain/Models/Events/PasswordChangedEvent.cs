
public class PasswordChangedEvent : IDomainEvent
{
    public Guid Id { get; }
    public string Email { get; }
    public string NewPassword { get; }

    public Guid AggregateId => Id;

    public PasswordChangedEvent(string email, string newPassword)
    {
        Id = Guid.NewGuid();
        Email = email;
        NewPassword = newPassword;
    }
}
