using Marten.Services;
using Microsoft.Extensions.Logging;

public class UrlScanJobService : IUrlScanJobService
{
    private readonly IScraperService scraperService;
    private readonly IDocumentParserService parserService;
    private readonly IVectorStoreService vectorStore;
    private readonly IEmbeddingService embeddingService;
    private readonly IRAGUnitOfWork unitOfWork;
    private readonly IEventDispatcher eventDispatcher;
    private readonly ILogger<UrlScanJobService> logger;

    public UrlScanJobService(
        IScraperService scraperService,
        IDocumentParserService parserService,
        IVectorStoreService vectorStore,
        IEmbeddingService embeddingService,
        IRAGUnitOfWork unitOfWork,
        IEventDispatcher eventDispatcher,
        ILogger<UrlScanJobService> logger)
    {
        this.scraperService = scraperService;
        this.parserService = parserService;
        this.vectorStore = vectorStore;
        this.embeddingService = embeddingService;
        this.unitOfWork = unitOfWork;
        this.eventDispatcher = eventDispatcher;
        this.logger = logger;
    }

    public async Task ProcessAsync(List<string> urls, Guid jobId, string uploaderEmail, CancellationToken cancellationToken)
    {
        try
        {
            await UpdateJobStatusAsync(jobId, JobStatusType.InProgress, "Processing");

            var scrapedDocs = await TryScrapeAsync(urls);
            if (!scrapedDocs.Any())
            {
                await UpdateJobStatusAsync(jobId, JobStatusType.Failed, "Nothing scraped.");
                await DispatchScanEvent("N/A", uploaderEmail, "Failed", null, "No content available", cancellationToken);
                return;
            }

            var tasks = new List<Task>();
            foreach (var doc in scrapedDocs)
            {
                var parsedPages = await ParseDocumentPagesAsync(doc);
                foreach (var content in parsedPages.Where(c => !string.IsNullOrWhiteSpace(c.Content)))
                {
                    tasks.Add(ProcessPageAsync(doc, content));
                }
            }

            await Task.WhenAll(tasks);
            await UpdateJobStatusAsync(jobId, JobStatusType.Completed, "Completed");

            // Dispatch events for successful processing
            foreach (var doc in scrapedDocs)
            {
                var parsedPages = await ParseDocumentPagesAsync(doc);
                foreach (var (content, index) in parsedPages.Select((page, idx) => (page.Content, idx)))
                {
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        await DispatchScanEvent(doc.Url, uploaderEmail, "Success", doc.IsPdf ? index + 1 : null, content, cancellationToken);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing job {JobId}", jobId);
            await UpdateJobStatusAsync(jobId, JobStatusType.Failed, $"Error: {ex.Message}");
            throw;
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

    private async Task<IEnumerable<DocumentContent>> ParseDocumentPagesAsync(ScrapedDocument doc)
    {
        try
        {
            if (doc.IsPdf)
            {
                var pages = await parserService.ParsePdfPerPage(doc.ContentBytes);
                return pages.Select((content, index) => new DocumentContent(content, index));
            }
            else
            {
                var pages = await parserService.ParseHtml(doc.ContentText);
                return pages.Select((content, index) => new DocumentContent(content, index));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse document from {Url}", doc.Url);
            return Enumerable.Empty<DocumentContent>();
        }
    }

    private async Task ProcessPageAsync(ScrapedDocument doc, DocumentContent pageContent)
    {
        try
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process page {Index} from {Url}", pageContent.Index, doc.Url);
            throw;
        }
    }

    private async Task UpdateJobStatusAsync(Guid jobId, JobStatusType status, string message)
    {
        try
        {
            await unitOfWork.JobStatuses.UpdateJobStatusAsync(jobId.ToString(), status, message);
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update job status.");
            throw;
        }
    }

    private async Task DispatchScanEvent(string documentUrl, string uploaderEmail, string status, int? pageNumber, string contentSnippet, CancellationToken cancellationToken)
    {
        try
        {
            await eventDispatcher.Dispatch(new DocumentScanEvent(documentUrl, DateTime.UtcNow, uploaderEmail, status, pageNumber, contentSnippet), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch scan event for {Url}", documentUrl);
            throw;
        }
    }

    private string ExtractTitle(string html)
    {
        try
        {
            const string startTag = "<title>", endTag = "</title>";
            var start = html.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
            var end = html.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);
            return (start == -1 || end == -1 || end <= start)
                ? "Untitled"
                : html[(start + startTag.Length)..end].Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to extract title from HTML");
            return "Untitled";
        }
    }
}