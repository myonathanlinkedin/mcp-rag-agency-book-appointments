using MediatR;
using Microsoft.Extensions.Logging;

public class ApproveAgencyCommand : IRequest<Result>
{
    public string Email { get; }

    public ApproveAgencyCommand(string email)
    {
        Email = email;
    }
}

public class ApproveAgencyCommandHandler : IRequestHandler<ApproveAgencyCommand, Result>
{
    private readonly IAgencyService agencyService;
    private readonly ILogger<ApproveAgencyCommandHandler> logger;

    public ApproveAgencyCommandHandler(IAgencyService agencyService, ILogger<ApproveAgencyCommandHandler> logger)
    {
        this.agencyService = agencyService;
        this.logger = logger;
    }

    public async Task<Result> Handle(ApproveAgencyCommand request, CancellationToken cancellationToken)
    {
        var agency = await agencyService.GetByEmailAsync(request.Email);
        if (agency == null)
        {
            logger.LogWarning("Failed to approve agency. No agency found with email {Email}.", request.Email);
            return Result.Failure(new[] { "No agency found with this email." });
        }

        var result = await agencyService.ApproveAgencyAsync(agency.Id, cancellationToken);
        if (!result.Succeeded)
        {
            logger.LogWarning("Failed to approve agency with email {Email}.", request.Email);
            return Result.Failure(new[] { "Agency approval failed." });
        }

        logger.LogInformation("Agency with email {Email} approved successfully.", request.Email);
        return Result.Success;
    }
}
