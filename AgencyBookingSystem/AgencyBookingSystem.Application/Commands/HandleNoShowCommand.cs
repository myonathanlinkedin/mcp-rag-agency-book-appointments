using MediatR;
using Microsoft.Extensions.Logging;

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

        public HandleNoShowCommandHandler(IAppointmentService appointmentService, ILogger<HandleNoShowCommandHandler> logger)
        {
            this.appointmentService = appointmentService;
            this.logger = logger;
        }

        public async Task<Result> Handle(HandleNoShowCommand request, CancellationToken cancellationToken)
        {
            if (!await appointmentService.ExistsAsync(request.AppointmentId))
            {
                logger.LogWarning("No-show processing failed. Appointment {AppointmentId} does not exist.", request.AppointmentId);
                return Result.Failure(new[] { "Appointment does not exist." });
            }

            await appointmentService.HandleNoShowAsync(request.AppointmentId);

            logger.LogInformation("Appointment {AppointmentId} marked as expired due to no-show.", request.AppointmentId);
            return Result.Success;
        }
    }
}
