
public class AppointmentEntityEvent : IDomainEvent
{
    private Guid appointmentId;

    public AppointmentEntityEvent(Guid appointmentId)
    {
        this.appointmentId = appointmentId == Guid.Empty
            ? throw new ArgumentNullException(nameof(appointmentId))
            : appointmentId;
    }

    public Guid AggregateId => appointmentId;
}
