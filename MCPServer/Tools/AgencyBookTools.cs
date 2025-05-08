using ModelContextProtocol.Server;
using Serilog;
using System.ComponentModel;

public class AgencyBookTools
{
    private readonly IAgencyBookAPI agencyBookApi;

    public AgencyBookTools(IAgencyBookAPI agencyBookApi)
    {
        this.agencyBookApi = agencyBookApi;
    }

    // Descriptions as constants for better management
    private const string CreateAgencyDescription = "Create a new agency. Provides basic information including the name, email, and maximum appointments per day.";
    private const string AssignUserToAgencyDescription = "Assign a user to an agency by specifying the user's email and roles.";
    private const string CancelAppointmentDescription = "Cancel an existing appointment using the appointment's ID.";
    private const string CreateAppointmentDescription = "Create an appointment by specifying the user's email, the date of the appointment, and the appointment's name.";
    private const string GetAppointmentsByDateDescription = "Retrieve all appointments for a specific date.";
    private const string HandleNoShowDescription = "Handle a no-show for an appointment by specifying the appointment's ID.";
    private const string RescheduleAppointmentDescription = "Reschedule an existing appointment to a new date and time.";
    private const string UpdateAgencySettingsDescription = "Update agency settings, including max appointments per day and holidays.";

    // Create Agency
    [McpServerTool, Description(CreateAgencyDescription)]
    public async Task<string> CreateAgencyAsync(
        [Description("Authorization token")] string token,
        [Description("Agency name")] string name,
        [Description("Agency email")] string email,
        [Description("Maximum appointments allowed per day")] int maxAppointmentsPerDay)
    {
        var payload = new { name, email, maxAppointmentsPerDay };

        try
        {
            var response = await agencyBookApi.CreateAgencyAsync(payload, $"Bearer {token}");
            return response.IsSuccessStatusCode ? "Agency created successfully."
                : $"Failed to create agency. Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while creating agency: {AgencyName}", name);
            return "An error occurred during agency creation.";
        }
    }

    // Assign User to Agency
    [McpServerTool, Description(AssignUserToAgencyDescription)]
    public async Task<string> AssignUserToAgencyAsync(
        [Description("Authorization token")] string token,
        [Description("Agency email (optional, can be blank or null)")] string? agencyEmail,
        [Description("User's email")] string userEmail,
        [Description("Roles assigned to the user")] List<string> roles)
    {
        var payload = new { AgencyEmail = agencyEmail, UserEmail = userEmail, Roles = roles };

        try
        {
            var response = await agencyBookApi.AssignUserToAgencyAsync(payload, $"Bearer {token}");
            return response.IsSuccessStatusCode ? "User assigned to agency successfully."
                : $"Failed to assign user. Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while assigning user: {UserEmail} to agency: {AgencyEmail}", userEmail, agencyEmail ?? "current agency");
            return "An error occurred during user assignment.";
        }
    }

    // Create Appointment
    [McpServerTool, Description(CreateAppointmentDescription)]
    public async Task<string> CreateAppointmentAsync(
        [Description("Authorization token")] string token,
        [Description("Agency email (optional, can be blank or null)")] string? agencyEmail,
        [Description("User's email for appointment")] string userEmail,
        [Description("Appointment date and time")] DateTime date,
        [Description("Name of the appointment")] string appointmentName)
    {
        var payload = new { AgencyEmail = agencyEmail, UserEmail = userEmail, Date = date, AppointmentName = appointmentName };

        try
        {
            var response = await agencyBookApi.CreateAppointmentAsync(payload, $"Bearer {token}");
            return response.IsSuccessStatusCode ? "Appointment created successfully."
                : $"Failed to create appointment. Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while creating appointment for agency: {AgencyEmail}, user: {UserEmail} on {Date}",
                agencyEmail ?? "current agency", userEmail, date);
            return "An error occurred during appointment creation.";
        }
    }

    // Get Appointments by Date
    [McpServerTool, Description(GetAppointmentsByDateDescription)]
    public async Task<List<object>> GetAppointmentsByDateAsync(
        [Description("Authorization token")] string token,
        [Description("The date to fetch appointments for")] DateTime date)
    {
        try
        {
            var response = await agencyBookApi.GetAppointmentsByDateAsync(date, $"Bearer {token}");
            Log.Information("Retrieved appointments for date: {Date}", date);
            return response ?? new List<object>();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while retrieving appointments for date: {Date}", date);
            return new List<object>();
        }
    }

    // Handle No-Show
    [McpServerTool, Description(HandleNoShowDescription)]
    public async Task<string> HandleNoShowAsync(
        [Description("Authorization token")] string token,
        [Description("Appointment ID to mark as no-show")] Guid appointmentId)
    {
        var payload = new { appointmentId };

        try
        {
            var response = await agencyBookApi.HandleNoShowAsync(payload, $"Bearer {token}");
            return response.IsSuccessStatusCode ? "No-show handled successfully."
                : $"Failed to handle no-show. Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while handling no-show for appointment ID: {AppointmentId}", appointmentId);
            return "An error occurred while handling no-show.";
        }
    }

    // Update Agency Settings
    [McpServerTool, Description(UpdateAgencySettingsDescription)]
    public async Task<string> UpdateAgencySettingsAsync(
        [Description("Authorization token")] string token,
        [Description("Agency email (optional, can be blank or null)")] string? agencyEmail,
        [Description("Maximum appointments allowed per day")] int maxAppointmentsPerDay,
        [Description("Holiday date")] DateTime holidayDate,
        [Description("Holiday reason")] string holidayReason)
    {
        var holiday = new { Id = Guid.NewGuid(), Date = holidayDate, Reason = holidayReason };
        var payload = new { AgencyEmail = agencyEmail, MaxAppointmentsPerDay = maxAppointmentsPerDay, Holidays = new List<object> { holiday } };

        try
        {
            var response = await agencyBookApi.UpdateAgencySettingsAsync(payload, $"Bearer {token}");
            return response.IsSuccessStatusCode ? "Agency settings updated successfully."
                : $"Failed to update agency settings. Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while updating agency settings for {AgencyEmail}", agencyEmail ?? "current agency");
            return "An error occurred while updating agency settings.";
        }
    }

    // Cancel Appointment
    [McpServerTool, Description(CancelAppointmentDescription)]
    public async Task<string> CancelAppointmentAsync(
        [Description("Authorization token")] string token,
        [Description("Appointment ID to cancel")] Guid appointmentId)
    {
        var payload = new { appointmentId };

        try
        {
            var response = await agencyBookApi.CancelAppointmentAsync(payload, $"Bearer {token}");
            return response.IsSuccessStatusCode
                ? "Appointment canceled successfully."
                : $"Failed to cancel appointment. Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while canceling appointment ID: {AppointmentId}", appointmentId);
            return "An error occurred during appointment cancellation.";
        }
    }

    // Reschedule Appointment
    [McpServerTool, Description(RescheduleAppointmentDescription)]
    public async Task<string> RescheduleAppointmentAsync(
        [Description("Authorization token")] string token,
        [Description("Appointment ID to reschedule")] Guid appointmentId,
        [Description("New appointment date and time")] DateTime newDate)
    {
        var payload = new { appointmentId, newDate };

        try
        {
            var response = await agencyBookApi.RescheduleAppointmentAsync(payload, $"Bearer {token}");
            return response.IsSuccessStatusCode
                ? "Appointment rescheduled successfully."
                : $"Failed to reschedule appointment. Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while rescheduling appointment ID: {AppointmentId} to {NewDate}", appointmentId, newDate);
            return "An error occurred during appointment rescheduling.";
        }
    }
}
