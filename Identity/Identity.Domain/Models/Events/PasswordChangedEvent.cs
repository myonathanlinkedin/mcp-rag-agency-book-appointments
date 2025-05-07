public class PasswordChangedEvent : IDomainEvent
{
    public string Email { get; }
    public string NewPassword { get; }

    public PasswordChangedEvent(string email, string newPassword)
    {
        Email = email;
        NewPassword = newPassword;
    }
}
