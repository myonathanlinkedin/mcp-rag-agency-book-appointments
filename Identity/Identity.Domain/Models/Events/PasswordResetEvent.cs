public class PasswordResetEvent : IDomainEvent
{
    public string Email { get; }
    public string NewPassword { get; }

    public PasswordResetEvent(string email, string newPassword)
    {
        Email = email;
        NewPassword = newPassword;
    }
}