using FluentValidation;

public class HandleNoShowCommandValidator : AbstractValidator<HandleNoShowCommand>
{
    public HandleNoShowCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("Appointment ID is required.");
    }
}
