using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

public class CancelAppointmentCommand : IRequest<Result>
{
    public Guid AppointmentId { get; }

    public CancelAppointmentCommand(Guid appointmentId)
    {
        AppointmentId = appointmentId;
    }
}

public class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, Result>
{
    private readonly IAppointmentService appointmentService;
    private readonly ILogger<CancelAppointmentCommandHandler> logger;

    public CancelAppointmentCommandHandler(
        IAppointmentService appointmentService,
        ILogger<CancelAppointmentCommandHandler> logger)
    {
        this.appointmentService = appointmentService;
        this.logger = logger;
    }

    public async Task<Result> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        if (!await appointmentService.ExistsAsync(request.AppointmentId))
        {
            logger.LogWarning("Cancellation failed. Appointment {AppointmentId} does not exist.", request.AppointmentId);
            return Result.Failure(new[] { "Appointment does not exist." });
        }

        await appointmentService.CancelAppointmentAsync(request.AppointmentId, cancellationToken);

        logger.LogInformation("Appointment {AppointmentId} canceled successfully.", request.AppointmentId);
        return Result.Success;
    }
}
