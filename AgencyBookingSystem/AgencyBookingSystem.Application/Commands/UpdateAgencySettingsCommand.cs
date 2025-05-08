using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class UpdateAgencySettingsCommand : IRequest<Result>
{
    public string? AgencyEmail { get; } // Optional: If provided, admin must execute
    public int MaxAppointmentsPerDay { get; }
    public List<Holiday> Holidays { get; }

    public UpdateAgencySettingsCommand(string? agencyEmail, int maxAppointmentsPerDay, List<Holiday> holidays)
    {
        AgencyEmail = agencyEmail;
        MaxAppointmentsPerDay = maxAppointmentsPerDay;
        Holidays = holidays;
    }

    public class UpdateAgencySettingsCommandHandler : IRequestHandler<UpdateAgencySettingsCommand, Result>
    {
        private readonly IAgencyService agencyService;
        private readonly ILogger<UpdateAgencySettingsCommandHandler> logger;
        private readonly IHttpContextAccessor httpContextAccessor;

        public UpdateAgencySettingsCommandHandler(
            IAgencyService agencyService,
            ILogger<UpdateAgencySettingsCommandHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            this.agencyService = agencyService;
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result> Handle(UpdateAgencySettingsCommand request, CancellationToken cancellationToken)
        {
            var user = httpContextAccessor.HttpContext?.User;
            var userEmail = user?.FindFirst(ClaimTypes.Email)?.Value;
            var isAdmin = user?.IsInRole(CommonModelConstants.Role.Administrator) ?? false;

            if (string.IsNullOrEmpty(userEmail))
            {
                logger.LogWarning("Update failed. No valid user email found in HttpContext.");
                return Result.Failure(new[] { "No valid user email found in HttpContext." });
            }

            string agencyEmail = request.AgencyEmail ?? userEmail;

            if (request.AgencyEmail != null && !isAdmin)
            {
                logger.LogWarning("Unauthorized update attempt. Only administrators can update another agency.");
                return Result.Failure(new[] { "Only administrators can update agency settings for another agency." });
            }

            var agency = await agencyService.GetByEmailAsync(agencyEmail);
            if (agency == null)
            {
                logger.LogWarning("Update failed. No agency found for email {AgencyEmail}.", agencyEmail);
                return Result.Failure(new[] { "Agency not found." });
            }

            agency.MaxAppointmentsPerDay = request.MaxAppointmentsPerDay;
            agency.Holidays = request.Holidays;

            await agencyService.SaveAsync(agency, cancellationToken);

            logger.LogInformation("Agency settings updated successfully for {AgencyEmail}.", agencyEmail);
            return Result.Success;
        }
    }
}
