using MediatR;
using Microsoft.Extensions.Logging;

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

        public RescheduleAppointmentCommandHandler(IAppointmentService appointmentService, ILogger<RescheduleAppointmentCommandHandler> logger)
        {
            this.appointmentService = appointmentService;
            this.logger = logger;
        }

        public async Task<Result> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
        {
            if (!await appointmentService.ExistsAsync(request.AppointmentId))
            {
                logger.LogWarning("Reschedule failed. Appointment {AppointmentId} does not exist.", request.AppointmentId);
                return Result.Failure(new[] { "Appointment does not exist." });
            }

            await appointmentService.RescheduleAppointmentAsync(request.AppointmentId, request.NewDate, cancellationToken);

            logger.LogInformation("Appointment {AppointmentId} successfully rescheduled to {NewDate}.", request.AppointmentId, request.NewDate);
            return Result.Success;
        }
    }
}
