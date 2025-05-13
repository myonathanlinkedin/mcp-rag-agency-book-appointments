using FluentValidation;

public class UpdateAgencySettingsCommandValidator : AbstractValidator<UpdateAgencySettingsCommand>
{
    public UpdateAgencySettingsCommandValidator()
    {
        RuleFor(x => x.AgencyEmail)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.AgencyEmail))
            .WithMessage("Please provide a valid email address.");

        RuleFor(x => x.MaxAppointmentsPerDay)
            .GreaterThan(0).WithMessage("Maximum appointments per day must be greater than zero.");

        RuleFor(x => x.Holidays)
            .NotNull().WithMessage("Holidays list cannot be null.");

        RuleForEach(x => x.Holidays)
            .ChildRules(holiday =>
            {
                holiday.RuleFor(h => h.Reason)
                    .NotEmpty()
                    .WithMessage("Holiday reason is required.");
            })
            .When(x => x.Holidays != null && x.Holidays.Any());

        When(x => x.IsApproved.HasValue, () =>
        {
            RuleFor(x => x.IsApproved)
                .NotNull().WithMessage("Approval status must have a value when provided.");
        });
    }
}
