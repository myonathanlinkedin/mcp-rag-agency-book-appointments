using FluentValidation;

public class AssignUserToAgencyCommandValidator : AbstractValidator<AssignUserToAgencyCommand>
{
    public AssignUserToAgencyCommandValidator()
    {
        RuleFor(u => u.AgencyEmail)
            .Must(email => string.IsNullOrWhiteSpace(email) || email.Contains("@"))
            .WithMessage("Agency email must be a valid email or left blank.");

        RuleFor(u => u.UserEmail)
            .NotEmpty().WithMessage("User email is required.")
            .EmailAddress().WithMessage("A valid user email is required.");

        RuleFor(u => u.Roles)
            .NotEmpty().WithMessage("Roles are required.");
    }
}
