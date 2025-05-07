using FluentValidation;

public class ScanUrlCommandValidator : AbstractValidator<ScanUrlCommand>
{
    public ScanUrlCommandValidator()
    {
        RuleFor(x => x.Urls)
            .NotEmpty()
            .WithMessage("At least one URL must be provided for scanning.");

        RuleForEach(x => x.Urls)
            .NotEmpty()
            .MaximumLength(CommonModelConstants.Common.MaxUrlLength)
            .Matches(@"^https?:\/\/")
            .WithMessage("Each URL must be a valid HTTP or HTTPS link.");
    }
}
