using FluentValidation;

public class CreateAgencyCommandValidator : AbstractValidator<CreateAgencyCommand>
{
    public CreateAgencyCommandValidator()
    {
        RuleFor(a => a.Name)
            .NotEmpty().WithMessage("Agency name is required.")
            .MaximumLength(100).WithMessage("Agency name must not exceed 100 characters.");

        RuleFor(a => a.Email)
            .NotEmpty()
            .EmailAddress().WithMessage("A valid email is required.")
            .MaximumLength(150).WithMessage("Email must not exceed 150 characters.");

        RuleFor(a => a.MaxAppointmentsPerDay)
            .GreaterThan(0).WithMessage("Max appointments per day must be at least 1.")
            .LessThanOrEqualTo(50).WithMessage("Max appointments per day should not exceed 50.");
    }
}
