using FluentValidation;

public class UserPromptCommandValidator : AbstractValidator<UserPromptCommand>
{
    public UserPromptCommandValidator()
    {
        RuleFor(x => x.Prompt)
            .NotEmpty()
            .MinimumLength(CommonModelConstants.Common.MinNameLength)
            .WithMessage($"Prompt minimal character is {CommonModelConstants.Common.MinNameLength}");
    }
}
