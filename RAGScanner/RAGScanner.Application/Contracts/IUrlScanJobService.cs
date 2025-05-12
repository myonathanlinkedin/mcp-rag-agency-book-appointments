public interface IUrlScanJobService
{
    Task ProcessAsync(List<string> urls, Guid jobId, string uploaderEmail, CancellationToken cancellationToken);
}
