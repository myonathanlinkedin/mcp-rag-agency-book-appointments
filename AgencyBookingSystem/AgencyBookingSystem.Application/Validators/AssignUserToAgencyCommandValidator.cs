using FluentValidation;

public class AssignUserToAgencyCommandValidator : AbstractValidator<AssignUserToAgencyCommand>
{
    public AssignUserToAgencyCommandValidator()
    {
        RuleFor(u => u.AgencyEmail)
            .Must(email => string.IsNullOrWhiteSpace(email) || email.Contains("@"))
            .WithMessage("Agency email must be a valid email address or left blank.")
            .MaximumLength(150).When(u => !string.IsNullOrWhiteSpace(u.AgencyEmail))
            .WithMessage("Agency email must not exceed 150 characters.");

        RuleFor(u => u.UserEmail)
            .NotEmpty().WithMessage("User email is required.")
            .EmailAddress().WithMessage("A valid user email is required.")
            .MaximumLength(150).WithMessage("User email must not exceed 150 characters.")
            .Must(email => !email.Contains(" ")).WithMessage("User email cannot contain spaces.")
            .Matches(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")
            .WithMessage("User email must be in a valid format.");

        RuleFor(u => u.Roles)
            .NotEmpty().WithMessage("At least one role is required.")
            .Must(roles => roles.Count <= 5).WithMessage("A user cannot have more than 5 roles.")
            .Must(roles => roles.All(role => !string.IsNullOrWhiteSpace(role)))
            .WithMessage("Role names cannot be empty.")
            .Must(roles => roles.All(role => role.Length <= 50))
            .WithMessage("Role names cannot exceed 50 characters.")
            .Must(roles => roles.All(role => CommonModelConstants.AgencyRole.ValidRoles.Contains(role)))
            .WithMessage("One or more roles are invalid. Valid roles are: " + string.Join(", ", CommonModelConstants.AgencyRole.ValidRoles));

        RuleFor(u => u.UserEmail)
            .NotEqual(u => u.AgencyEmail)
            .When(u => !string.IsNullOrWhiteSpace(u.AgencyEmail))
            .WithMessage("User email cannot be the same as agency email.");
    }
}
