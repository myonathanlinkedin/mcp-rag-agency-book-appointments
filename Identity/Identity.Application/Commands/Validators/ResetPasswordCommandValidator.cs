using FluentValidation;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MinimumLength(CommonModelConstants.Identity.MinEmailLength)
            .MaximumLength(CommonModelConstants.Identity.MaxEmailLength)
            .EmailAddress()
            .WithMessage("A valid email is required.");
    }
}
