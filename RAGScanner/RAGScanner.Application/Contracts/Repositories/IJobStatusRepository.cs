public interface IJobStatusRepository
{
    Task<string> CreateJobAsync(List<string> urls);
    Task<JobStatus> GetJobStatusAsync(string jobId);
    Task UpdateJobStatusAsync(string jobId, JobStatusType status, string message = null);
}
