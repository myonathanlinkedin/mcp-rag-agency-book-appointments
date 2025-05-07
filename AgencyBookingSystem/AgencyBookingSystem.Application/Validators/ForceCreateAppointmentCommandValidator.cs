using FluentValidation;

public class ForceCreateAppointmentCommandValidator : AbstractValidator<ForceCreateAppointmentCommand>
{
    public ForceCreateAppointmentCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Date)
            .GreaterThan(DateTime.UtcNow).WithMessage("Appointment date must be in the future.");

        RuleFor(x => x.AppointmentName)
            .NotEmpty().WithMessage("Appointment name is required.")
            .MaximumLength(100).WithMessage("Appointment name must not exceed 100 characters.");
    }
}
