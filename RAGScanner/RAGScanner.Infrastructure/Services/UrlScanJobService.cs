using Microsoft.Extensions.Logging;

public class UrlScanJobService : IUrlScanJobService
{
    private readonly IScraperService scraperService;
    private readonly IDocumentParserService parserService;
    private readonly IVectorStoreService vectorStore;
    private readonly IEmbeddingService embeddingService;
    private readonly IJobStatusRepository jobStatusRepository;
    private readonly IEventDispatcher eventDispatcher;
    private readonly ILogger<UrlScanJobService> logger;

    public UrlScanJobService(
        IScraperService scraperService,
        IDocumentParserService parserService,
        IVectorStoreService vectorStore,
        IEmbeddingService embeddingService,
        IJobStatusRepository jobStatusRepository,
        IEventDispatcher eventDispatcher,
        ILogger<UrlScanJobService> logger)
    {
        this.scraperService = scraperService;
        this.parserService = parserService;
        this.vectorStore = vectorStore;
        this.embeddingService = embeddingService;
        this.jobStatusRepository = jobStatusRepository;
        this.eventDispatcher = eventDispatcher;
        this.logger = logger;
    }

    public async Task ProcessAsync(List<string> urls, Guid jobId, string uploaderEmail, CancellationToken cancellationToken)
    {
        await UpdateJobStatusAsync(jobId, JobStatusType.InProgress, "Processing");

        var scrapedDocs = await TryScrapeAsync(urls);
        if (!scrapedDocs.Any())
        {
            await UpdateJobStatusAsync(jobId, JobStatusType.Failed, "Nothing scraped.");
            await DispatchScanEvent("N/A", uploaderEmail, "Failed", null, "No content available", cancellationToken);
            return;
        }

        var tasks = scrapedDocs
            .SelectMany(doc => ParseDocumentPages(doc)
                .Where(content => !string.IsNullOrWhiteSpace(content.Content))
                .Select(content => ProcessPageAsync(doc, content)));

        await Task.WhenAll(tasks);
        await UpdateJobStatusAsync(jobId, JobStatusType.Completed, "Completed");

        foreach (var doc in scrapedDocs)
        {
            var parsedPages = ParseDocumentPages(doc);
            foreach (var (content, index) in parsedPages.Select((page, idx) => (page.Content, idx)))
            {
                await DispatchScanEvent(doc.Url, uploaderEmail, "Success", doc.IsPdf ? index + 1 : null, content, cancellationToken: cancellationToken);
            }
        }
    }

    private async Task<IEnumerable<ScrapedDocument>> TryScrapeAsync(List<string> urls)
    {
        try
        {
            return await scraperService.ScrapeUrlsAsync(urls);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scraping failed.");
            return Enumerable.Empty<ScrapedDocument>();
        }
    }

    private IEnumerable<DocumentContent> ParseDocumentPages(ScrapedDocument doc) =>
        doc.IsPdf
            ? parserService.ParsePdfPerPage(doc.ContentBytes)
                .Select((content, index) => new DocumentContent(content ?? string.Empty, index))
            : new[] { new DocumentContent(parserService.ParseHtml(doc.ContentText) ?? string.Empty, 0) };

    private async Task ProcessPageAsync(ScrapedDocument doc, DocumentContent pageContent)
    {
        var embedding = await embeddingService.GenerateEmbeddingAsync(pageContent.Content, default);
        var metadata = new DocumentMetadata
        {
            Url = doc.Url,
            Title = doc.IsPdf ? $"Page {pageContent.Index + 1}" : ExtractTitle(doc.ContentText),
            SourceType = doc.IsPdf ? "pdf" : "html",
            Content = pageContent.Content,
            ScrapedAt = DateTime.UtcNow
        };
        await vectorStore.SaveDocumentAsync(new DocumentVector { Embedding = embedding, Metadata = metadata }, embedding.Length);
    }

    private async Task UpdateJobStatusAsync(Guid jobId, JobStatusType status, string message)
    {
        try
        {
            await jobStatusRepository.UpdateJobStatusAsync(jobId.ToString(), status, message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update job status.");
        }
    }

    private async Task DispatchScanEvent(string documentUrl, string uploaderEmail, string status, int? pageNumber, string contentSnippet, CancellationToken cancellationToken)
    {
        await eventDispatcher.Dispatch(new DocumentScanEvent(documentUrl, DateTime.UtcNow, uploaderEmail, status, pageNumber, contentSnippet), cancellationToken);
    }

    private string ExtractTitle(string html)
    {
        const string startTag = "<title>", endTag = "</title>";
        var start = html.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
        var end = html.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);
        return (start == -1 || end == -1 || end <= start)
            ? "Untitled"
            : html[(start + startTag.Length)..end].Trim();
    }
}