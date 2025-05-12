using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class UpdateAgencySettingsCommand : IRequest<Result>
{
    public string? AgencyEmail { get; } // Optional: If provided, admin must execute
    public int MaxAppointmentsPerDay { get; }
    public List<Holiday> Holidays { get; }
    public bool? IsApproved { get; } // Optional: Only admins can set this

    public UpdateAgencySettingsCommand(
        string? agencyEmail, 
        int maxAppointmentsPerDay, 
        List<Holiday> holidays,
        bool? isApproved = null)
    {
        AgencyEmail = agencyEmail;
        MaxAppointmentsPerDay = maxAppointmentsPerDay;
        Holidays = holidays ?? new List<Holiday>();
        IsApproved = isApproved;
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
            // Get user context
            var user = httpContextAccessor.HttpContext?.User;
            var userEmail = user?.FindFirst(ClaimTypes.Name)?.Value;
            var isAdmin = user?.IsInRole(CommonModelConstants.Role.Administrator) ?? false;
            var isAgency = user?.IsInRole(CommonModelConstants.Role.Agency) ?? false;

            if (string.IsNullOrEmpty(userEmail))
            {
                logger.LogWarning("Settings update failed. No valid user email found in HttpContext.");
                return Result.Failure(new[] { "No valid user email found in HttpContext." });
            }

            // Determine which agency email to use
            string agencyEmail = request.AgencyEmail ?? userEmail;

            // Validate authorization
            if (request.AgencyEmail != null && !isAdmin)
            {
                logger.LogWarning("Unauthorized update attempt. Only administrators can update another agency's settings.");
                return Result.Failure(new[] { "Only administrators can update settings for another agency." });
            }

            if (request.AgencyEmail == null && !isAgency)
            {
                logger.LogWarning("Unauthorized update attempt. User must have Agency role.");
                return Result.Failure(new[] { "User must have Agency role to update agency settings." });
            }

            // Validate IsApproved can only be set by admin
            if (request.IsApproved.HasValue && !isAdmin)
            {
                logger.LogWarning("Unauthorized update attempt. Only administrators can change approval status.");
                return Result.Failure(new[] { "Only administrators can change agency approval status." });
            }

            // Get agency
            var agency = await agencyService.GetByEmailAsync(agencyEmail);
            if (agency == null)
            {
                logger.LogWarning("Failed to update settings. No agency found for email {AgencyEmail}.", agencyEmail);
                return Result.Failure(new[] { "Agency not found." });
            }

            // Update agency details
            var updateResult = agency.UpdateDetails(
                agency.Name, // Keep existing name
                agency.Email, // Keep existing email
                request.MaxAppointmentsPerDay);

            if (!updateResult.Succeeded)
            {
                logger.LogWarning("Failed to update agency details. Errors: {Errors}", string.Join(", ", updateResult.Errors));
                return updateResult;
            }

            // Update approval status if provided by admin
            if (request.IsApproved.HasValue)
            {
                if (request.IsApproved.Value)
                {
                    var approveResult = agency.Approve();
                    if (!approveResult.Succeeded)
                    {
                        logger.LogWarning("Failed to approve agency. Errors: {Errors}", string.Join(", ", approveResult.Errors));
                        return approveResult;
                    }
                }
                else
                {
                    var unapproveResult = agency.Unapprove();
                    if (!unapproveResult.Succeeded)
                    {
                        logger.LogWarning("Failed to unapprove agency. Errors: {Errors}", string.Join(", ", unapproveResult.Errors));
                        return unapproveResult;
                    }
                }
            }

            // Clear existing holidays and add new ones
            agency.ClearHolidays();
            foreach (var holiday in request.Holidays)
            {
                var addHolidayResult = agency.AddHoliday(holiday.Date, holiday.Reason);
                if (!addHolidayResult.Succeeded)
                {
                    logger.LogWarning("Failed to add holiday. Date: {Date}, Reason: {Reason}. Errors: {Errors}", 
                        holiday.Date, holiday.Reason, string.Join(", ", addHolidayResult.Errors));
                    return addHolidayResult;
                }
            }

            // Save changes
            await agencyService.UpsertAsync(agency, cancellationToken);

            logger.LogInformation("Successfully updated settings for agency {AgencyEmail}.", agencyEmail);
            return Result.Success;
        }
    }
}
