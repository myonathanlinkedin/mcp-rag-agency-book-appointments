using ModelContextProtocol.Server;
using Serilog;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

public sealed class AgencyBookTools : BaseTool
{
    private readonly IAgencyBookAPI agencyBookApi;

    public AgencyBookTools(IAgencyBookAPI agencyBookApi, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
    {
        this.agencyBookApi = agencyBookApi ?? throw new ArgumentNullException(nameof(agencyBookApi));
    }

    private const string CreateAgencyDescription = "Create a new agency with name, email, and daily appointment limit.";
    private const string AssignUserToAgencyDescription = "Assign a user to an agency using their email and role(s).";
    private const string CancelAppointmentDescription = "Cancel an appointment by its ID.";
    private const string CreateAppointmentDescription = "Schedule an appointment with a user's email, date, and title.";
    private const string GetAppointmentsByDateDescription = "List all appointments on a given date.";
    private const string HandleNoShowDescription = "Mark an appointment as a no-show using its ID.";
    private const string RescheduleAppointmentDescription = "Move an appointment to a new date and time.";
    private const string UpdateAgencySettingsDescription = "Modify agency settings like max appointments and holidays.";
    private const string InitializeAppointmentSlotsDescription = "Generate available slots for an agency over a date range.";


    [McpServerTool, Description(CreateAgencyDescription)]
    public async Task<object> CreateAgencyAsync(
        [Description("Name of the agency")] string name,
        [Description("Email address for the agency")] string email,
        [Description("Maximum number of appointments allowed per day")] int maxAppointmentsPerDay)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            var response = await agencyBookApi.CreateAgencyAsync(new { name, email, maxAppointmentsPerDay }, GetBearerToken());
            if (response.IsSuccessStatusCode)
            {
                Log.Information("Agency created successfully: {Name}, {Email}", name, email);
                return new { Message = "Agency created successfully." };
            }
            
            var errorMessage = await GetErrorMessage(response);
            Log.Warning("Failed to create agency: {Name}, {Email}. Error: {Error}", name, email, errorMessage);
            return new { Error = new List<string> { errorMessage } };
        }
        catch (Refit.ApiException ex)
        {
            Log.Error(ex, "API error during agency creation: {Name}, {Email}", name, email);
            var errorContent = await ex.GetContentAsAsync<List<string>>();
            return new { Error = errorContent ?? new List<string> { ex.Message } };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during agency creation: {Name}, {Email}", name, email);
            return new { Error = new List<string> { ex.Message } };
        }
    }

    [McpServerTool, Description(AssignUserToAgencyDescription)]
    public async Task<object> AssignUserToAgencyAsync(
        [Description("Optional: Agency email. If provided, admin must execute")] string? agencyEmail,
        [Description("Email address of the user to assign")] string userEmail,
        [Description("List of roles to assign to the user")] List<string> roles)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            var response = await agencyBookApi.AssignUserToAgencyAsync(new { agencyEmail, userEmail, roles }, GetBearerToken());
            if (response.IsSuccessStatusCode)
            {
                Log.Information("User assigned to agency successfully: {UserEmail}", userEmail);
                return new { Message = "User assigned to agency successfully." };
            }
            var errorMessage = await GetErrorMessage(response);
            Log.Warning("Failed to assign user: {UserEmail}. Error: {Error}", userEmail, errorMessage);
            return new { Error = new List<string> { errorMessage } };
        }
        catch (Refit.ApiException ex)
        {
            Log.Error(ex, "API error during user assignment: {UserEmail}", userEmail);
            var errorContent = await ex.GetContentAsAsync<List<string>>();
            return new { Error = errorContent ?? new List<string> { ex.Message } };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during user assignment: {UserEmail}", userEmail);
            return new { Error = new List<string> { ex.Message } };
        }
    }

    [McpServerTool, Description(GetAppointmentsByDateDescription)]
    public async Task<object> GetAppointmentsByDateAsync(DateTime date)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            var response = await agencyBookApi.GetAppointmentsByDateAsync(new { FilterDate = date }, GetBearerToken());
            Log.Information("Retrieved appointments for date: {Date}", date);
            return new { Data = response ?? new List<object>() };
        }
        catch (Refit.ApiException ex)
        {
            Log.Error(ex, "API error retrieving appointments for date: {Date}", date);
            var errorContent = await ex.GetContentAsAsync<List<string>>();
            return new { Error = errorContent ?? new List<string> { ex.Message } };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving appointments for date: {Date}", date);
            return new { Error = new List<string> { ex.Message } };
        }
    }

    [McpServerTool, Description(HandleNoShowDescription)]
    public async Task<object> HandleNoShowAsync(
        [Description("Unique identifier of the appointment (format: GUID string)")] string appointmentId)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            if (!Guid.TryParse(appointmentId, out Guid appointmentGuid))
            {
                return new { Error = new List<string> { "Invalid appointment ID format. Please provide a valid GUID." } };
            }

            var response = await agencyBookApi.HandleNoShowAsync(new { appointmentId = appointmentGuid }, GetBearerToken());
            if (response.IsSuccessStatusCode)
            {
                Log.Information("No-show handled successfully for appointment: {AppointmentId}", appointmentId);
                return new { Message = "No-show handled successfully." };
            }
            var errorMessage = await GetErrorMessage(response);
            Log.Warning("Failed to handle no-show for appointment: {AppointmentId}. Error: {Error}", appointmentId, errorMessage);
            return new { Error = new List<string> { errorMessage } };
        }
        catch (Refit.ApiException ex)
        {
            Log.Error(ex, "API error handling no-show for appointment: {AppointmentId}", appointmentId);
            var errorContent = await ex.GetContentAsAsync<List<string>>();
            return new { Error = errorContent ?? new List<string> { ex.Message } };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling no-show for appointment: {AppointmentId}", appointmentId);
            return new { Error = new List<string> { ex.Message } };
        }
    }

    [McpServerTool, Description(CancelAppointmentDescription)]
    public async Task<object> CancelAppointmentAsync(string appointmentId)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            if (!Guid.TryParse(appointmentId, out Guid appointmentGuid))
            {
                return new { Error = new List<string> { "Invalid appointment ID format. Please provide a valid GUID." } };
            }

            var response = await agencyBookApi.CancelAppointmentAsync(new { appointmentId = appointmentGuid }, GetBearerToken());
            if (response.IsSuccessStatusCode)
            {
                Log.Information("Appointment cancelled successfully: {AppointmentId}", appointmentId);
                return new { Message = "Appointment cancelled successfully." };
            }
            var errorMessage = await GetErrorMessage(response);
            Log.Warning("Failed to cancel appointment: {AppointmentId}. Error: {Error}", appointmentId, errorMessage);
            return new { Error = new List<string> { errorMessage } };
        }
        catch (Refit.ApiException ex)
        {
            Log.Error(ex, "API error cancelling appointment: {AppointmentId}", appointmentId);
            var errorContent = await ex.GetContentAsAsync<List<string>>();
            return new { Error = errorContent ?? new List<string> { ex.Message } };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error cancelling appointment: {AppointmentId}", appointmentId);
            return new { Error = new List<string> { ex.Message } };
        }
    }

    [McpServerTool, Description(CreateAppointmentDescription)]
    public async Task<object> CreateAppointmentAsync(string? agencyEmail, string userEmail, DateTime date, string appointmentName)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            var response = await agencyBookApi.CreateAppointmentAsync(new { agencyEmail, userEmail, date, appointmentName }, GetBearerToken());
            if (response.IsSuccessStatusCode)
            {
                Log.Information("Appointment created successfully: {AppointmentName} for {UserEmail}", appointmentName, userEmail);
                return new { Message = "Appointment created successfully." };
            }
            var errorMessage = await GetErrorMessage(response);
            Log.Warning("Failed to create appointment for user: {UserEmail}. Error: {Error}", userEmail, errorMessage);
            return new { Error = new List<string> { errorMessage } };
        }
        catch (Refit.ApiException ex)
        {
            Log.Error(ex, "API error creating appointment for user: {UserEmail}", userEmail);
            var errorContent = await ex.GetContentAsAsync<List<string>>();
            return new { Error = errorContent ?? new List<string> { ex.Message } };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating appointment for user: {UserEmail}", userEmail);
            return new { Error = new List<string> { ex.Message } };
        }
    }

    [McpServerTool, Description(UpdateAgencySettingsDescription)]
    public async Task<object> UpdateAgencySettingsAsync(string? agencyEmail, int maxAppointmentsPerDay, List<object> holidays, bool? isApproved = null)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            var response = await agencyBookApi.UpdateAgencySettingsAsync(new { agencyEmail, maxAppointmentsPerDay, holidays, isApproved }, GetBearerToken());
            if (response.IsSuccessStatusCode)
            {
                Log.Information("Agency settings updated successfully for: {AgencyEmail}", agencyEmail ?? "current agency");
                return new { Message = "Agency settings updated successfully." };
            }
            var errorMessage = await GetErrorMessage(response);
            Log.Warning("Failed to update agency settings for: {AgencyEmail}. Error: {Error}", agencyEmail ?? "current agency", errorMessage);
            return new { Error = new List<string> { errorMessage } };
        }
        catch (Refit.ApiException ex)
        {
            Log.Error(ex, "API error updating agency settings: {AgencyEmail}", agencyEmail ?? "current agency");
            var errorContent = await ex.GetContentAsAsync<List<string>>();
            return new { Error = errorContent ?? new List<string> { ex.Message } };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating agency settings: {AgencyEmail}", agencyEmail ?? "current agency");
            return new { Error = new List<string> { ex.Message } };
        }
    }

    [McpServerTool, Description(RescheduleAppointmentDescription)]
    public async Task<object> RescheduleAppointmentAsync(
        [Description("Unique identifier of the appointment (format: GUID string)")] string appointmentId,
        [Description("New date and time for the appointment (format: yyyy-MM-dd HH:mm:ss)")] DateTime newDate)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            if (!Guid.TryParse(appointmentId, out Guid appointmentGuid))
            {
                return new { Error = new List<string> { "Invalid appointment ID format. Please provide a valid GUID." } };
            }

            var response = await agencyBookApi.RescheduleAppointmentAsync(new { appointmentId = appointmentGuid, newDate }, GetBearerToken());
            if (response.IsSuccessStatusCode)
            {
                Log.Information("Appointment rescheduled successfully: {AppointmentId} to {NewDate}", appointmentId, newDate);
                return new { Message = "Appointment rescheduled successfully." };
            }
            var errorMessage = await GetErrorMessage(response);
            Log.Warning("Failed to reschedule appointment: {AppointmentId}. Error: {Error}", appointmentId, errorMessage);
            return new { Error = new List<string> { errorMessage } };
        }
        catch (Refit.ApiException ex)
        {
            Log.Error(ex, "API error rescheduling appointment: {AppointmentId}", appointmentId);
            var errorContent = await ex.GetContentAsAsync<List<string>>();
            return new { Error = errorContent ?? new List<string> { ex.Message } };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error rescheduling appointment: {AppointmentId}", appointmentId);
            return new { Error = new List<string> { ex.Message } };
        }
    }

    [McpServerTool, Description(InitializeAppointmentSlotsDescription)]
    public async Task<object> InitializeAppointmentSlotsAsync(
        [Description("Optional: Agency email. If provided, admin must execute")] string? agencyEmail,
        [Description("Start date for slot initialization (format: yyyy-MM-dd HH:mm:ss)")] DateTime startDate,
        [Description("End date for slot initialization (format: yyyy-MM-dd HH:mm:ss)")] DateTime endDate,
        [Description("Duration of each appointment slot (format: hh:mm:ss)")] TimeSpan slotDuration,
        [Description("Number of slots to create per day")] int slotsPerDay,
        [Description("Maximum number of concurrent appointments per slot")] int capacityPerSlot)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            var response = await agencyBookApi.InitializeAppointmentSlotsAsync(new 
            { 
                agencyEmail,
                startDate,
                endDate,
                slotDuration,
                slotsPerDay,
                capacityPerSlot
            }, GetBearerToken());

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Appointment slots initialized successfully for agency: {AgencyEmail} from {StartDate} to {EndDate}", 
                    agencyEmail ?? "current agency", startDate, endDate);
                return new { Message = "Appointment slots initialized successfully." };
            }
            var errorMessage = await GetErrorMessage(response);
            Log.Warning("Failed to initialize appointment slots for agency: {AgencyEmail}. Error: {Error}", 
                agencyEmail ?? "current agency", errorMessage);
            return new { Error = new List<string> { errorMessage } };
        }
        catch (Refit.ApiException ex)
        {
            Log.Error(ex, "API error initializing appointment slots: {AgencyEmail}", agencyEmail ?? "current agency");
            var errorContent = await ex.GetContentAsAsync<List<string>>();
            return new { Error = errorContent ?? new List<string> { ex.Message } };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initializing appointment slots: {AgencyEmail}", agencyEmail ?? "current agency");
            return new { Error = new List<string> { ex.Message } };
        }
    }
}
