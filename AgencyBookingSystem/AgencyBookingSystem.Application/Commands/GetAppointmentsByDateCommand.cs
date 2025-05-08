using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class GetAppointmentsByDateCommand : IRequest<Result<List<AppointmentDto>>>
{
    public DateTime Date { get; }

    public GetAppointmentsByDateCommand(DateTime date) => Date = date;

    public class GetAppointmentsByDateCommandHandler : IRequestHandler<GetAppointmentsByDateCommand, Result<List<AppointmentDto>>>
    {
        private readonly IAppointmentService appointmentService;
        private readonly ILogger<GetAppointmentsByDateCommandHandler> logger;
        private readonly IHttpContextAccessor httpContextAccessor;

        public GetAppointmentsByDateCommandHandler(
            IAppointmentService appointmentService,
            ILogger<GetAppointmentsByDateCommandHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            this.appointmentService = appointmentService;
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result<List<AppointmentDto>>> Handle(GetAppointmentsByDateCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Fetching appointments for date {Date}.", request.Date);

            var user = httpContextAccessor.HttpContext?.User;
            var userEmail = user?.FindFirst(ClaimTypes.Email)?.Value;
            var isAdmin = user?.IsInRole(CommonModelConstants.Role.Administrator) ?? false;

            if (string.IsNullOrEmpty(userEmail))
            {
                logger.LogWarning("Failed to fetch appointments. No valid user email found.");
                return Result<List<AppointmentDto>>.Failure(new[] { "User email is missing." });
            }

            List<AppointmentDto> appointmentDtos = isAdmin
                ? await appointmentService.GetAppointmentsByDateAsync(request.Date)
                : await appointmentService.GetAppointmentsByDateForUserAsync(request.Date, userEmail);

            if (appointmentDtos is null || !appointmentDtos.Any())
            {
                logger.LogWarning("No appointments found for date {Date}.", request.Date);
                return Result<List<AppointmentDto>>.Failure(new[] { "No appointments found.", "Try a different date." });
            }

            logger.LogInformation("Successfully fetched {AppointmentCount} appointments for date {AppointmentDate}.", appointmentDtos.Count, request.Date);
            return Result<List<AppointmentDto>>.SuccessWith(appointmentDtos);
        }
    }
}
