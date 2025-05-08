using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class UpdateAgencySettingsCommand : IRequest<Result>
{
    public Guid AppointmentId { get; }
    public int MaxAppointmentsPerDay { get; }
    public List<Holiday> Holidays { get; }

    public UpdateAgencySettingsCommand(Guid appointmentId, int maxAppointmentsPerDay, List<Holiday> holidays)
    {
        AppointmentId = appointmentId;
        MaxAppointmentsPerDay = maxAppointmentsPerDay;
        Holidays = holidays;
    }

    public class UpdateAgencySettingsCommandHandler : IRequestHandler<UpdateAgencySettingsCommand, Result>
    {
        private readonly IAgencyService agencyService;
        private readonly IAppointmentService appointmentService;
        private readonly ILogger<UpdateAgencySettingsCommandHandler> logger;
        private readonly IHttpContextAccessor httpContextAccessor;

        public UpdateAgencySettingsCommandHandler(
            IAgencyService agencyService,
            IAppointmentService appointmentService,
            ILogger<UpdateAgencySettingsCommandHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            this.agencyService = agencyService;
            this.appointmentService = appointmentService;
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result> Handle(UpdateAgencySettingsCommand request, CancellationToken cancellationToken)
        {
            // Retrieve user role and email from HttpContextAccessor
            var user = httpContextAccessor.HttpContext?.User;
            var userEmail = user?.FindFirst(ClaimTypes.Email)?.Value;
            var isAdmin = user?.IsInRole(CommonModelConstants.Role.Administrator) ?? false;

            if (string.IsNullOrEmpty(userEmail))
            {
                logger.LogWarning("Update failed. No valid user email found in HttpContext.");
                return Result.Failure(new[] { "No valid user email found in HttpContext." });
            }

            // If the user is an administrator, allow unrestricted access
            if (isAdmin)
            {
                logger.LogInformation("Administrator {UserEmail} is updating agency settings.", userEmail);
            }
            else
            {
                // Validate that the appointment belongs to the agency associated with the email
                var appointment = await appointmentService.GetByIdAsync(request.AppointmentId);
                if (appointment == null || appointment.AgencyUser?.Email != userEmail)
                {
                    logger.LogWarning("Update failed. Appointment {AppointmentId} does not belong to agency with email {UserEmail}.", request.AppointmentId, userEmail);
                    return Result.Failure(new[] { "Appointment does not belong to your agency." });
                }
            }

            var agency = await agencyService.GetByEmailAsync(userEmail);
            if (agency == null)
            {
                logger.LogWarning("Update failed. No agency found for email {UserEmail}.", userEmail);
                return Result.Failure(new[] { "Agency not found." });
            }

            // Update settings
            agency.MaxAppointmentsPerDay = request.MaxAppointmentsPerDay;
            agency.Holidays = request.Holidays;

            await agencyService.SaveAsync(agency, cancellationToken);

            logger.LogInformation("Agency settings updated successfully for {UserEmail}.", userEmail);
            return Result.Success;
        }

    }
}