public class AppointmentEvent : IDomainEvent
{
    public Guid AppointmentId { get; }
    public string AppointmentName { get; }
    public DateTime AppointmentDate { get; }
    public string Status { get; }
    public string AgencyName { get; }
    public string AgencyEmail { get; }
    public string UserEmail { get; }

    public Guid AggregateId => AppointmentId;

    public AppointmentEvent(Guid appointmentId, string appointmentName, DateTime appointmentDate, string status, string agencyName, string agencyEmail, string userEmail)
    {
        AppointmentId = appointmentId == Guid.Empty
            ? throw new ArgumentNullException(nameof(appointmentId))
            : appointmentId;

        AppointmentName = string.IsNullOrWhiteSpace(appointmentName)
            ? throw new ArgumentNullException(nameof(appointmentName))
            : appointmentName;

        AppointmentDate = appointmentDate <= DateTime.MinValue
            ? throw new ArgumentException("Invalid appointment date", nameof(appointmentDate))
            : appointmentDate;

        Status = string.IsNullOrWhiteSpace(status)
            ? throw new ArgumentNullException(nameof(status))
            : status;

        AgencyName = string.IsNullOrWhiteSpace(agencyName)
            ? throw new ArgumentNullException(nameof(agencyName))
            : agencyName;

        AgencyEmail = string.IsNullOrWhiteSpace(agencyEmail)
            ? throw new ArgumentNullException(nameof(agencyEmail))
            : agencyEmail;

        UserEmail = string.IsNullOrWhiteSpace(userEmail)
            ? throw new ArgumentNullException(nameof(userEmail))
            : userEmail;
    }
}
