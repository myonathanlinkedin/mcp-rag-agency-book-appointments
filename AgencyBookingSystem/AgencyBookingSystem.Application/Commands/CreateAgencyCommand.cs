using MediatR;
using Microsoft.Extensions.Logging;

public class CreateAgencyCommand : IRequest<Result>
{
    public string Name { get; }
    public string Email { get; }
    public int MaxAppointmentsPerDay { get; }

    public CreateAgencyCommand(string name, string email, int maxAppointmentsPerDay)
    {
        Name = name;
        Email = email;
        MaxAppointmentsPerDay = maxAppointmentsPerDay;
    }

    public class CreateAgencyCommandHandler : IRequestHandler<CreateAgencyCommand, Result>
    {
        private readonly IAgencyService agencyService;
        private readonly ILogger<CreateAgencyCommandHandler> logger;

        public CreateAgencyCommandHandler(IAgencyService agencyService, ILogger<CreateAgencyCommandHandler> logger)
        {
            this.agencyService = agencyService;
            this.logger = logger;
        }

        public async Task<Result> Handle(CreateAgencyCommand request, CancellationToken cancellationToken)
        {
            if (await agencyService.GetByEmailAsync(request.Email) != null)
            {
                logger.LogWarning("Agency creation failed. Email {Email} is already in use.", request.Email);
                return Result.Failure(new[] { "An agency with this email already exists." });
            }

            var agencyResult = Agency.Create(
                request.Name,
                request.Email,
                true, // requiresApproval
                request.MaxAppointmentsPerDay);

            if (!agencyResult.Succeeded)
            {
                logger.LogError("Agency creation failed. {Errors}", string.Join(", ", agencyResult.Errors));
                return agencyResult;
            }

            var agency = agencyResult.Data;
            await agencyService.UpsertAsync(agency, cancellationToken);

            logger.LogInformation("Agency {Name} created successfully.", agency.Name);
            return Result.Success;
        }
    }
}
