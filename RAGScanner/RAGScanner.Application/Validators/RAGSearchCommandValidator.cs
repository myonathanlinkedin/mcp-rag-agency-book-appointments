using FluentValidation;

public class RAGSearchCommandValidator : AbstractValidator<RAGSearchCommand>
{
    public RAGSearchCommandValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .MinimumLength(CommonModelConstants.Common.MinNameLength)
            .MaximumLength(CommonModelConstants.Common.MaxNameLength)
            .WithMessage("Query must be between "
                        + CommonModelConstants.Common.MinNameLength
                        + " and "
                        + CommonModelConstants.Common.MaxNameLength + " characters.");

        RuleFor(x => x.TopK)
            .GreaterThanOrEqualTo(CommonModelConstants.Common.Zero)
            .WithMessage("TopK must be a non-negative integer.");
    }
}
