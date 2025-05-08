using FluentValidation;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(u => u.Email)
            .NotEmpty()
            .MinimumLength(CommonModelConstants.Identity.MinEmailLength)
            .MaximumLength(CommonModelConstants.Identity.MaxEmailLength)
            .EmailAddress()
            .WithMessage("A valid email is required.");

        RuleFor(u => u.Password)
            .NotEmpty()
            .MinimumLength(CommonModelConstants.Identity.MinPasswordLength)
            .MaximumLength(CommonModelConstants.Identity.MaxPasswordLength)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
            .WithMessage("Password must be strong.");

        RuleFor(u => u.ConfirmPassword)
            .NotEmpty()
            .Equal(u => u.Password)
            .WithMessage("The password and confirmation password do not match.");
    }
}
