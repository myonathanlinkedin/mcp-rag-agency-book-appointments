using FluentValidation;

public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(a => a.AgencyEmail)
            .NotEmpty().WithMessage("Agency email is required.")
            .EmailAddress().WithMessage("A valid agency email is required.");

        RuleFor(a => a.UserEmail)
            .NotEmpty().WithMessage("User email is required.")
            .EmailAddress().WithMessage("A valid user email is required.");

        RuleFor(a => a.Date)
            .GreaterThan(DateTime.UtcNow).WithMessage("Appointment date must be in the future.");

        RuleFor(a => a.AppointmentName)
            .NotEmpty().WithMessage("Appointment name is required.")
            .MaximumLength(100).WithMessage("Appointment name must not exceed 100 characters.");
    }
}
