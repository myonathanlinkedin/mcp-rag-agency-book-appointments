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
    private const string GetAgencyByEmailDescription = "Look up agency details using its email.";
    private const string GetApprovedAgenciesDescription = "List all agencies that have been approved.";
    private const string GetAvailableSlotsDescription = "View open appointment slots for an agency on a specific date.";
    private const string GetUpcomingAppointmentsDescription = "List upcoming appointments for an agency starting from a date.";
    private const string GetNextAvailableDateDescription = "Find the next date with available appointment slots.";
    private const string IsBookingAllowedDescription = "Check if the agency currently allows bookings.";
    private const string HasAvailableSlotDescription = "Determine if any slots are open on a specific date.";

    

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
    public async Task<object> GetAppointmentsByDateAsync(
        [Description("Date to retrieve appointments for (format: yyyy-MM-dd HH:mm:ss)")] DateTime date)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            var response = await agencyBookApi.GetAppointmentsByDateAsync(date, GetBearerToken());
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

    [McpServerTool, Description(GetAgencyByEmailDescription)]
    public async Task<object> GetAgencyByEmailAsync(
        [Description("Email address of the agency to retrieve")] string email)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            var agency = await agencyBookApi.GetAgencyByEmailAsync(email, GetBearerToken());
            Log.Information("Retrieved agency details for email: {Email}", email);
            return new { Data = agency };
        }
        catch (Refit.ApiException ex)
        {
            Log.Error(ex, "API error retrieving agency details for email: {Email}", email);
            var errorContent = await ex.GetContentAsAsync<List<string>>();
            return new { Error = errorContent ?? new List<string> { ex.Message } };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving agency details for email: {Email}", email);
            return new { Error = new List<string> { ex.Message } };
        }
    }

    [McpServerTool, Description(GetApprovedAgenciesDescription)]
    public async Task<object> GetApprovedAgenciesAsync()
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            var agencies = await agencyBookApi.GetApprovedAgenciesAsync(GetBearerToken());
            Log.Information("Retrieved {Count} approved agencies", agencies.Count);
            return new { Data = agencies };
        }
        catch (Refit.ApiException ex)
        {
            Log.Error(ex, "API error retrieving approved agencies");
            var errorContent = await ex.GetContentAsAsync<List<string>>();
            return new { Error = errorContent ?? new List<string> { ex.Message } };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving approved agencies");
            return new { Error = new List<string> { ex.Message } };
        }
    }

    [McpServerTool, Description(GetAvailableSlotsDescription)]
    public async Task<object> GetAvailableSlotsAsync(
        [Description("Unique identifier of the agency (format: GUID string)")] string agencyId,
        [Description("Date to check for available slots (format: yyyy-MM-dd HH:mm:ss)")] DateTime date)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            if (!Guid.TryParse(agencyId, out Guid agencyGuid))
            {
                return new { Error = new List<string> { "Invalid agency ID format. Please provide a valid GUID." } };
            }

            var slots = await agencyBookApi.GetAvailableSlotsAsync(agencyGuid, date, GetBearerToken());
            Log.Information("Retrieved {Count} available slots for agency {AgencyId} on {Date}", slots.Count, agencyId, date);
            return new { Data = slots };
        }
        catch (Refit.ApiException ex)
        {
            Log.Error(ex, "API error retrieving available slots for agency {AgencyId}", agencyId);
            var errorContent = await ex.GetContentAsAsync<List<string>>();
            return new { Error = errorContent ?? new List<string> { ex.Message } };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving available slots for agency {AgencyId}", agencyId);
            return new { Error = new List<string> { ex.Message } };
        }
    }

    [McpServerTool, Description(GetUpcomingAppointmentsDescription)]
    public async Task<object> GetUpcomingAppointmentsAsync(
        [Description("Unique identifier of the agency (format: GUID string)")] string agencyId,
        [Description("Starting date to retrieve upcoming appointments (format: yyyy-MM-dd HH:mm:ss)")] DateTime fromDate)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            if (!Guid.TryParse(agencyId, out Guid agencyGuid))
            {
                return new { Error = new List<string> { "Invalid agency ID format. Please provide a valid GUID." } };
            }

            var appointments = await agencyBookApi.GetUpcomingAppointmentsAsync(agencyGuid, fromDate, GetBearerToken());
            Log.Information("Retrieved {Count} upcoming appointments for agency {AgencyId} from {Date}", appointments.Count, agencyId, fromDate);
            return new { Data = appointments };
        }
        catch (Refit.ApiException ex)
        {
            Log.Error(ex, "API error retrieving upcoming appointments for agency {AgencyId}", agencyId);
            var errorContent = await ex.GetContentAsAsync<List<string>>();
            return new { Error = errorContent ?? new List<string> { ex.Message } };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving upcoming appointments for agency {AgencyId}", agencyId);
            return new { Error = new List<string> { ex.Message } };
        }
    }

    [McpServerTool, Description(GetNextAvailableDateDescription)]
    public async Task<object> GetNextAvailableDateAsync(
        [Description("Unique identifier of the agency (format: GUID string)")] string agencyId,
        [Description("Preferred date to start searching from (format: yyyy-MM-dd HH:mm:ss)")] DateTime preferredDate)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            if (!Guid.TryParse(agencyId, out Guid agencyGuid))
            {
                return new { Error = new List<string> { "Invalid agency ID format. Please provide a valid GUID." } };
            }

            var nextDate = await agencyBookApi.GetNextAvailableDateAsync(agencyGuid, preferredDate, GetBearerToken());
            if (nextDate.HasValue)
            {
                Log.Information("Next available date for agency {AgencyId} after {PreferredDate} is {NextDate}", agencyId, preferredDate, nextDate.Value);
                return new { Data = nextDate.Value };
            }
            Log.Information("No available dates found for agency {AgencyId} after {PreferredDate}", agencyId, preferredDate);
            return new { Data = (DateTime?)null };
        }
        catch (Refit.ApiException ex)
        {
            Log.Error(ex, "API error getting next available date for agency {AgencyId}", agencyId);
            var errorContent = await ex.GetContentAsAsync<List<string>>();
            return new { Error = errorContent ?? new List<string> { ex.Message } };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting next available date for agency {AgencyId}", agencyId);
            return new { Error = new List<string> { ex.Message } };
        }
    }

    [McpServerTool, Description(IsBookingAllowedDescription)]
    public async Task<object> IsBookingAllowedAsync(
        [Description("Unique identifier of the agency (format: GUID string)")] string agencyId)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            if (!Guid.TryParse(agencyId, out Guid agencyGuid))
            {
                return new { Error = new List<string> { "Invalid agency ID format. Please provide a valid GUID." } };
            }

            var isAllowed = await agencyBookApi.IsBookingAllowedAsync(agencyGuid, GetBearerToken());
            Log.Information("Booking allowed status for agency {AgencyId}: {IsAllowed}", agencyId, isAllowed);
            return new { Data = isAllowed };
        }
        catch (Refit.ApiException ex)
        {
            Log.Error(ex, "API error checking booking allowed status for agency {AgencyId}", agencyId);
            var errorContent = await ex.GetContentAsAsync<List<string>>();
            return new { Error = errorContent ?? new List<string> { ex.Message } };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking booking allowed status for agency {AgencyId}", agencyId);
            return new { Error = new List<string> { ex.Message } };
        }
    }

    [McpServerTool, Description(HasAvailableSlotDescription)]
    public async Task<object> HasAvailableSlotAsync(
        [Description("Unique identifier of the agency (format: GUID string)")] string agencyId,
        [Description("Date to check for slot availability (format: yyyy-MM-dd HH:mm:ss)")] DateTime date)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new { Error = new List<string> { "Authentication token is missing or invalid." } };

            if (!Guid.TryParse(agencyId, out Guid agencyGuid))
            {
                return new { Error = new List<string> { "Invalid agency ID format. Please provide a valid GUID." } };
            }

            var hasSlot = await agencyBookApi.HasAvailableSlotAsync(agencyGuid, date, GetBearerToken());
            Log.Information("Available slot status for agency {AgencyId} on {Date}: {HasSlot}", agencyId, date, hasSlot);
            return new { Data = hasSlot };
        }
        catch (Refit.ApiException ex)
        {
            Log.Error(ex, "API error checking available slots for agency {AgencyId}", agencyId);
            var errorContent = await ex.GetContentAsAsync<List<string>>();
            return new { Error = errorContent ?? new List<string> { ex.Message } };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking available slots for agency {AgencyId}", agencyId);
            return new { Error = new List<string> { ex.Message } };
        }
    }

    
}
