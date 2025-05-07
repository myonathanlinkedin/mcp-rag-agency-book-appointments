using MediatR;
using Microsoft.Extensions.Logging;

public class CreateAppointmentCommand : IRequest<Result>
{
    public string AgencyEmail { get; }
    public string UserEmail { get; }
    public DateTime Date { get; }
    public string AppointmentName { get; } // Added appointment name

    public CreateAppointmentCommand(string agencyEmail, string userEmail, DateTime date, string appointmentName)
    {
        AgencyEmail = agencyEmail;
        UserEmail = userEmail;
        Date = date;
        AppointmentName = appointmentName;
    }
}

public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, Result>
{
    private readonly IAppointmentService appointmentService;
    private readonly IAgencyService agencyService;
    private readonly ILogger<CreateAppointmentCommandHandler> logger;

    public CreateAppointmentCommandHandler(IAppointmentService appointmentService, IAgencyService agencyService, ILogger<CreateAppointmentCommandHandler> logger)
    {
        this.appointmentService = appointmentService;
        this.agencyService = agencyService;
        this.logger = logger;
    }

    public async Task<Result> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var agency = await agencyService.GetByEmailAsync(request.AgencyEmail);
        if (agency == null)
        {
            logger.LogWarning("Failed to create appointment. No agency found for email {AgencyEmail}.", request.AgencyEmail);
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
                request.AppointmentName, request.AgencyEmail, request.UserEmail, string.Join(", ", result.Errors));
            return Result.Failure(result.Errors);
        }

        logger.LogInformation("Appointment '{AppointmentName}' created successfully for Agency {AgencyEmail}, User {UserEmail}.",
            request.AppointmentName, request.AgencyEmail, request.UserEmail);
        return Result.Success;
    }
}
