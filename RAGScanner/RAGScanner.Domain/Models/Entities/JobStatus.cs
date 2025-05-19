using System.ComponentModel.DataAnnotations;

public class JobStatus : Entity, IAggregateRoot
{
    [Required]
    public string JobId { get; private set; }

    [Required]
    public string Status { get; private set; }

    [Required]
    public string Message { get; private set; }

    public List<string> Urls { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public JobStatus() : base() { } // Parameterless constructor for EF Core    

    // Main constructor
    public JobStatus(
        Guid id,
        string jobId,
        string status,
        string message,
        IEnumerable<string> urls,
        DateTime createdAt) : base(id)
    {
        this.Urls = new List<string>();

        JobId = jobId;
        Status = status;
        Message = message;
        this.Urls.AddRange(urls);
        CreatedAt = createdAt;
        UpdatedAt = null;

        RaiseEvent(new JobStatusEntityEvent(id));
    }

    // Factory method for creating a new job status
    public static Result<JobStatus> Create(
        string jobId,
        string status,
        string message,
        IEnumerable<string> urls)
    {
        var jobStatus = new JobStatus(
            id: Guid.NewGuid(),
            jobId: jobId,
            status: status,
            message: message,
            urls: urls,
            createdAt: DateTime.UtcNow);

        var validationResult = jobStatus.Validate();
        if (!validationResult.Succeeded)
        {
            return Result<JobStatus>.Failure(validationResult.Errors);
        }

        return Result<JobStatus>.SuccessWith(jobStatus);
    }

    public Result UpdateStatus(string newStatus, string newMessage)
    {
        if (string.IsNullOrWhiteSpace(newStatus))
        {
            return Result.Failure(new[] { "Status cannot be empty." });
        }

        if (string.IsNullOrWhiteSpace(newMessage))
        {
            return Result.Failure(new[] { "Message cannot be empty." });
        }

        Status = newStatus;
        Message = newMessage;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success;
    }

    public Result AddUrl(string url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {

            if (Urls.Contains(url))
            {
                return Result.Failure(new[] { "URL already exists." });
            }

            Urls.Add(url);
            UpdatedAt = DateTime.UtcNow;
        }

        return Result.Success;
    }

    private Result Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(JobId))
        {
            errors.Add("Job ID is required.");
        }

        if (string.IsNullOrWhiteSpace(Status))
        {
            errors.Add("Status is required.");
        }

        if (string.IsNullOrWhiteSpace(Message))
        {
            errors.Add("Message is required.");
        }

        return errors.Any() ? Result.Failure(errors) : Result.Success;
    }
}