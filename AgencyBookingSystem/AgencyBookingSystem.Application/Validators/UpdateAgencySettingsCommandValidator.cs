using FluentValidation;

public class UpdateAgencySettingsCommandValidator : AbstractValidator<UpdateAgencySettingsCommand>
{
    public UpdateAgencySettingsCommandValidator()
    {
        RuleFor(x => x.AgencyEmail)
            .Must(email => string.IsNullOrWhiteSpace(email) || email.Contains("@"))
            .WithMessage("Agency email must be a valid email address or left blank.")
            .MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.AgencyEmail))
            .WithMessage("Agency email must not exceed 150 characters.")
            .Matches(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")
            .When(x => !string.IsNullOrWhiteSpace(x.AgencyEmail))
            .WithMessage("Agency email must be in a valid format.");

        RuleFor(x => x.MaxAppointmentsPerDay)
            .GreaterThan(0).WithMessage("Maximum appointments per day must be greater than zero.")
            .LessThanOrEqualTo(50).WithMessage("Maximum appointments per day cannot exceed 50.")
            .Must(max => max % 1 == 0).WithMessage("Maximum appointments per day must be a whole number.");

        RuleFor(x => x.Holidays)
            .NotNull().WithMessage("Holidays list cannot be null.")
            .Must(holidays => holidays.Count <= 100)
            .WithMessage("Cannot have more than 100 holidays.")
            .Must(holidays => !holidays.GroupBy(h => h.Date.Date).Any(g => g.Count() > 1))
            .WithMessage("Cannot have multiple holidays on the same date.");

        RuleForEach(x => x.Holidays).ChildRules(holiday =>
        {
            holiday.RuleFor(h => h.Date)
                .NotEmpty().WithMessage("Holiday date is required.")
                .GreaterThan(DateTime.UtcNow.Date).WithMessage("Holiday date must be in the future.")
                .LessThan(DateTime.UtcNow.AddYears(2)).WithMessage("Holiday date cannot be more than 2 years in the future.");

            holiday.RuleFor(h => h.Reason)
                .NotEmpty().WithMessage("Holiday reason is required.")
                .MinimumLength(3).WithMessage("Holiday reason must be at least 3 characters.")
                .MaximumLength(200).WithMessage("Holiday reason must not exceed 200 characters.")
                .Matches(@"^[\w\s\-\.,!?()]+$").WithMessage("Holiday reason can only contain letters, numbers, spaces, and basic punctuation.");
        });

        When(x => x.IsApproved.HasValue, () =>
        {
            RuleFor(x => x.IsApproved)
                .NotNull().WithMessage("Approval status must have a value when provided.");
        });
    }
}
