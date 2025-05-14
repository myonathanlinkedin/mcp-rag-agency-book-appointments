using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Principal;

public class UserPromptCommand : IRequest<Result<string>>
{
    public string Prompt { get; }

    public UserPromptCommand(string prompt) => Prompt = prompt;

    public class UserPromptCommandHandler : IRequestHandler<UserPromptCommand, Result<string>>
    {
        private readonly IMCPServerRequester mCPServerRequester;
        private readonly IHttpContextAccessor httpContextAccessor;

        public UserPromptCommandHandler(IMCPServerRequester mCPServerRequester, IHttpContextAccessor httpContextAccessor)
        {
            this.mCPServerRequester = mCPServerRequester;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result<string>> Handle(UserPromptCommand request, CancellationToken cancellationToken)
        {
            var token = httpContextAccessor.HttpContext?.Request.Headers["Authorization"]
                .FirstOrDefault()?.Split(" ").Last();

            if (string.IsNullOrWhiteSpace(token))
                return Result<string>.Failure(new List<string> { "Authorization token is missing." });

            return await this.mCPServerRequester.RequestAsync(prompt: request.Prompt, useSession: true, cancellationToken: cancellationToken);
        }
    }
}