using HtmlAgilityPack;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class DocumentParserService : IDocumentParserService
{
    private readonly IMCPServerRequester? mCPServerRequester;
    private readonly ITextCleaningService textCleaningService;
    private readonly ILogger<DocumentParserService> logger;
    private readonly bool useLLMDocumentParsing;
    private readonly int maxChunkSize;

    private const int MaxRetries = 3;
    private const string ProcessPromptTemplate = "Fix and improve the following text while maintaining its meaning. " +
                                                    "Correct any OCR errors, " +
                                                    "fix grammar, improve readability, " +
                                                    "and ensure proper formatting. " +
                                                    "Return only the improved text without any explanations or additional content:\n\n{0}";

    private static readonly string[] UnwantedTags = { 
        "script", "style", "noscript", "header", "footer", "nav", 
        "iframe", "meta", "link", "head", "svg", "path", "button" 
    };

    private static readonly string[] RelevantTags = {
        "p", "h1", "h2", "h3", "h4", "h5", "h6", "article", "section",
        "main", "div", "span", "li", "td", "th"
    };

    public DocumentParserService(
        ITextCleaningService textCleaningService, 
        IMCPServerRequester? mCPServerRequester,
        ILogger<DocumentParserService> logger,
        IOptions<ApplicationSettings> appSettings)
    {
        this.textCleaningService = textCleaningService ?? throw new ArgumentNullException(nameof(textCleaningService));
        this.mCPServerRequester = mCPServerRequester;
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.useLLMDocumentParsing = appSettings.Value.LLMDocumentParsing.EnabledLLMDocParsing;
        this.maxChunkSize = appSettings.Value.LLMDocumentParsing.MaxChunkSize;
    }

    public async Task<List<string>> ParseHtml(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            logger.LogWarning("Empty HTML content provided");
            return new List<string>();
        }

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            var structuredContent = new StringBuilder();
            
            // Remove unwanted elements
            RemoveUnwantedTags(doc);

            // Extract content in order of importance
            ExtractHeadings(doc, structuredContent);
            ExtractStructuredElements(doc, structuredContent);
            ExtractMainContent(doc, structuredContent);

            var rawText = structuredContent.ToString();
            if (string.IsNullOrWhiteSpace(rawText))
            {
                logger.LogWarning("No content extracted from HTML");
                return new List<string>();
            }

            var cleanText = textCleaningService.CleanText(rawText);
            return await CreateSemanticChunks(cleanText);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing HTML content");
            throw new DocumentParsingException("Failed to parse HTML content", ex);
        }
    }

    public async Task<List<string>> ParsePdfPerPage(byte[] pdfBytes)
    {
        if (pdfBytes == null || pdfBytes.Length == 0)
        {
            logger.LogWarning("Empty PDF content provided");
            return new List<string>();
        }

        try
        {
            using var pdfDocument = PdfDocument.Open(new MemoryStream(pdfBytes));
            var pages = new List<string>();

            foreach (var page in pdfDocument.GetPages())
            {
                var structuredContent = new StringBuilder();
                ExtractPdfStructuredContent(page, structuredContent);

                var cleanedText = textCleaningService.CleanText(structuredContent.ToString());
                if (!string.IsNullOrWhiteSpace(cleanedText))
                {
                    pages.AddRange(await CreateSemanticChunks(cleanedText));
                }
            }

            return pages;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing PDF content");
            throw new DocumentParsingException("Failed to parse PDF content", ex);
        }
    }

    public string ParsePdf(byte[] pdfBytes)
    {
        var pages = ParsePdfPerPage(pdfBytes).GetAwaiter().GetResult();
        return string.Join(Environment.NewLine + Environment.NewLine, pages);
    }

    private void ExtractHeadings(HtmlDocument doc, StringBuilder sb)
    {
        try
        {
            var headings = doc.DocumentNode.SelectNodes("//h1|//h2|//h3|//h4|//h5|//h6");
            if (headings != null)
            {
                foreach (var heading in headings.OrderBy(h => h.Line))
                {
                    var text = HtmlEntity.DeEntitize(heading.InnerText.Trim());
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        sb.AppendLine(text);
                        sb.AppendLine(); // Add extra line break after headings
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting headings from HTML");
            throw;
        }
    }

    private void ExtractStructuredElements(HtmlDocument doc, StringBuilder sb)
    {
        try
        {
            // Extract tables
            var tables = doc.DocumentNode.SelectNodes("//table");
            if (tables != null)
            {
                foreach (var table in tables)
                {
                    ExtractTableContent(table, sb);
                    sb.AppendLine();
                }
            }

            // Extract lists
            var lists = doc.DocumentNode.SelectNodes("//ul|//ol");
            if (lists != null)
            {
                foreach (var list in lists)
                {
                    ExtractListContent(list, sb);
                    sb.AppendLine();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting structured elements from HTML");
            throw;
        }
    }

    private void ExtractMainContent(HtmlDocument doc, StringBuilder sb)
    {
        try
        {
            var relevantNodes = doc.DocumentNode.SelectNodes($"//*[self::{string.Join(" or self::", RelevantTags)}]");
            if (relevantNodes != null)
            {
                foreach (var node in relevantNodes.OrderBy(n => n.Line))
                {
                    if (node.ParentNode.Name == "table" || node.ParentNode.Name == "ul" || node.ParentNode.Name == "ol")
                        continue; // Skip if already processed as part of table or list

                    var text = HtmlEntity.DeEntitize(node.InnerText.Trim());
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        sb.AppendLine(text);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting main content from HTML");
            throw;
        }
    }

    private static void ExtractTableContent(HtmlNode table, StringBuilder sb)
    {
        var rows = table.SelectNodes(".//tr");
        if (rows == null) return;

        foreach (var row in rows)
        {
            var cells = row.SelectNodes(".//td|.//th");
            if (cells != null)
            {
                var cellTexts = cells
                    .Select(cell => HtmlEntity.DeEntitize(cell.InnerText.Trim()))
                    .Where(text => !string.IsNullOrWhiteSpace(text));

                sb.AppendLine(string.Join(" | ", cellTexts));
            }
        }
    }

    private static void ExtractListContent(HtmlNode list, StringBuilder sb)
    {
        var items = list.SelectNodes(".//li");
        if (items == null) return;

        foreach (var item in items)
        {
            var text = HtmlEntity.DeEntitize(item.InnerText.Trim());
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine($"• {text}");
            }
        }
    }

    private void ExtractPdfStructuredContent(Page page, StringBuilder sb)
    {
        try
        {
            // Group words by their vertical position to identify lines
            var wordsByLine = page.GetWords()
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom))
                .OrderByDescending(g => g.Key);

            // Calculate average word height for the page
            var avgWordHeight = page.GetWords()
                .Average(w => w.BoundingBox.Height);

            foreach (var line in wordsByLine)
            {
                var words = line.OrderBy(w => w.BoundingBox.Left);
                var lineText = string.Join(" ", words.Select(w => w.Text));

                if (!string.IsNullOrWhiteSpace(lineText))
                {
                    // Detect headings based on word height and other characteristics
                    var avgLineHeight = words.Average(w => w.BoundingBox.Height);
                    var isLikelyHeading = IsLikelyHeading(words.ToList(), avgWordHeight);

                    if (isLikelyHeading)
                    {
                        sb.AppendLine();
                        sb.AppendLine(lineText);
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendLine(lineText);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting structured content from PDF");
            throw;
        }
    }

    private static bool IsLikelyHeading(List<Word> words, double avgPageWordHeight)
    {
        if (!words.Any()) return false;

        // Check if the line's words are significantly larger than average
        var lineHeight = words.Average(w => w.BoundingBox.Height);
        if (lineHeight > avgPageWordHeight * 1.2)
            return true;

        // Check if the line is short and starts with common heading patterns
        var text = string.Join(" ", words.Select(w => w.Text));
        if (text.Length < 100 && (
            text.StartsWith("Chapter ", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("Section ", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(text, @"^\d+\.\s") || // Numbered sections
            Regex.IsMatch(text, @"^[IVX]+\.\s") // Roman numerals
        ))
            return true;

        return false;
    }

    private async Task<List<string>> CreateSemanticChunks(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var chunks = new List<string>();
        
        // Split text into smaller chunks first
        for (int i = 0; i < text.Length; i += maxChunkSize)
        {
            var length = Math.Min(maxChunkSize, text.Length - i);
            var chunk = text.Substring(i, length);
            
            // Try to find a good breaking point
            if (i + length < text.Length)
            {
                // Look for the last space or punctuation in the chunk
                var lastSpace = chunk.LastIndexOf(' ');
                var lastPunct = Math.Max(
                    chunk.LastIndexOf('.'),
                    Math.Max(
                        chunk.LastIndexOf('!'),
                        chunk.LastIndexOf('?')
                    )
                );
                var breakPoint = Math.Max(lastSpace, lastPunct);
                
                if (breakPoint > maxChunkSize / 2) // Only break if we find a good point
                {
                    chunk = chunk.Substring(0, breakPoint + 1);
                    i -= (length - (breakPoint + 1)); // Adjust the index to account for the shorter chunk
                }
            }
            
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                await ProcessChunkAsync(chunk, chunks);
            }
        }

        return chunks;
    }

    private async Task ProcessChunkAsync(string chunk, List<string> processedChunks)
    {
        if (chunk.Length > maxChunkSize)
        {
            // Split oversized chunks
            for (int i = 0; i < chunk.Length; i += maxChunkSize)
            {
                var length = Math.Min(maxChunkSize, chunk.Length - i);
                var subChunk = chunk.Substring(i, length);
                
                // Try to find a good breaking point
                if (i + length < chunk.Length)
                {
                    var lastSpace = subChunk.LastIndexOf(' ');
                    var lastPunct = Math.Max(
                        subChunk.LastIndexOf('.'),
                        Math.Max(
                            subChunk.LastIndexOf('!'),
                            subChunk.LastIndexOf('?')
                        )
                    );
                    var breakPoint = Math.Max(lastSpace, lastPunct);
                    
                    if (breakPoint > maxChunkSize / 2)
                    {
                        subChunk = subChunk.Substring(0, breakPoint + 1);
                        i -= (length - (breakPoint + 1));
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(subChunk))
                {
                    await ProcessSubChunkAsync(subChunk, processedChunks);
                }
            }
        }
        else
        {
            await ProcessSubChunkAsync(chunk, processedChunks);
        }
    }

    private async Task ProcessSubChunkAsync(string subChunk, List<string> processedChunks)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                // Ensure the chunk doesn't exceed the maximum size
                if (subChunk.Length > maxChunkSize)
                {
                    logger.LogWarning("Chunk exceeds maximum size, truncating from {OriginalLength} to {MaxLength} characters", 
                        subChunk.Length, maxChunkSize);
                    subChunk = subChunk.Substring(0, maxChunkSize);
                }

                var processedText = await ProcessTextWithMcp(subChunk);
                processedChunks.Add(processedText);
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing chunk (attempt {Attempt} of {MaxRetries})", attempt, MaxRetries);
                if (attempt == MaxRetries)
                    throw;
            }
        }
    }

    private async Task<string> ProcessTextWithMcp(string text)
    {
        if (!useLLMDocumentParsing || mCPServerRequester == null)
        {
            logger.LogWarning("LLM document parsing is not configured or not available. Using direct text processing.");
            return text;
        }

        try
        {
            var prompt = string.Format(ProcessPromptTemplate, text);
            var result = await mCPServerRequester.RequestAsync(prompt: prompt);
            
            if (result.Succeeded && !string.IsNullOrWhiteSpace(result.Data))
            {
                return CleanLLMResponse(result.Data);
            }
            
            logger.LogWarning("LLM document parsing request failed. Using original text.");
            return text;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing text with LLM document parsing");
            return text;
        }
    }

    private string CleanLLMResponse(string response)
    {
        // Remove common LLM prefixes/suffixes
        var cleaned = response.Trim();
        
        // Remove any markdown code block markers
        cleaned = cleaned.Replace("```", "").Trim();

        // Remove any explanatory prefixes
        var commonPrefixes = new[] { 
            "Here's the improved text:", 
            "The improved text is:", 
            "Here's the fixed text:", 
            "Fixed text:",
            "Improved text:",
            "Here's the corrected text:",
            "Corrected text:"
        };
        
        foreach (var prefix in commonPrefixes)
        {
            if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(prefix.Length).TrimStart();
            }
        }

        return cleaned;
    }

    private static void RemoveUnwantedTags(HtmlDocument doc)
    {
        var unwantedNodes = doc.DocumentNode.SelectNodes($"//{string.Join("|//", UnwantedTags)}");
        if (unwantedNodes != null)
        {
            foreach (var node in unwantedNodes)
            {
                node.Remove();
            }
        }
    }
}

public class DocumentParsingException : Exception
{
    public DocumentParsingException(string message) : base(message) { }
    public DocumentParsingException(string message, Exception innerException) : base(message, innerException) { }
}