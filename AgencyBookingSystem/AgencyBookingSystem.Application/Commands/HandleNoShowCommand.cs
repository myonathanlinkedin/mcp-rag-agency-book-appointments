using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class HandleNoShowCommand : IRequest<Result>
{
    public Guid AppointmentId { get; }

    public HandleNoShowCommand(Guid appointmentId)
    {
        AppointmentId = appointmentId;
    }

    public class HandleNoShowCommandHandler : IRequestHandler<HandleNoShowCommand, Result>
    {
        private readonly IAppointmentService appointmentService;
        private readonly ILogger<HandleNoShowCommandHandler> logger;
        private readonly IHttpContextAccessor httpContextAccessor;

        public HandleNoShowCommandHandler(
            IAppointmentService appointmentService,
            ILogger<HandleNoShowCommandHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            this.appointmentService = appointmentService;
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result> Handle(HandleNoShowCommand request, CancellationToken cancellationToken)
        {
            // Retrieve user role and email from HttpContextAccessor
            var user = httpContextAccessor.HttpContext?.User;
            var userEmail = user?.FindFirst(ClaimTypes.Email)?.Value;
            var isAdmin = user?.IsInRole("Administrator") ?? false;

            if (string.IsNullOrEmpty(userEmail))
            {
                logger.LogWarning("No-show processing failed. No valid user email found in HttpContext.");
                return Result.Failure(new[] { "No valid user email found in HttpContext." });
            }

            // If the user is an administrator, allow unrestricted access
            if (isAdmin)
            {
                logger.LogInformation("Administrator {UserEmail} is handling no-show for appointment {AppointmentId}.", userEmail, request.AppointmentId);
            }
            else
            {
                // Validate that the appointment belongs to the agency associated with the email
                var appointment = await appointmentService.GetByIdAsync(request.AppointmentId);
                if (appointment == null || appointment.AgencyUser?.Email != userEmail)
                {
                    logger.LogWarning("No-show processing failed. Appointment {AppointmentId} does not belong to agency with email {UserEmail}.", request.AppointmentId, userEmail);
                    return Result.Failure(new[] { "Appointment does not belong to your agency." });
                }
            }

            await appointmentService.HandleNoShowAsync(request.AppointmentId);

            logger.LogInformation("Appointment {AppointmentId} marked as expired due to no-show.", request.AppointmentId);
            return Result.Success;
        }
    }
}