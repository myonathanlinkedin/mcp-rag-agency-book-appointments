using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class CreateAppointmentCommand : IRequest<Result>
{
    public string? AgencyEmail { get; }  // Optional: Admin can specify, otherwise uses logged-in Agency user's email
    public string UserEmail { get; }  // This is the customer's email
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
            // Get user context
            var user = httpContextAccessor.HttpContext?.User;
            var userEmail = user?.FindFirst(ClaimTypes.Name)?.Value;
            var isAdmin = user?.IsInRole(CommonModelConstants.Role.Administrator) ?? false;
            var isAgency = user?.IsInRole(CommonModelConstants.Role.Agency) ?? false;

            if (string.IsNullOrEmpty(userEmail))
            {
                logger.LogWarning("Appointment creation failed. No valid user email found in HttpContext.");
                return Result.Failure(new[] { "No valid user email found in HttpContext." });
            }

            // Determine which agency email to use
            string agencyEmail = request.AgencyEmail ?? userEmail;

            // Validate authorization
            if (request.AgencyEmail != null && !isAdmin)
            {
                logger.LogWarning("Unauthorized appointment creation attempt. Only administrators can create for another agency.");
                return Result.Failure(new[] { "Only administrators can create appointments for another agency." });
            }

            if (request.AgencyEmail == null && !isAgency)
            {
                logger.LogWarning("Unauthorized appointment creation attempt. User must have Agency role.");
                return Result.Failure(new[] { "User must have Agency role to create appointments." });
            }

            // Get agency
            var agency = await agencyService.GetByEmailAsync(agencyEmail);
            if (agency == null)
            {
                logger.LogWarning("Failed to create appointment. No agency found for email {AgencyEmail}.", agencyEmail);
                return Result.Failure(new[] { "Agency not found." });
            }

            // Validate agency is approved
            if (!agency.IsApproved)
            {
                logger.LogWarning("Failed to create appointment. Agency {AgencyEmail} is not approved.", agencyEmail);
                return Result.Failure(new[] { "Agency is not approved for bookings." });
            }

            // Validate appointment slot availability
            if (!await appointmentService.HasAvailableSlotAsync(agency.Id, request.Date))
            {
                logger.LogWarning("Failed to create appointment. No available slots for {Date} at Agency {AgencyName}.", request.Date, agency.Name);
                return Result.Failure(new[] { "No available slots for the selected date and time." });
            }

            // Create the appointment
            var result = await appointmentService.CreateAppointmentAsync(
                agency.Id,
                request.UserEmail,
                request.AppointmentName,
                request.Date,
                cancellationToken);

            if (!result.Succeeded)
            {
                logger.LogWarning("Failed to create appointment. Errors: {Errors}", string.Join(", ", result.Errors));
                return result;
            }

            logger.LogInformation("Successfully created appointment for Agency {AgencyName} at {Date}.", agency.Name, request.Date);
            return Result.Success;
        }
    }
}
