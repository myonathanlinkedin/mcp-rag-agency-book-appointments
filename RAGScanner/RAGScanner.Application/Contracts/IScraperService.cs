public interface IScraperService
{
    Task<List<ScrapedDocument>> ScrapeUrlsAsync(List<string> urls);
}
