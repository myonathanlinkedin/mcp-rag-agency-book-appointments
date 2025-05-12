
public class UserRegisteredEvent : IDomainEvent
{
    public Guid Id { get; }
    public string Email { get; }
    public string Password { get; }

    public Guid AggregateId => Id;

    public UserRegisteredEvent(string email, string password)
    {
        Id = Guid.NewGuid();
        Email = email;
        Password = password;
    }
}