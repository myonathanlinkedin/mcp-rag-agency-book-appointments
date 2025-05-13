using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Transactions;
using Microsoft.Extensions.DependencyInjection;

internal class JobStatusRepository : BufferedDataRepository<RAGDbContext, JobStatus>, IJobStatusRepository
{
    private readonly ILogger<JobStatusRepository> _logger;

    public JobStatusRepository(
        RAGDbContext db,
        ILogger<JobStatusRepository> logger)
        : base(db, logger)
    {
        _logger = logger;
    }

    public async Task<string> CreateJobAsync(List<string> urls)
    {
        var jobId = Guid.NewGuid().ToString();

        var jobStatusResult = JobStatus.Create(
            jobId: jobId,
            status: JobStatusType.Pending.ToString(),
            message: "Started",
            urls: urls);

        if (!jobStatusResult.Succeeded)
        {
            var errorMessage = string.Join("; ", jobStatusResult.Errors);
            _logger.LogError("Failed to create JobStatus: {Error}", errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        var jobStatus = jobStatusResult.Data;

        try
        {
            await SaveAsync(jobStatus);
            _logger.LogInformation("Created job with ID {JobId}", jobId);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create job with ID {JobId}", jobId);
            throw;
        }
    }

    public async Task<JobStatus> GetJobStatusAsync(string jobId)
    {
        try
        {
            var jobStatus = await Data.JobStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(js => js.JobId == jobId);

            if (jobStatus == null)
            {
                throw new InvalidOperationException($"Job with ID {jobId} not found.");
            }

            return jobStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve job status for JobId {JobId}", jobId);
            throw;
        }
    }

    public async Task UpdateJobStatusAsync(string jobId, JobStatusType status, string message = null)
    {
        try
        {
            var job = await Data.JobStatuses
                .FirstOrDefaultAsync(js => js.JobId == jobId);

            if (job == null)
            {
                _logger.LogWarning("Attempted to update non-existent job with ID {JobId}", jobId);
                return;
            }

            job.UpdateStatus(status.ToString(), message ?? job.Message);
            await UpsertAsync(job, CancellationToken.None);
            
            _logger.LogInformation("Updated job {JobId} with status: {Status}, message: {Message}", 
                jobId, status, job.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update job status for JobId {JobId}", jobId);
            throw;
        }
    }
}
