using FluentValidation;

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MinimumLength(CommonModelConstants.Identity.MinEmailLength)
            .MaximumLength(CommonModelConstants.Identity.MaxEmailLength)
            .EmailAddress()
            .WithMessage("A valid email is required.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(CommonModelConstants.Identity.MinPasswordLength)
            .MaximumLength(CommonModelConstants.Identity.MaxPasswordLength)
            .WithMessage("Password length must be between "
                        + CommonModelConstants.Identity.MinPasswordLength
                        + " and "
                        + CommonModelConstants.Identity.MaxPasswordLength + " characters.");
    }
}
