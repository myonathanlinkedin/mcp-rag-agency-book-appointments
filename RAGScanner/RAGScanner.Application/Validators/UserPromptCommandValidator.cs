using FluentValidation;

public class UserPromptCommandValidator : AbstractValidator<UserPromptCommand>
{
    public UserPromptCommandValidator()
    {
        RuleFor(x => x.Prompt)
            .NotEmpty()
            .MinimumLength(CommonModelConstants.Common.MinNameLength)
            .MaximumLength(CommonModelConstants.Common.MaxNameLength)
            .WithMessage("Prompt must be between "
                        + CommonModelConstants.Common.MinNameLength
                        + " and "
                        + CommonModelConstants.Common.MaxNameLength + " characters.");
    }
}
