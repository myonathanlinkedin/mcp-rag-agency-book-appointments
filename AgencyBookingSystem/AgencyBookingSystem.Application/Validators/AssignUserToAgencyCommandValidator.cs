using FluentValidation;

public class AssignUserToAgencyCommandValidator : AbstractValidator<AssignUserToAgencyCommand>
{
    public AssignUserToAgencyCommandValidator()
    {
        RuleFor(u => u.UserEmail)
            .NotEmpty().WithMessage("User email is required.")
            .EmailAddress().WithMessage("A valid user email is required.");

        RuleFor(u => u.Roles)
            .NotEmpty().WithMessage("Roles are required.");
    }
}
