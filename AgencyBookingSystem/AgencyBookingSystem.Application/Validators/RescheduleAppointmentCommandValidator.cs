using FluentValidation;

public class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
{
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("Appointment ID is required.");

        RuleFor(x => x.NewDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Rescheduled date must be in the future.");
    }
}
