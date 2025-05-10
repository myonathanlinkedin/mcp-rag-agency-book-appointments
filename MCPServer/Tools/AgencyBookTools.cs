using ModelContextProtocol.Server;
using Serilog;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;

public class AgencyBookTools : BaseTool
{
    private readonly IAgencyBookAPI agencyBookApi;

    public AgencyBookTools(IAgencyBookAPI agencyBookApi, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
    {
        this.agencyBookApi = agencyBookApi ?? throw new ArgumentNullException(nameof(agencyBookApi));
    }

    private const string CreateAgencyDescription = "Create a new agency. Provides basic information including the name, email, and maximum appointments per day.";
    private const string AssignUserToAgencyDescription = "Assign a user to an agency by specifying the user's email and roles.";
    private const string CancelAppointmentDescription = "Cancel an existing appointment using the appointment's ID.";
    private const string CreateAppointmentDescription = "Create an appointment by specifying the user's email, the date of the appointment, and the appointment's name.";
    private const string GetAppointmentsByDateDescription = "Retrieve all appointments for a specific date.";
    private const string HandleNoShowDescription = "Handle a no-show for an appointment by specifying the appointment's ID.";
    private const string RescheduleAppointmentDescription = "Reschedule an existing appointment to a new date and time.";
    private const string UpdateAgencySettingsDescription = "Update agency settings, including max appointments per day and holidays.";

    private string? GetToken()
    {
        var token = GetTokenFromHttpContext();
        return string.IsNullOrWhiteSpace(token) ? LogAndReturnMissingToken() : $"Bearer {token}";
    }

    private string LogAndReturnMissingToken()
    {
        Log.Warning("Authentication token is missing.");
        return "Authentication token is missing or invalid.";
    }

    [McpServerTool, Description(CreateAgencyDescription)]
    public async Task<string> CreateAgencyAsync(
        [Description("Agency name")] string name,
        [Description("Agency email")] string email,
        [Description("Maximum appointments allowed per day")] int maxAppointmentsPerDay)
    {
        var token = GetToken();
        if (token == LogAndReturnMissingToken()) return token;

        var response = await agencyBookApi.CreateAgencyAsync(new { name, email, maxAppointmentsPerDay }, token);
        return response.IsSuccessStatusCode ? "Agency created successfully."
            : $"Failed to create agency. Status code: {response.StatusCode}";
    }

    [McpServerTool, Description(AssignUserToAgencyDescription)]
    public async Task<string> AssignUserToAgencyAsync(
        [Description("Agency email (optional, can be blank or null)")] string? agencyEmail,
        [Description("User's email")] string userEmail,
        [Description("Roles assigned to the user")] List<string> roles)
    {
        var token = GetToken();
        if (token == LogAndReturnMissingToken()) return token;

        var response = await agencyBookApi.AssignUserToAgencyAsync(new { agencyEmail, userEmail, roles }, token);
        return response.IsSuccessStatusCode ? "User assigned to agency successfully."
            : $"Failed to assign user. Status code: {response.StatusCode}";
    }

    [McpServerTool, Description(CreateAppointmentDescription)]
    public async Task<string> CreateAppointmentAsync(
        [Description("Agency email (optional, can be blank or null)")] string? agencyEmail,
        [Description("User's email for appointment")] string userEmail,
        [Description("Appointment date and time")] DateTime date,
        [Description("Name of the appointment")] string appointmentName)
    {
        var token = GetToken();
        if (token == LogAndReturnMissingToken()) return token;

        var response = await agencyBookApi.CreateAppointmentAsync(new { agencyEmail, userEmail, date, appointmentName }, token);
        return response.IsSuccessStatusCode ? "Appointment created successfully."
            : $"Failed to create appointment. Status code: {response.StatusCode}";
    }

    [McpServerTool, Description(GetAppointmentsByDateDescription)]
    public async Task<List<object>> GetAppointmentsByDateAsync(
        [Description("The date to fetch appointments for")] DateTime date)
    {
        var token = GetToken();
        if (token == LogAndReturnMissingToken()) return new List<object>();

        try
        {
            var response = await agencyBookApi.GetAppointmentsByDateAsync(date, token);
            Log.Information("Retrieved appointments for date: {Date}", date);
            return response ?? new List<object>();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while retrieving appointments for date: {Date}", date);
            return new List<object>();
        }
    }

    [McpServerTool, Description(HandleNoShowDescription)]
    public async Task<string> HandleNoShowAsync(
        [Description("Appointment ID to mark as no-show")] Guid appointmentId)
    {
        var token = GetToken();
        if (token == LogAndReturnMissingToken()) return token;

        var response = await agencyBookApi.HandleNoShowAsync(new { appointmentId }, token);
        return response.IsSuccessStatusCode ? "No-show handled successfully."
            : $"Failed to handle no-show. Status code: {response.StatusCode}";
    }

    [McpServerTool, Description(UpdateAgencySettingsDescription)]
    public async Task<string> UpdateAgencySettingsAsync(
        [Description("Agency email (optional, can be blank or null)")] string? agencyEmail,
        [Description("Maximum appointments allowed per day")] int maxAppointmentsPerDay,
        [Description("Holiday date")] DateTime holidayDate,
        [Description("Holiday reason")] string holidayReason)
    {
        var token = GetToken();
        if (token == LogAndReturnMissingToken()) return token;

        var holiday = new { Id = Guid.NewGuid(), Date = holidayDate, Reason = holidayReason };
        var response = await agencyBookApi.UpdateAgencySettingsAsync(new { agencyEmail, maxAppointmentsPerDay, Holidays = new List<object> { holiday } }, token);
        return response.IsSuccessStatusCode ? "Agency settings updated successfully."
            : $"Failed to update agency settings. Status code: {response.StatusCode}";
    }

    [McpServerTool, Description(CancelAppointmentDescription)]
    public async Task<string> CancelAppointmentAsync(
        [Description("Appointment ID to cancel")] Guid appointmentId)
    {
        var token = GetToken();
        if (token == LogAndReturnMissingToken()) return token;

        var response = await agencyBookApi.CancelAppointmentAsync(new { appointmentId }, token);
        return response.IsSuccessStatusCode
            ? "Appointment canceled successfully."
            : $"Failed to cancel appointment. Status code: {response.StatusCode}";
    }

    [McpServerTool, Description(RescheduleAppointmentDescription)]
    public async Task<string> RescheduleAppointmentAsync(
        [Description("Appointment ID to reschedule")] Guid appointmentId,
        [Description("New appointment date and time")] DateTime newDate)
    {
        var token = GetToken();
        if (token == LogAndReturnMissingToken()) return token;

        var response = await agencyBookApi.RescheduleAppointmentAsync(new { appointmentId, newDate }, token);
        return response.IsSuccessStatusCode
            ? "Appointment rescheduled successfully."
            : $"Failed to reschedule appointment. Status code: {response.StatusCode}";
    }
}
