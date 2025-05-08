using FluentValidation;

public class UpdateAgencySettingsCommandValidator : AbstractValidator<UpdateAgencySettingsCommand>
{
    public UpdateAgencySettingsCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("AppointmentId is required.")
            .Must(id => id != Guid.Empty).WithMessage("AppointmentId must be a valid GUID.");

        RuleFor(x => x.MaxAppointmentsPerDay)
            .GreaterThan(0).WithMessage("MaxAppointmentsPerDay must be greater than zero.");

        RuleFor(x => x.Holidays)
            .NotNull().WithMessage("Holidays list cannot be null.")
            .Must(holidays => holidays.All(h => h.Date > DateTime.UtcNow))
            .WithMessage("Holiday dates must be in the future.");
    }
}
