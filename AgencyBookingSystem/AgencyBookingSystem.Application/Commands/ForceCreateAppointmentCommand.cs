using MediatR;
using Microsoft.Extensions.Logging;

public class ForceCreateAppointmentCommand : IRequest<Result>
{
    public string Email { get; }
    public DateTime Date { get; }
    public string AppointmentName { get; } // Added appointment name

    public ForceCreateAppointmentCommand(string email, DateTime date, string appointmentName)
    {
        Email = email;
        Date = date;
        AppointmentName = appointmentName;
    }
}

public class ForceCreateAppointmentCommandHandler : IRequestHandler<ForceCreateAppointmentCommand, Result>
{
    private readonly IAppointmentService appointmentService;
    private readonly IAgencyService agencyService;
    private readonly ILogger<ForceCreateAppointmentCommandHandler> logger;

    public ForceCreateAppointmentCommandHandler(IAppointmentService appointmentService, IAgencyService agencyService, ILogger<ForceCreateAppointmentCommandHandler> logger)
    {
        this.appointmentService = appointmentService;
        this.agencyService = agencyService;
        this.logger = logger;
    }

    public async Task<Result> Handle(ForceCreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var agency = await agencyService.GetByEmailAsync(request.Email);
        if (agency == null)
        {
            logger.LogWarning("Admin override failed. No agency found for email {Email}.", request.Email);
            return Result.Failure(new[] { "No agency found for this email." });
        }

        var result = await appointmentService.ForceCreateAppointmentAsync(request.Email, request.AppointmentName, request.Date, cancellationToken);

        if (!result.Succeeded)
        {
            logger.LogWarning("Admin override failed for appointment '{AppointmentName}' on {Date}.", request.AppointmentName, request.Date);
            return Result.Failure(new[] { "Appointment override failed." });
        }

        logger.LogInformation("Admin override: Appointment '{AppointmentName}' created successfully for Agency {AgencyId}, User {Email}.",
            request.AppointmentName, agency.Id, request.Email);

        return Result.Success;
    }
}
