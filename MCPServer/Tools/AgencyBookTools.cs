using ModelContextProtocol.Server;
using Serilog;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;

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
    public async Task<string> CreateAgencyAsync(
        [Description("Name of the agency")] string name,
        [Description("Email address for the agency")] string email,
        [Description("Maximum number of appointments allowed per day")] int maxAppointmentsPerDay)
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
    public async Task<string> AssignUserToAgencyAsync(
        [Description("Optional: Agency email. If provided, admin must execute")] string? agencyEmail,
        [Description("Email address of the user to assign")] string userEmail,
        [Description("List of roles to assign to the user")] List<string> roles)
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
    public async Task<List<object>> GetAppointmentsByDateAsync(
        [Description("Date to retrieve appointments for")] DateTime date)
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
    public async Task<string> HandleNoShowAsync(
        [Description("Unique identifier of the appointment to mark as no-show")] Guid appointmentId)
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

    [McpServerTool, Description(CancelAppointmentDescription)]
    public async Task<string> CancelAppointmentAsync(
        [Description("Unique identifier of the appointment to cancel")] Guid appointmentId)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return token;

            var response = await agencyBookApi.CancelAppointmentAsync(new { appointmentId }, GetBearerToken());
            if (response.IsSuccessStatusCode)
            {
                Log.Information("Appointment cancelled successfully: {AppointmentId}", appointmentId);
                return "Appointment cancelled successfully.";
            }
            Log.Warning("Failed to cancel appointment: {AppointmentId}. Status code: {StatusCode}", appointmentId, response.StatusCode);
            return $"Failed to cancel appointment. Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            Log.Error("Exception during appointment cancellation: {AppointmentId}. Error: {Error}", appointmentId, ex.Message);
            return "An error occurred while cancelling the appointment.";
        }
    }

    [McpServerTool, Description(CreateAppointmentDescription)]
    public async Task<string> CreateAppointmentAsync(
        [Description("Optional: Agency email. If provided, admin must execute")] string? agencyEmail,
        [Description("Email address of the user booking the appointment")] string userEmail,
        [Description("Date and time of the appointment")] DateTime date,
        [Description("Name or description of the appointment")] string appointmentName)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return token;

            var response = await agencyBookApi.CreateAppointmentAsync(new { agencyEmail, userEmail, date, appointmentName }, GetBearerToken());
            if (response.IsSuccessStatusCode)
            {
                Log.Information("Appointment created successfully: {AppointmentName} for {UserEmail}", appointmentName, userEmail);
                return "Appointment created successfully.";
            }
            Log.Warning("Failed to create appointment for user: {UserEmail}. Status code: {StatusCode}", userEmail, response.StatusCode);
            return $"Failed to create appointment. Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            Log.Error("Exception during appointment creation: {UserEmail}. Error: {Error}", userEmail, ex.Message);
            return "An error occurred while creating the appointment.";
        }
    }

    [McpServerTool, Description(UpdateAgencySettingsDescription)]
    public async Task<string> UpdateAgencySettingsAsync(
        [Description("Optional: Agency email. If provided, admin must execute")] string? agencyEmail,
        [Description("Maximum number of appointments allowed per day")] int maxAppointmentsPerDay,
        [Description("List of holiday dates and their descriptions")] List<object> holidays,
        [Description("Optional: Set agency approval status. Only admins can set this")] bool? isApproved = null)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return token;

            var response = await agencyBookApi.UpdateAgencySettingsAsync(new { agencyEmail, maxAppointmentsPerDay, holidays, isApproved }, GetBearerToken());
            if (response.IsSuccessStatusCode)
            {
                Log.Information("Agency settings updated successfully for: {AgencyEmail}", agencyEmail ?? "current agency");
                return "Agency settings updated successfully.";
            }
            Log.Warning("Failed to update agency settings for: {AgencyEmail}. Status code: {StatusCode}", agencyEmail ?? "current agency", response.StatusCode);
            return $"Failed to update agency settings. Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            Log.Error("Exception during agency settings update: {AgencyEmail}. Error: {Error}", agencyEmail ?? "current agency", ex.Message);
            return "An error occurred while updating agency settings.";
        }
    }

    [McpServerTool, Description(RescheduleAppointmentDescription)]
    public async Task<string> RescheduleAppointmentAsync(
        [Description("Unique identifier of the appointment to reschedule")] Guid appointmentId,
        [Description("New date and time for the appointment")] DateTime newDate)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return token;

            var response = await agencyBookApi.RescheduleAppointmentAsync(new { appointmentId, newDate }, GetBearerToken());
            if (response.IsSuccessStatusCode)
            {
                Log.Information("Appointment rescheduled successfully: {AppointmentId} to {NewDate}", appointmentId, newDate);
                return "Appointment rescheduled successfully.";
            }
            Log.Warning("Failed to reschedule appointment: {AppointmentId}. Status code: {StatusCode}", appointmentId, response.StatusCode);
            return $"Failed to reschedule appointment. Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            Log.Error("Exception during appointment rescheduling: {AppointmentId}. Error: {Error}", appointmentId, ex.Message);
            return "An error occurred while rescheduling the appointment.";
        }
    }

    [McpServerTool, Description(InitializeAppointmentSlotsDescription)]
    public async Task<string> InitializeAppointmentSlotsAsync(
        [Description("Optional: Agency email. If provided, admin must execute")] string? agencyEmail,
        [Description("Start date for slot initialization")] DateTime startDate,
        [Description("End date for slot initialization")] DateTime endDate,
        [Description("Duration of each appointment slot")] TimeSpan slotDuration,
        [Description("Number of slots to create per day")] int slotsPerDay,
        [Description("Maximum number of concurrent appointments per slot")] int capacityPerSlot)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return token;

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
                return "Appointment slots initialized successfully.";
            }
            Log.Warning("Failed to initialize appointment slots for agency: {AgencyEmail}. Status code: {StatusCode}", 
                agencyEmail ?? "current agency", response.StatusCode);
            return $"Failed to initialize appointment slots. Status code: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            Log.Error("Exception during appointment slots initialization: {AgencyEmail}. Error: {Error}", 
                agencyEmail ?? "current agency", ex.Message);
            return "An error occurred while initializing appointment slots.";
        }
    }

    [McpServerTool, Description(GetAgencyByEmailDescription)]
    public async Task<object> GetAgencyByEmailAsync(
        [Description("Email address of the agency to retrieve")] string email)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return null;

            var agency = await agencyBookApi.GetAgencyByEmailAsync(email, GetBearerToken());
            Log.Information("Retrieved agency details for email: {Email}", email);
            return agency;
        }
        catch (Exception ex)
        {
            Log.Error("Exception while retrieving agency details for email: {Email}. Error: {Error}", email, ex.Message);
            return null;
        }
    }

    [McpServerTool, Description(GetApprovedAgenciesDescription)]
    public async Task<List<object>> GetApprovedAgenciesAsync()
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new List<object>();

            var agencies = await agencyBookApi.GetApprovedAgenciesAsync(GetBearerToken());
            Log.Information("Retrieved {Count} approved agencies", agencies.Count);
            return agencies;
        }
        catch (Exception ex)
        {
            Log.Error("Exception while retrieving approved agencies. Error: {Error}", ex.Message);
            return new List<object>();
        }
    }

    [McpServerTool, Description(GetAvailableSlotsDescription)]
    public async Task<List<object>> GetAvailableSlotsAsync(
        [Description("Unique identifier of the agency")] Guid agencyId,
        [Description("Date to check for available slots")] DateTime date)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new List<object>();

            var slots = await agencyBookApi.GetAvailableSlotsAsync(agencyId, date, GetBearerToken());
            Log.Information("Retrieved {Count} available slots for agency {AgencyId} on {Date}", slots.Count, agencyId, date);
            return slots;
        }
        catch (Exception ex)
        {
            Log.Error("Exception while retrieving available slots for agency {AgencyId}. Error: {Error}", agencyId, ex.Message);
            return new List<object>();
        }
    }

    [McpServerTool, Description(GetUpcomingAppointmentsDescription)]
    public async Task<List<object>> GetUpcomingAppointmentsAsync(
        [Description("Unique identifier of the agency")] Guid agencyId,
        [Description("Starting date to retrieve upcoming appointments")] DateTime fromDate)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return new List<object>();

            var appointments = await agencyBookApi.GetUpcomingAppointmentsAsync(agencyId, fromDate, GetBearerToken());
            Log.Information("Retrieved {Count} upcoming appointments for agency {AgencyId} from {Date}", appointments.Count, agencyId, fromDate);
            return appointments;
        }
        catch (Exception ex)
        {
            Log.Error("Exception while retrieving upcoming appointments for agency {AgencyId}. Error: {Error}", agencyId, ex.Message);
            return new List<object>();
        }
    }

    [McpServerTool, Description(GetNextAvailableDateDescription)]
    public async Task<DateTime?> GetNextAvailableDateAsync(
        [Description("Unique identifier of the agency")] Guid agencyId,
        [Description("Preferred date to start searching from")] DateTime preferredDate)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return null;

            var nextDate = await agencyBookApi.GetNextAvailableDateAsync(agencyId, preferredDate, GetBearerToken());
            if (nextDate.HasValue)
            {
                Log.Information("Next available date for agency {AgencyId} after {PreferredDate} is {NextDate}", agencyId, preferredDate, nextDate.Value);
            }
            else
            {
                Log.Information("No available dates found for agency {AgencyId} after {PreferredDate}", agencyId, preferredDate);
            }
            return nextDate;
        }
        catch (Exception ex)
        {
            Log.Error("Exception while getting next available date for agency {AgencyId}. Error: {Error}", agencyId, ex.Message);
            return null;
        }
    }

    [McpServerTool, Description(IsBookingAllowedDescription)]
    public async Task<bool> IsBookingAllowedAsync(
        [Description("Unique identifier of the agency to check booking permission")] Guid agencyId)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return false;

            var isAllowed = await agencyBookApi.IsBookingAllowedAsync(agencyId, GetBearerToken());
            Log.Information("Booking allowed status for agency {AgencyId}: {IsAllowed}", agencyId, isAllowed);
            return isAllowed;
        }
        catch (Exception ex)
        {
            Log.Error("Exception while checking booking allowed status for agency {AgencyId}. Error: {Error}", agencyId, ex.Message);
            return false;
        }
    }

    [McpServerTool, Description(HasAvailableSlotDescription)]
    public async Task<bool> HasAvailableSlotAsync(
        [Description("Unique identifier of the agency")] Guid agencyId,
        [Description("Date to check for slot availability")] DateTime date)
    {
        try
        {
            var token = GetTokenFromHttpContext();
            if (token == LogAndReturnMissingToken()) return false;

            var hasSlot = await agencyBookApi.HasAvailableSlotAsync(agencyId, date, GetBearerToken());
            Log.Information("Available slot status for agency {AgencyId} on {Date}: {HasSlot}", agencyId, date, hasSlot);
            return hasSlot;
        }
        catch (Exception ex)
        {
            Log.Error("Exception while checking available slots for agency {AgencyId}. Error: {Error}", agencyId, ex.Message);
            return false;
        }
    }
}
