
public class PasswordResetEvent : IDomainEvent
{
    public Guid Id { get; }
    public string Email { get; }
    public string NewPassword { get; }

    public Guid AggregateId => Id;

    public PasswordResetEvent(string email, string newPassword)
    {
        Id = Guid.NewGuid();
        Email = email;
        NewPassword = newPassword;
    }
}