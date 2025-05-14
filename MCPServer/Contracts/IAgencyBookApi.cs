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
}
