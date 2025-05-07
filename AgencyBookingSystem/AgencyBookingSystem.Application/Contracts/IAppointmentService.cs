public interface IAppointmentService : IBaseService<Appointment>
{
    // Retrieve appointments for a specific agency
    Task<List<Appointment>> GetAppointmentsByAgencyAsync(Guid agencyId);

    // Fetch upcoming appointments starting from a specific date
    Task<List<Appointment>> GetUpcomingAppointmentsAsync(Guid agencyId, DateTime fromDate);

    // Create an appointment while checking availability and handling notifications
    Task<Result> CreateAppointmentAsync(Guid agencyId, string email, string appointmentName, DateTime date, CancellationToken cancellationToken = default);

    // Allow admins to forcefully create appointments regardless of availability constraints
    Task<Result> ForceCreateAppointmentAsync(string email, string appointmentName, DateTime date, CancellationToken cancellationToken = default);

    // Check if a given appointment exists in the system
    Task<bool> ExistsAsync(Guid appointmentId);

    // Cancel an appointment with proper notification handling
    Task CancelAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    // Suggest the next available appointment date when slots are full
    Task<DateTime?> GetNextAvailableDateAsync(Guid agencyId, DateTime preferredDate);

    // Handle no-show cases, marking appointments as expired and notifying relevant users
    Task HandleNoShowAsync(Guid appointmentId);

    // Validate whether the agency allows public bookings or if appointments are restricted to staff
    Task<bool> IsBookingAllowedAsync(Guid agencyId);

    // Allow users to reschedule appointments while ensuring new availability
    Task<Result> RescheduleAppointmentAsync(Guid appointmentId, DateTime newDate, CancellationToken cancellationToken = default);

    // Fetch all appointments for a specific user
    Task<List<AppointmentDto>> GetAppointmentsByDateAsync(DateTime date);
}
