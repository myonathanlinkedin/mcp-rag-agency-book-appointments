using Refit;

public interface IAgencyBookAPI
{
    [Post("/api/AgencyBook/CreateAgency/CreateAgencyAsync")]
    Task<HttpResponseMessage> CreateAgencyAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/AgencyBook/AssignUserToAgency/AssignUserToAgencyAsync")]
    Task<HttpResponseMessage> AssignUserToAgencyAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/AgencyBook/CancelAppointment/CancelAppointmentAsync")]
    Task<HttpResponseMessage> CancelAppointmentAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/AgencyBook/CreateAppointment/CreateAppointmentAsync")]
    Task<HttpResponseMessage> CreateAppointmentAsync([Body] object payload, [Header("Authorization")] string token);

    [Put("/api/AgencyBook/UpdateAgencySettings/UpdateAgencySettingsAsync")]
    Task<HttpResponseMessage> UpdateAgencySettingsAsync([Body] object payload, [Header("Authorization")] string token);

    [Get("/api/AgencyBook/GetAppointmentsByDate/GetAppointmentsByDateAsync")]
    Task<List<object>> GetAppointmentsByDateAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/AgencyBook/HandleNoShow/HandleNoShowAsync")]
    Task<HttpResponseMessage> HandleNoShowAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/AgencyBook/RescheduleAppointment/RescheduleAppointmentAsync")]
    Task<HttpResponseMessage> RescheduleAppointmentAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/AgencyBook/InitializeAppointmentSlots/InitializeAppointmentSlotsAsync")]
    Task<HttpResponseMessage> InitializeAppointmentSlotsAsync([Body] object payload, [Header("Authorization")] string token);

    [Get("/api/AgencyBook/GetAgencyByEmail/GetAgencyByEmailAsync")]
    Task<object> GetAgencyByEmailAsync([Query] string email, [Header("Authorization")] string token);

    [Get("/api/AgencyBook/GetApprovedAgencies/GetApprovedAgenciesAsync")]
    Task<List<object>> GetApprovedAgenciesAsync([Header("Authorization")] string token);

    [Get("/api/AgencyBookGetAvailableSlots/GetAvailableSlotsAsync")]
    Task<List<object>> GetAvailableSlotsAsync([Query] Guid agencyId, [Query] DateTime date, [Header("Authorization")] string token);

    [Get("/api/AgencyBookGetUpcomingAppointments/GetUpcomingAppointmentsAsync")]
    Task<List<object>> GetUpcomingAppointmentsAsync([Query] Guid agencyId, [Query] DateTime fromDate, [Header("Authorization")] string token);

    [Get("/api/AgencyBookGetNextAvailableDate/GetNextAvailableDateAsync")]
    Task<DateTime?> GetNextAvailableDateAsync([Query] Guid agencyId, [Query] DateTime preferredDate, [Header("Authorization")] string token);

    [Get("/api/AgencyBook/IsBookingAllowed/IsBookingAllowedAsync")]
    Task<bool> IsBookingAllowedAsync([Query] Guid agencyId, [Header("Authorization")] string token);

    [Get("/api/AgencyBook/HasAvailableSlot/HasAvailableSlotAsync")]
    Task<bool> HasAvailableSlotAsync([Query] Guid agencyId, [Query] DateTime date, [Header("Authorization")] string token);
}
