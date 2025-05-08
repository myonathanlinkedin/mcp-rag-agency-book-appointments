using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class CreateAppointmentCommand : IRequest<Result>
{
    public string? AgencyEmail { get; } // Optional: Admin must provide, otherwise retrieved from context
    public string UserEmail { get; }
    public DateTime Date { get; }
    public string AppointmentName { get; }

    public CreateAppointmentCommand(string? agencyEmail, string userEmail, DateTime date, string appointmentName)
    {
        AgencyEmail = agencyEmail;
        UserEmail = userEmail;
        Date = date;
        AppointmentName = appointmentName;
    }

    public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, Result>
    {
        private readonly IAppointmentService appointmentService;
        private readonly IAgencyService agencyService;
        private readonly ILogger<CreateAppointmentCommandHandler> logger;
        private readonly IHttpContextAccessor httpContextAccessor;

        public CreateAppointmentCommandHandler(
            IAppointmentService appointmentService,
            IAgencyService agencyService,
            ILogger<CreateAppointmentCommandHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            this.appointmentService = appointmentService;
            this.agencyService = agencyService;
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
        {
            var user = httpContextAccessor.HttpContext?.User;
            var userEmail = user?.FindFirst(ClaimTypes.Email)?.Value;
            var isAdmin = user?.IsInRole(CommonModelConstants.Role.Administrator) ?? false;

            if (string.IsNullOrEmpty(userEmail))
            {
                logger.LogWarning("Failed to create appointment. No valid user email found in HttpContext.");
                return Result.Failure(new[] { "No valid user email found in HttpContext." });
            }

            // Determine agency email: Admin must provide it, otherwise retrieve from user context
            string agencyEmail = request.AgencyEmail ?? userEmail;

            if (request.AgencyEmail != null && !isAdmin)
            {
                logger.LogWarning("Unauthorized appointment creation attempt. Only administrators can create for another agency.");
                return Result.Failure(new[] { "Only administrators can create appointments for another agency." });
            }

            var agency = await agencyService.GetByEmailAsync(agencyEmail);
            if (agency == null)
            {
                logger.LogWarning("Failed to create appointment. No agency found for email {AgencyEmail}.", agencyEmail);
                return Result.Failure(new[] { "No agency found for this email." });
            }

            var userExists = await agencyService.GetAgencyUserByEmailAsync(request.UserEmail);
            if (userExists == null)
            {
                logger.LogWarning("Failed to create appointment. No user found for email {UserEmail}.", request.UserEmail);
                return Result.Failure(new[] { "No user found for this email." });
            }

            var result = await appointmentService.CreateAppointmentAsync(agency.Id, request.UserEmail, request.AppointmentName, request.Date, cancellationToken);
            if (!result.Succeeded)
            {
                logger.LogWarning("Failed to create appointment '{AppointmentName}' for Agency {AgencyEmail}, User {UserEmail}. Reason: {Errors}",
                    request.AppointmentName, agencyEmail, request.UserEmail, string.Join(", ", result.Errors));
                return Result.Failure(result.Errors);
            }

            logger.LogInformation("Appointment '{AppointmentName}' created successfully for Agency {AgencyEmail}, User {UserEmail}.",
                request.AppointmentName, agencyEmail, request.UserEmail);
            return Result.Success;
        }
    }
}
