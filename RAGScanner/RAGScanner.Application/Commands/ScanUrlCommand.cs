using Hangfire;
using MediatR;

public class ScanUrlCommand : BaseCommand<ScanUrlCommand>, IRequest<Result>
{
    public List<string> Urls { get; set; } = new();

    public class ScanUrlCommandHandler : IRequestHandler<UserRequestWrapper<ScanUrlCommand>, Result>
    {
        private readonly IBackgroundJobClient jobClient;
        private readonly IJobStatusRepository jobStatusStore;

        public ScanUrlCommandHandler(IBackgroundJobClient jobClient, IJobStatusRepository jobStatusStore)
        {
            this.jobClient = jobClient;
            this.jobStatusStore = jobStatusStore;
        }

        public async Task<Result> Handle(UserRequestWrapper<ScanUrlCommand> request, CancellationToken cancellationToken)
        {
            var command = request.Request;
            var user = request.User;

            if (command.Urls == null || !command.Urls.Any())
            {
                return Result.Failure(new[] { "No URLs provided for scanning." });
            }

            if (string.IsNullOrEmpty(user.Email))
            {
                return Result.Failure(new[] { "Authenticated user does not have a valid email." });
            }

            var jobs = await Task.WhenAll(command.Urls.Select(url => jobStatusStore.CreateJobAsync(new List<string> { url })));

            foreach (var (url, jobId) in command.Urls.Zip(jobs))
            {
                jobClient.Enqueue<IUrlScanJobService>(svc =>
                    svc.ProcessAsync(new List<string> { url }, new Guid(jobId), user.Email));
            }

            return Result.Success;
        }
    }
}