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
    Task<List<object>> GetAppointmentsByDateAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/AgencyBook/HandleNoShowAsync")]
    Task<HttpResponseMessage> HandleNoShowAsync([Body] object payload, [Header("Authorization")] string token);

    [Post("/api/AgencyBook/RescheduleAppointmentAsync")]
    Task<HttpResponseMessage> RescheduleAppointmentAsync([Body] object payload, [Header("Authorization")] string token);
}
