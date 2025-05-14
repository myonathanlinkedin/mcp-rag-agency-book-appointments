using HtmlAgilityPack;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text.RegularExpressions;

public class DocumentParserService : IDocumentParserService
{
    private static readonly string[] UnwantedTags = { 
        "script", "style", "noscript", "header", "footer", "nav", 
        "iframe", "meta", "link", "head", "svg", "path", "button" 
    };

    private static readonly string[] RelevantTags = {
        "p", "h1", "h2", "h3", "h4", "h5", "h6", "article", "section",
        "main", "div", "span", "li", "td", "th"
    };

    private readonly ITextCleaningService textCleaningService;
    private const int MaxChunkSize = 1000; // Maximum characters per chunk
    private const int ChunkOverlap = 200;  // Overlap between chunks
    private static readonly Regex TableRegex = new(@"<table[^>]*>.*?</table>", RegexOptions.Singleline);
    private static readonly Regex ListRegex = new(@"<[uo]l[^>]*>.*?</[uo]l>", RegexOptions.Singleline);

    public DocumentParserService(ITextCleaningService textCleaningService)
    {
        this.textCleaningService = textCleaningService;
    }

    public string ParseHtml(string htmlContent)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        // Remove unwanted elements
        RemoveUnwantedTags(doc);

        // Extract structured content
        var structuredContent = new StringBuilder();

        // 1. Extract title and headings first (they're more important)
        ExtractHeadings(doc, structuredContent);

        // 2. Extract tables and lists (preserve structure)
        ExtractStructuredElements(doc, structuredContent);

        // 3. Extract main content
        ExtractMainContent(doc, structuredContent);

        var rawText = structuredContent.ToString();
        return textCleaningService.CleanText(rawText);
    }

    public List<string> ParsePdfPerPage(byte[] pdfBytes)
    {
        using var pdfDocument = PdfDocument.Open(new MemoryStream(pdfBytes));
        var pages = new List<string>();

        foreach (var page in pdfDocument.GetPages())
        {
            var structuredContent = new StringBuilder();

            // Extract text while preserving structure
            ExtractPdfStructuredContent(page, structuredContent);

            var cleanedText = textCleaningService.CleanText(structuredContent.ToString());
            if (!string.IsNullOrWhiteSpace(cleanedText))
            {
                // Split into semantic chunks for better RAG performance
                pages.AddRange(CreateSemanticChunks(cleanedText));
            }
        }

        return pages;
    }

    public string ParsePdf(byte[] pdfBytes) => 
        string.Join(Environment.NewLine + Environment.NewLine, ParsePdfPerPage(pdfBytes));

    private void ExtractHeadings(HtmlDocument doc, StringBuilder sb)
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

    private void ExtractStructuredElements(HtmlDocument doc, StringBuilder sb)
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

    private void ExtractMainContent(HtmlDocument doc, StringBuilder sb)
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

    private static List<string> CreateSemanticChunks(string text)
    {
        var chunks = new List<string>();
        var sentences = text.Split(new[] { ". ", "! ", "? ", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new StringBuilder();
        
        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > MaxChunkSize)
            {
                // If the chunk would exceed max size, start a new one
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    
                    // Start new chunk with overlap from previous
                    var lastChunk = currentChunk.ToString();
                    currentChunk.Clear();
                    if (lastChunk.Length > ChunkOverlap)
                    {
                        var overlapText = lastChunk.Substring(Math.Max(0, lastChunk.Length - ChunkOverlap));
                        currentChunk.Append(overlapText);
                    }
                }
            }
            
            currentChunk.AppendLine(sentence.Trim() + ".");
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
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