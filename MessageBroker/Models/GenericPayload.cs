
public class GenericPayload
{
    public string Id { get; set; } // Unique identifier for any type of entity (Book, Customer, etc.)
    public string Action { get; set; } // Action Type: CREATE, UPDATE, DELETE
}
