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
    private const string CreateAppointmentDescription = "Create an appointment by specifying the user's email, the date of the appointment, and the appointment's name.";
    private const string GetAppointmentsByDateDescription = "Retrieve all appointments for a specific date.";
    private const string HandleNoShowDescription = "Handle a no-show for an appointment by specifying the appointment's ID.";
    private const string UpdateAgencySettingsDescription = "Update agency settings, including max appointments per day and holidays.";

    // Create Agency
    [McpServerTool, Description(CreateAgencyDescription)]
    public async Task<string> CreateAgencyAsync(
        [Description("Agency name")] string name,
        [Description("Agency email")] string email,
        [Description("Maximum appointments allowed per day")] int maxAppointmentsPerDay)
    {
        var payload = new
        {
            name,
            email,
            maxAppointmentsPerDay
        };

        try
        {
            var response = await agencyBookApi.CreateAgencyAsync(payload, "Bearer YourTokenHere");

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Successfully created agency: {AgencyName}", name);
                return "Agency created successfully.";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Log.Error("Failed to create agency: {AgencyName}, StatusCode: {StatusCode}, Error: {Error}",
                    name, response.StatusCode, errorContent);
                return $"Failed to create agency. Status code: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An exception occurred while creating agency: {AgencyName}", name);
            return "An error occurred during agency creation.";
        }
    }

    // Assign User to Agency
    [McpServerTool, Description(AssignUserToAgencyDescription)]
    public async Task<string> AssignUserToAgencyAsync(
        [Description("User's email")] string userEmail,
        [Description("Roles assigned to the user")] List<string> roles)
    {
        var payload = new
        {
            userEmail,
            roles
        };

        try
        {
            var response = await agencyBookApi.AssignUserToAgencyAsync(payload, "Bearer YourTokenHere");

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Successfully assigned user: {UserEmail} to agency.", userEmail);
                return "User assigned to agency successfully.";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Log.Error("Failed to assign user: {UserEmail} to agency, StatusCode: {StatusCode}, Error: {Error}",
                    userEmail, response.StatusCode, errorContent);
                return $"Failed to assign user. Status code: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An exception occurred while assigning user: {UserEmail} to agency.", userEmail);
            return "An error occurred during user assignment.";
        }
    }

    // Create Appointment
    [McpServerTool, Description(CreateAppointmentDescription)]
    public async Task<string> CreateAppointmentAsync(
        [Description("User's email for appointment")] string userEmail,
        [Description("Appointment date and time")] DateTime date,
        [Description("Name of the appointment")] string appointmentName)
    {
        var payload = new
        {
            userEmail,
            date,
            appointmentName
        };

        try
        {
            var response = await agencyBookApi.CreateAppointmentAsync(payload, "Bearer YourTokenHere");

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Successfully created appointment for user: {UserEmail} on {Date}", userEmail, date);
                return "Appointment created successfully.";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Log.Error("Failed to create appointment for user: {UserEmail} on {Date}, StatusCode: {StatusCode}, Error: {Error}",
                    userEmail, date, response.StatusCode, errorContent);
                return $"Failed to create appointment. Status code: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An exception occurred while creating appointment for user: {UserEmail} on {Date}", userEmail, date);
            return "An error occurred during appointment creation.";
        }
    }

    // Get Appointments by Date
    [McpServerTool, Description(GetAppointmentsByDateDescription)]
    public async Task<List<object>> GetAppointmentsByDateAsync(
        [Description("The date to fetch appointments for")] DateTime date)
    {
        var payload = new
        {
            date
        };

        try
        {
            var response = await agencyBookApi.GetAppointmentsByDateAsync(payload, "Bearer YourTokenHere");

            if (response != null && response.Count > 0)
            {
                Log.Information("Retrieved {Count} appointments for date: {Date}", response.Count, date);
                return response;
            }
            else
            {
                Log.Warning("No appointments found for date: {Date}", date);
                return new List<object>();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An exception occurred while retrieving appointments for date: {Date}", date);
            return new List<object>();
        }
    }

    // Handle No-Show
    [McpServerTool, Description(HandleNoShowDescription)]
    public async Task<string> HandleNoShowAsync(
        [Description("Appointment ID to mark as no-show")] Guid appointmentId)
    {
        var payload = new
        {
            appointmentId
        };

        try
        {
            var response = await agencyBookApi.HandleNoShowAsync(payload, "Bearer YourTokenHere");

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Successfully handled no-show for appointment ID: {AppointmentId}", appointmentId);
                return "No-show handled successfully.";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Log.Error("Failed to handle no-show for appointment ID: {AppointmentId}, StatusCode: {StatusCode}, Error: {Error}",
                    appointmentId, response.StatusCode, errorContent);
                return $"Failed to handle no-show. Status code: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An exception occurred while handling no-show for appointment ID: {AppointmentId}", appointmentId);
            return "An error occurred while handling no-show.";
        }
    }

    // Update Agency Settings
    [McpServerTool, Description(UpdateAgencySettingsDescription)]
    public async Task<string> UpdateAgencySettingsAsync(
        [Description("Appointment ID to update settings for")] Guid appointmentId,
        [Description("Maximum appointments allowed per day")] int maxAppointmentsPerDay,
        [Description("Holiday date")] DateTime holidayDate,
        [Description("Holiday reason")] string holidayReason)
    {
        // Constructing the holiday object anonymously
        var holiday = new
        {
            Id = Guid.NewGuid(),  // Generate a new ID for the holiday
            AgencyId = appointmentId,  // Assuming that AgencyId is linked to the appointmentId
            Date = holidayDate,
            Reason = holidayReason
        };

        var payload = new
        {
            appointmentId,
            maxAppointmentsPerDay,
            holidays = new List<object> { holiday }
        };

        try
        {
            var response = await agencyBookApi.UpdateAgencySettingsAsync(payload, "Bearer YourTokenHere");

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Successfully updated agency settings for appointment ID: {AppointmentId}", appointmentId);
                return "Agency settings updated successfully.";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Log.Error("Failed to update agency settings for appointment ID: {AppointmentId}, StatusCode: {StatusCode}, Error: {Error}",
                    appointmentId, response.StatusCode, errorContent);
                return $"Failed to update agency settings. Status code: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An exception occurred while updating agency settings for appointment ID: {AppointmentId}", appointmentId);
            return "An error occurred while updating agency settings.";
        }
    }
}
