using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class RescheduleAppointmentCommand : IRequest<Result>
{
    public Guid AppointmentId { get; }
    public DateTime NewDate { get; }

    public RescheduleAppointmentCommand(Guid appointmentId, DateTime newDate)
    {
        AppointmentId = appointmentId;
        NewDate = newDate;
    }

    public class RescheduleAppointmentCommandHandler : IRequestHandler<RescheduleAppointmentCommand, Result>
    {
        private readonly IAppointmentService appointmentService;
        private readonly ILogger<RescheduleAppointmentCommandHandler> logger;
        private readonly IHttpContextAccessor httpContextAccessor;

        public RescheduleAppointmentCommandHandler(
            IAppointmentService appointmentService,
            ILogger<RescheduleAppointmentCommandHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            this.appointmentService = appointmentService;
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
        {
            // Retrieve user role and email from HttpContextAccessor
            var user = httpContextAccessor.HttpContext?.User;
            var userEmail = user?.FindFirst(ClaimTypes.Email)?.Value;
            var isAdmin = user?.IsInRole("Administrator") ?? false;

            if (string.IsNullOrEmpty(userEmail))
            {
                logger.LogWarning("Reschedule failed. No valid user email found in HttpContext.");
                return Result.Failure(new[] { "No valid user email found in HttpContext." });
            }

            // If the user is an administrator, allow unrestricted access
            if (isAdmin)
            {
                logger.LogInformation("Administrator {UserEmail} is rescheduling appointment {AppointmentId}.", userEmail, request.AppointmentId);
            }
            else
            {
                // Ensure the appointment belongs to the agency associated with the email
                var appointment = await appointmentService.GetByIdAsync(request.AppointmentId);
                if (appointment == null || appointment.AgencyUser?.Email != userEmail)
                {
                    logger.LogWarning("Reschedule failed. Appointment {AppointmentId} does not belong to agency with email {UserEmail}.", request.AppointmentId, userEmail);
                    return Result.Failure(new[] { "Appointment does not belong to your agency." });
                }
            }

            await appointmentService.RescheduleAppointmentAsync(request.AppointmentId, request.NewDate, cancellationToken);

            logger.LogInformation("Appointment {AppointmentId} successfully rescheduled to {NewDate}.", request.AppointmentId, request.NewDate);
            return Result.Success;
        }
    }
}