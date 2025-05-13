using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class CancelAppointmentCommand : IRequest<Result>
{
    public Guid AppointmentId { get; }

    public CancelAppointmentCommand(Guid appointmentId)
    {
        AppointmentId = appointmentId;
    }

    public class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, Result>
    {
        private readonly IAppointmentService appointmentService;
        private readonly IAgencyService agencyService;
        private readonly ILogger<CancelAppointmentCommandHandler> logger;
        private readonly IHttpContextAccessor httpContextAccessor;

        public CancelAppointmentCommandHandler(
            IAppointmentService appointmentService,
            IAgencyService agencyService,
            ILogger<CancelAppointmentCommandHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            this.appointmentService = appointmentService;
            this.agencyService = agencyService;
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
        {
            // Retrieve user role and email from HttpContextAccessor
            var user = httpContextAccessor.HttpContext?.User;
            var userEmail = user?.FindFirst(ClaimTypes.Name)?.Value;
            var isAdmin = user?.IsInRole(CommonModelConstants.Role.Administrator) ?? false;

            if (string.IsNullOrEmpty(userEmail))
            {
                logger.LogWarning("Cancellation failed. No valid user email found in HttpContext.");
                return Result.Failure(new[] { "No valid user email found in HttpContext." });
            }

            // If the user is an administrator, allow unrestricted access
            if (isAdmin)
            {
                logger.LogInformation("Administrator {UserEmail} is canceling appointment {AppointmentId}.", userEmail, request.AppointmentId);
            }
            else
            {
                // Ensure the appointment belongs to the agency associated with the email
                var appointment = await appointmentService.GetByIdAsync(request.AppointmentId);
                if (appointment == null)
                {
                    logger.LogWarning("Cancellation failed. Appointment {AppointmentId} not found.", request.AppointmentId);
                    return Result.Failure(new[] { "Appointment not found." });
                }

                var agency = await agencyService.GetByIdAsync(appointment.AgencyId);
                if (agency == null || agency.Email != userEmail)
                {
                    logger.LogWarning("Cancellation failed. Appointment {AppointmentId} does not belong to agency with email {UserEmail}.", request.AppointmentId, userEmail);
                    return Result.Failure(new[] { "Appointment does not belong to your agency." });
                }
            }

            await appointmentService.CancelAppointmentAsync(request.AppointmentId, cancellationToken);

            logger.LogInformation("Appointment {AppointmentId} canceled successfully.", request.AppointmentId);
            return Result.Success;
        }
    }
}