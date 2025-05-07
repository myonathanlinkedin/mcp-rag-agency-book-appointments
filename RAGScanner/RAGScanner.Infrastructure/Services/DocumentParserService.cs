using HtmlAgilityPack;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

public class DocumentParserService : IDocumentParserService
{
    private static readonly string[] UnwantedTags = { "script", "style", "noscript" };

    public string ParseHtml(string htmlContent)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);
        RemoveUnwantedTags(doc);

        return ExtractVisibleText(doc);
    }

    public List<string> ParsePdfPerPage(byte[] pdfBytes) =>
        PdfDocument.Open(new MemoryStream(pdfBytes))
            .GetPages()
            .Select(ParsePageText)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

    public string ParsePdf(byte[] pdfBytes) => string.Join(Environment.NewLine, ParsePdfPerPage(pdfBytes));

    private static void RemoveUnwantedTags(HtmlDocument doc)
    {
        foreach (var node in doc.DocumentNode.SelectNodes($"//{string.Join("|//", UnwantedTags)}") ?? Enumerable.Empty<HtmlNode>())
        {
            node.Remove();
        }
    }

    private static string ExtractVisibleText(HtmlDocument doc) =>
        doc.DocumentNode.SelectNodes("//body//text()")?
            .Select(node => HtmlEntity.DeEntitize(node.InnerText.Trim()))
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Aggregate(new StringBuilder(), (sb, text) => sb.AppendLine(text), sb => sb.ToString())
            ?? string.Empty;

    private static string ParsePageText(Page page) =>
        string.Join(" ", page.GetWords().Select(word => word.Text)).Trim();
}