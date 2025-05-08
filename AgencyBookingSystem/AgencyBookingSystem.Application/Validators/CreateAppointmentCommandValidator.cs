using FluentValidation;

public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(a => a.AgencyEmail)
            .Must(email => string.IsNullOrWhiteSpace(email) || email.Contains("@"))
            .WithMessage("Agency email must be a valid email or left blank.");

        RuleFor(a => a.UserEmail)
            .NotEmpty().WithMessage("User email is required.")
            .EmailAddress().WithMessage("User email must be a valid email address.");

        RuleFor(a => a.Date)
            .GreaterThan(DateTime.UtcNow).WithMessage("Appointment date must be in the future.");

        RuleFor(a => a.AppointmentName)
            .NotEmpty().WithMessage("Appointment name is required.")
            .MaximumLength(100).WithMessage("Appointment name must not exceed 100 characters.");
    }
}
