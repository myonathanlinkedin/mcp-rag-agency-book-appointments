using Refit;

public interface IAgencyBookAPI
{
    [Post("/api/AgencyBook/CreateAgencyAsync")]
    Task<HttpResponseMessage> CreateAgencyAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/AgencyBook/AssignUserToAgencyAsync")]
    Task<HttpResponseMessage> AssignUserToAgencyAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/AgencyBook/CancelAppointmentAsync")]
    Task<HttpResponseMessage> CancelAppointmentAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/AgencyBook/CreateAppointmentAsync")]
    Task<HttpResponseMessage> CreateAppointmentAsync([Body] object payload, [Header("Authorization")] string token);

    [Put("/api/AgencyBook/UpdateAgencySettingsAsync")]
    Task<HttpResponseMessage> UpdateAgencySettingsAsync([Body] object payload, [Header("Authorization")] string token);

    [Get("/api/AgencyBook/GetAppointmentsByDateAsync")]
    Task<List<object>> GetAppointmentsByDateAsync([Query] DateTime date, [Header("Authorization")] string token);

    [Post("/api/AgencyBook/HandleNoShowAsync")]
    Task<HttpResponseMessage> HandleNoShowAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/AgencyBook/RescheduleAppointmentAsync")]
    Task<HttpResponseMessage> RescheduleAppointmentAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/AgencyBook/InitializeAppointmentSlotsAsync")]
    Task<HttpResponseMessage> InitializeAppointmentSlotsAsync([Body] object payload, [Header("Authorization")] string token);

    [Get("/api/AgencyBook/GetAgencyByEmailAsync")]
    Task<Agency> GetAgencyByEmailAsync([Query] string email, [Header("Authorization")] string token);

    [Get("/api/AgencyBook/GetApprovedAgenciesAsync")]
    Task<List<Agency>> GetApprovedAgenciesAsync([Header("Authorization")] string token);

    [Get("/api/AgencyBook/GetAvailableSlotsAsync")]
    Task<List<AppointmentSlot>> GetAvailableSlotsAsync([Query] Guid agencyId, [Query] DateTime date, [Header("Authorization")] string token);

    [Get("/api/AgencyBook/GetUpcomingAppointmentsAsync")]
    Task<List<Appointment>> GetUpcomingAppointmentsAsync([Query] Guid agencyId, [Query] DateTime fromDate, [Header("Authorization")] string token);

    [Get("/api/AgencyBook/GetNextAvailableDateAsync")]
    Task<DateTime?> GetNextAvailableDateAsync([Query] Guid agencyId, [Query] DateTime preferredDate, [Header("Authorization")] string token);

    [Get("/api/AgencyBook/IsBookingAllowedAsync")]
    Task<bool> IsBookingAllowedAsync([Query] Guid agencyId, [Header("Authorization")] string token);

    [Get("/api/AgencyBook/HasAvailableSlotAsync")]
    Task<bool> HasAvailableSlotAsync([Query] Guid agencyId, [Query] DateTime date, [Header("Authorization")] string token);
}
