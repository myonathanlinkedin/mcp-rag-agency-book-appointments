using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Transactions;

internal class JobStatusRepository : DataRepository<RAGDbContext, JobStatus>, IJobStatusRepository
{
    private readonly ILogger<JobStatusRepository> logger;

    public JobStatusRepository(RAGDbContext db, ILogger<JobStatusRepository> logger) : base(db)
        => this.logger = logger;

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
            logger.LogError("Failed to create JobStatus: {Error}", errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        var jobStatus = jobStatusResult.Data;

        try
        {
            await Data.JobStatuses.AddAsync(jobStatus);
            await Data.SaveChangesAsync();

            logger.LogInformation("Created job with ID {JobId}", jobId);
            return jobId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create job with ID {JobId}", jobId);
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
            logger.LogError(ex, "Failed to retrieve job status for JobId {JobId}", jobId);
            throw;
        }
    }

    public async Task UpdateJobStatusAsync(string jobId, JobStatusType status, string message = null)
    {
        try
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            
            var job = await Data.JobStatuses
                .FirstOrDefaultAsync(js => js.JobId == jobId);

            if (job == null)
            {
                logger.LogWarning("Attempted to update non-existent job with ID {JobId}", jobId);
                return;
            }

            Data.Entry(job).State = EntityState.Detached;

            job = await Data.JobStatuses
                .FirstOrDefaultAsync(js => js.JobId == jobId);

            if (job != null)
            {
                job.UpdateStatus(status.ToString(), message ?? job.Message);
                
                try
                {
                    await Data.SaveChangesAsync();
                    scope.Complete();
                    logger.LogInformation("Updated job {JobId} with status: {Status}, message: {Message}", 
                        jobId, status, job.Message);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    logger.LogWarning(ex, "Concurrency conflict detected while updating job {JobId}. Retrying operation.", jobId);
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update job status for JobId {JobId}", jobId);
            throw;
        }
    }
}
