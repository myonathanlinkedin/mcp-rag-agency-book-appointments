using Hangfire;
using MediatR;

public class ScanUrlCommand : BaseCommand<ScanUrlCommand>, IRequest<Result>
{
    public List<string> Urls { get; set; } = new();

    public class ScanUrlCommandHandler : IRequestHandler<UserRequestWrapper<ScanUrlCommand>, Result>
    {
        private readonly IBackgroundJobClient jobClient;
        private readonly IRAGUnitOfWork unitOfWork;

        public ScanUrlCommandHandler(IBackgroundJobClient jobClient, IRAGUnitOfWork unitOfWork)
        {
            this.jobClient = jobClient;
            this.unitOfWork = unitOfWork;
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

            var jobs = await Task.WhenAll(command.Urls.Select(url => unitOfWork.JobStatuses.CreateJobAsync(new List<string> { url })));
            await unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var (url, jobId) in command.Urls.Zip(jobs))
            {
                jobClient.Enqueue<IUrlScanJobService>(svc =>
                    svc.ProcessAsync(new List<string> { url }, new Guid(jobId), user.Email, cancellationToken));
            }

            return Result.Success;
        }
    }
}