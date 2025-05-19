using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

public class AgencyUser : Entity, IAggregateRoot
{
    public Guid AgencyId { get; private set; }
    
    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; private set; }
    
    [Required]
    [StringLength(100)]
    public string FullName { get; private set; }
    
    private readonly List<string> roles = new();
    public IReadOnlyCollection<string> Roles => roles.AsReadOnly();

    public AgencyUser() : base()
    {
        // Default constructor for EF Core
    }   

    // Main constructor
    public AgencyUser(
        Guid id,
        Guid agencyId,
        string email,
        string fullName) : base(id)
    {
        AgencyId = agencyId;
        Email = email;
        FullName = fullName;

        RaiseEvent(new AgencyUserEntityEvent(id));
    }

    // Factory method for creating a new agency user
    public static Result<AgencyUser> Create(
        Guid agencyId,
        string email,
        string fullName,
        IReadOnlyCollection<string> roles)
    {
        var user = new AgencyUser(
            id: Guid.NewGuid(),
            agencyId: agencyId,
            email: email,
            fullName: fullName);

        var addRolesResult = user.AddRoles(roles.ToList());
        if (!addRolesResult.Succeeded)
        {
            return Result<AgencyUser>.Failure(addRolesResult.Errors);
        }

        var validationResult = user.Validate();
        if (!validationResult.Succeeded)
        {
            return Result<AgencyUser>.Failure(validationResult.Errors);
        }

        return Result<AgencyUser>.SuccessWith(user);
    }

    // Domain methods
    public Result UpdateDetails(string fullName, IReadOnlyCollection<string> newRoles)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result.Failure(new[] { "Full name is required." });
        }

        if (fullName.Length > 100)
        {
            return Result.Failure(new[] { "Full name must not exceed 100 characters." });
        }

        var addRolesResult = AddRoles(newRoles.ToList());
        if (!addRolesResult.Succeeded)
        {
            return addRolesResult;
        }

        FullName = fullName;
        return Result.Success;
    }

    public Result AddRoles(List<string> newRoles)
    {
        if (newRoles == null || !newRoles.Any())
        {
            return Result.Failure(new[] { "At least one role must be provided." });
        }

        foreach (var role in newRoles)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return Result.Failure(new[] { "Role cannot be empty." });
            }

            if (!roles.Contains(role))
            {
                roles.Add(role);
            }
        }

        return Result.Success;
    }

    public Result RemoveRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return Result.Failure(new[] { "Role cannot be empty." });
        }

        if (!roles.Contains(role))
        {
            return Result.Failure(new[] { "Role not found." });
        }

        if (roles.Count == 1)
        {
            return Result.Failure(new[] { "Cannot remove the last role. At least one role is required." });
        }

        roles.Remove(role);
        return Result.Success;
    }

    public bool HasRole(string role)
    {
        return roles.Contains(role);
    }

    private Result Validate()
    {
        var errors = new List<string>();

        if (AgencyId == Guid.Empty)
        {
            errors.Add("Agency ID is required.");
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            errors.Add("Email is required.");
        }
        else if (!new EmailAddressAttribute().IsValid(Email))
        {
            errors.Add("Invalid email format.");
        }
        else if (Email.Length > 150)
        {
            errors.Add("Email must not exceed 150 characters.");
        }
        else if (Email.Contains(" "))
        {
            errors.Add("Email cannot contain spaces.");
        }
        else if (!Regex.IsMatch(Email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
        {
            errors.Add("Email must be in a valid format.");
        }

        if (string.IsNullOrWhiteSpace(FullName))
        {
            errors.Add("Full name is required.");
        }
        else if (FullName.Length > 100)
        {
            errors.Add("Full name must not exceed 100 characters.");
        }

        if (!roles.Any())
        {
            errors.Add("At least one role must be assigned.");
        }
        else if (roles.Count > 5)
        {
            errors.Add("A user cannot have more than 5 roles.");
        }
        else if (roles.Any(role => string.IsNullOrWhiteSpace(role)))
        {
            errors.Add("Role names cannot be empty.");
        }
        else if (roles.Any(role => role.Length > 50))
        {
            errors.Add("Role names cannot exceed 50 characters.");
        }
        else if (roles.Any(role => !CommonModelConstants.AgencyRole.ValidRoles.Contains(role)))
        {
            errors.Add("One or more roles are invalid. Valid roles are: " + string.Join(", ", CommonModelConstants.AgencyRole.ValidRoles));
        }

        return errors.Any() ? Result.Failure(errors) : Result.Success;
    }
}
