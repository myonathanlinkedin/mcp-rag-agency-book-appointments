using FluentValidation;

public class UpdateAgencySettingsCommandValidator : AbstractValidator<UpdateAgencySettingsCommand>
{
    public UpdateAgencySettingsCommandValidator()
    {
        RuleFor(x => x.AgencyEmail)
            .Must(email => string.IsNullOrWhiteSpace(email) || email.Contains("@"))
            .WithMessage("AgencyEmail must be a valid email or empty.");

        RuleFor(x => x.MaxAppointmentsPerDay)
            .GreaterThan(0).WithMessage("MaxAppointmentsPerDay must be greater than zero.");

        RuleFor(x => x.Holidays)
            .NotNull().WithMessage("Holidays list cannot be null.")
            .Must(holidays => holidays.All(h => h.Date > DateTime.UtcNow))
            .WithMessage("Holiday dates must be in the future.");
    }
}
