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

    [McpServerTool, Description(CreateAgencyDescription)]
    public async Task<string> CreateAgencyAsync(string name, string email, int maxAppointmentsPerDay)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return token;

            var response = await agencyBookApi.CreateAgencyAsync(new { name, email, maxAppointmentsPerDay }, GetBearerToken());
            if (response.IsSuccessStatusCode)
            {
                Log.Information("Agency created successfully: {Name}, {Email}", name, email);
                return "Agency created successfully.";
            }
            Log.Warning("Failed to create agency: {Name}, {Email}. Status code: {StatusCode}", name, email, response.StatusCode);
            return $"Failed to create agency. Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            Log.Error("Exception during agency creation: {Name}, {Email}. Error: {Error}", name, email, ex.Message);
            return "An error occurred while creating the agency.";
        }
    }

    [McpServerTool, Description(AssignUserToAgencyDescription)]
    public async Task<string> AssignUserToAgencyAsync(string? agencyEmail, string userEmail, List<string> roles)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return token;

            var response = await agencyBookApi.AssignUserToAgencyAsync(new { agencyEmail, userEmail, roles }, GetBearerToken());
            if (response.IsSuccessStatusCode)
            {
                Log.Information("User assigned to agency successfully: {UserEmail}", userEmail);
                return "User assigned to agency successfully.";
            }
            Log.Warning("Failed to assign user: {UserEmail}. Status code: {StatusCode}", userEmail, response.StatusCode);
            return $"Failed to assign user. Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            Log.Error("Exception during user assignment: {UserEmail}. Error: {Error}", userEmail, ex.Message);
            return "An error occurred while assigning the user.";
        }
    }

    [McpServerTool, Description(GetAppointmentsByDateDescription)]
    public async Task<List<object>> GetAppointmentsByDateAsync(DateTime date)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new List<object>();

            var response = await agencyBookApi.GetAppointmentsByDateAsync(date, GetBearerToken());
            Log.Information("Retrieved appointments for date: {Date}", date);
            return response ?? new List<object>();
        }
        catch (Exception ex)
        {
            Log.Error("Exception during retrieving appointments for date: {Date}. Error: {Error}", date, ex.Message);
            return new List<object>();
        }
    }

    [McpServerTool, Description(HandleNoShowDescription)]
    public async Task<string> HandleNoShowAsync(Guid appointmentId)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return token;

            var response = await agencyBookApi.HandleNoShowAsync(new { appointmentId }, GetBearerToken());
            if (response.IsSuccessStatusCode)
            {
                Log.Information("No-show handled successfully for appointment: {AppointmentId}", appointmentId);
                return "No-show handled successfully.";
            }
            Log.Warning("Failed to handle no-show for appointment: {AppointmentId}. Status code: {StatusCode}", appointmentId, response.StatusCode);
            return $"Failed to handle no-show. Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            Log.Error("Exception while handling no-show for appointment: {AppointmentId}. Error: {Error}", appointmentId, ex.Message);
            return "An error occurred while handling the no-show.";
        }
    }
}
