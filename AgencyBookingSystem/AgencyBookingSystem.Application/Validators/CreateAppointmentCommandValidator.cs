using FluentValidation;

public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(a => a.AgencyEmail)
            .Must(email => string.IsNullOrWhiteSpace(email) || email.Contains("@"))
            .WithMessage("Agency email must be a valid email address or left blank.");

        RuleFor(a => a.UserEmail)
            .NotEmpty().WithMessage("Customer email is required.")
            .EmailAddress().WithMessage("Customer email must be a valid email address.")
            .MaximumLength(150).WithMessage("Customer email must not exceed 150 characters.");

        RuleFor(a => a.Date)
            .NotEmpty().WithMessage("Appointment date is required.")
            .GreaterThan(DateTime.UtcNow).WithMessage("Appointment date must be in the future.")
            .LessThan(DateTime.UtcNow.AddMonths(6)).WithMessage("Appointments cannot be booked more than 6 months in advance.");

        RuleFor(a => a.AppointmentName)
            .NotEmpty().WithMessage("Appointment name is required.")
            .MinimumLength(3).WithMessage("Appointment name must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Appointment name must not exceed 100 characters.")
            .Matches(@"^[\w\s\-\.]+$").WithMessage("Appointment name can only contain letters, numbers, spaces, hyphens, and periods.");
    }
}
