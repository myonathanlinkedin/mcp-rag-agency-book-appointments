using MediatR;
using Microsoft.Extensions.Logging;

public class InitializeAppointmentSlotsCommand : IRequest<Result>
{
    public Guid AgencyId { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public TimeSpan SlotDuration { get; }
    public int SlotsPerDay { get; }
    public int CapacityPerSlot { get; }

    public InitializeAppointmentSlotsCommand(
        Guid agencyId,
        DateTime startDate,
        DateTime endDate,
        TimeSpan slotDuration,
        int slotsPerDay,
        int capacityPerSlot)
    {
        AgencyId = agencyId;
        StartDate = startDate;
        EndDate = endDate;
        SlotDuration = slotDuration;
        SlotsPerDay = slotsPerDay;
        CapacityPerSlot = capacityPerSlot;
    }

    public class InitializeAppointmentSlotsCommandHandler : IRequestHandler<InitializeAppointmentSlotsCommand, Result>
    {
        private readonly IAgencyService agencyService;
        private readonly ILogger<InitializeAppointmentSlotsCommandHandler> logger;

        public InitializeAppointmentSlotsCommandHandler(
            IAgencyService agencyService,
            ILogger<InitializeAppointmentSlotsCommandHandler> logger)
        {
            this.agencyService = agencyService;
            this.logger = logger;
        }

        public async Task<Result> Handle(InitializeAppointmentSlotsCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "Initializing appointment slots for Agency {AgencyId} from {StartDate} to {EndDate}",
                request.AgencyId,
                request.StartDate.Date,
                request.EndDate.Date);

            return await agencyService.InitializeAppointmentSlotsAsync(
                request.AgencyId,
                request.StartDate,
                request.EndDate,
                request.SlotDuration,
                request.SlotsPerDay,
                request.CapacityPerSlot,
                cancellationToken);
        }
    }
} 