using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

public class GetAppointmentsByDateCommand : IRequest<List<AppointmentDto>>
{
    public DateTime Date { get; }

    public GetAppointmentsByDateCommand(DateTime date)
    {
        Date = date;
    }

    public class GetAppointmentsByDateCommandHandler : IRequestHandler<GetAppointmentsByDateCommand, List<AppointmentDto>>
    {
        private readonly IAppointmentService appointmentService;
        private readonly ILogger<GetAppointmentsByDateCommandHandler> logger;

        public GetAppointmentsByDateCommandHandler(IAppointmentService appointmentService, ILogger<GetAppointmentsByDateCommandHandler> logger)
        {
            this.appointmentService = appointmentService;
            this.logger = logger;
        }

        public async Task<List<AppointmentDto>> Handle(GetAppointmentsByDateCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Fetching appointments for date {Date}.", request.Date);

            var appointmentDtos = await appointmentService.GetAppointmentsByDateAsync(request.Date);
            if (appointmentDtos == null || !appointmentDtos.Any())
            {
                logger.LogWarning("No appointments found for date {Date}.", request.Date);
                return new List<AppointmentDto>();
            }

            logger.LogInformation("Successfully fetched {AppointmentCount} appointments for date {AppointmentDate}.", appointmentDtos.Count, request.Date);
            return appointmentDtos;
        }
    }
}