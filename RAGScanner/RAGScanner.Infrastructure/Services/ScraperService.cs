using Microsoft.Extensions.Logging;

public class ScraperService : IScraperService
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<ScraperService> logger;

    public ScraperService(IHttpClientFactory httpClientFactory, ILogger<ScraperService> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public async Task<List<ScrapedDocument>> ScrapeUrlsAsync(List<string> urls)
    {
        var client = httpClientFactory.CreateClient();
        var tasks = urls.Select(url => ScrapeUrlAsync(client, url));
        var results = await Task.WhenAll(tasks);
        return results.Where(r => r != null).ToList();
    }

    private async Task<ScrapedDocument> ScrapeUrlAsync(HttpClient client, string url)
    {
        try
        {
            var response = await HttpHelper.GetAsync(client, url);
            var contentType = response.Content.Headers.ContentType?.MediaType;
            var isPdf = ContentTypeDetector.IsPdf(contentType);

            var contentBytes = await GetContentBytesAsync(response, isPdf);
            var contentText = isPdf ? null : await response.Content.ReadAsStringAsync();

            return new ScrapedDocument
            {
                Url = url,
                ContentBytes = contentBytes,
                ContentText = contentText,
                IsPdf = isPdf,
                ScrapedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to scrape URL: {Url}", url);
            return null;
        }
    }

    private async Task<byte[]> GetContentBytesAsync(HttpResponseMessage response, bool isPdf)
    {
        if (!isPdf) return await response.Content.ReadAsByteArrayAsync();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var ms = new MemoryStream();
        var buffer = new byte[81920]; // 80 KB buffer for better performance

        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
        {
            await ms.WriteAsync(buffer.AsMemory(0, bytesRead));
        }

        return ms.ToArray();
    }
}