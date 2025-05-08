using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class CreateAppointmentCommand : IRequest<Result>
{
    public string UserEmail { get; }
    public DateTime Date { get; }
    public string AppointmentName { get; } // Added appointment name

    public CreateAppointmentCommand(string userEmail, DateTime date, string appointmentName)
    {
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
            // Retrieve AgencyEmail from HttpContextAccessor
            var agencyEmail = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(agencyEmail))
            {
                logger.LogWarning("Failed to create appointment. No valid AgencyEmail found in HttpContext.");
                return Result.Failure(new[] { "No valid AgencyEmail found in HttpContext." });
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