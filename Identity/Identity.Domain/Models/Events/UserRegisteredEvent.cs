public class UserRegisteredEvent : IDomainEvent
{
    public string Email { get; }
    public string Password { get; }

    public UserRegisteredEvent(string email, string password)
    {
        Email = email;
        Password = password;
    }
}