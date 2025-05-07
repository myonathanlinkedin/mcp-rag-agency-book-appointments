using FluentValidation;

public class ApproveAgencyCommandValidator : AbstractValidator<ApproveAgencyCommand>
{
    public ApproveAgencyCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Agency email is required.")
            .EmailAddress().WithMessage("Invalid email format.");
    }
}
