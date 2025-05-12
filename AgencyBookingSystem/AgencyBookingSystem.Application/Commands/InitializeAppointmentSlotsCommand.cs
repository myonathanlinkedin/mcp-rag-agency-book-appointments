using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class InitializeAppointmentSlotsCommand : IRequest<Result>
{
    public string? AgencyEmail { get; } // Optional: If provided, admin must execute
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public TimeSpan SlotDuration { get; }
    public int SlotsPerDay { get; }
    public int CapacityPerSlot { get; }

    public InitializeAppointmentSlotsCommand(
        string? agencyEmail,
        DateTime startDate,
        DateTime endDate,
        TimeSpan slotDuration,
        int slotsPerDay,
        int capacityPerSlot)
    {
        AgencyEmail = agencyEmail;
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
        private readonly IHttpContextAccessor httpContextAccessor;

        public InitializeAppointmentSlotsCommandHandler(
            IAgencyService agencyService,
            ILogger<InitializeAppointmentSlotsCommandHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            this.agencyService = agencyService;
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result> Handle(InitializeAppointmentSlotsCommand request, CancellationToken cancellationToken)
        {
            // Get user context
            var user = httpContextAccessor.HttpContext?.User;
            var userEmail = user?.FindFirst(ClaimTypes.Name)?.Value;
            var isAdmin = user?.IsInRole(CommonModelConstants.Role.Administrator) ?? false;
            var isAgency = user?.IsInRole(CommonModelConstants.Role.Agency) ?? false;

            if (string.IsNullOrEmpty(userEmail))
            {
                logger.LogWarning("Slot initialization failed. No valid user email found in HttpContext.");
                return Result.Failure(new[] { "No valid user email found in HttpContext." });
            }

            // Determine which agency email to use
            string agencyEmail = request.AgencyEmail ?? userEmail;

            // Validate authorization
            if (request.AgencyEmail != null && !isAdmin)
            {
                logger.LogWarning("Unauthorized initialization attempt. Only administrators can initialize slots for another agency.");
                return Result.Failure(new[] { "Only administrators can initialize slots for another agency." });
            }

            if (request.AgencyEmail == null && !isAgency)
            {
                logger.LogWarning("Unauthorized initialization attempt. User must have Agency role.");
                return Result.Failure(new[] { "User must have Agency role to initialize slots." });
            }

            // Get agency
            var agency = await agencyService.GetByEmailAsync(agencyEmail);
            if (agency == null)
            {
                logger.LogWarning("Failed to initialize slots. No agency found for email {AgencyEmail}.", agencyEmail);
                return Result.Failure(new[] { "Agency not found." });
            }

            logger.LogInformation(
                "Initializing appointment slots for Agency {AgencyEmail} from {StartDate} to {EndDate}",
                agencyEmail,
                request.StartDate.Date,
                request.EndDate.Date);

            return await agencyService.InitializeAppointmentSlotsAsync(
                agency.Id,
                request.StartDate,
                request.EndDate,
                request.SlotDuration,
                request.SlotsPerDay,
                request.CapacityPerSlot,
                cancellationToken);
        }
    }
} 