public interface IDocumentParserService
{
    Task<List<string>> ParseHtml(string htmlContent);
    string ParsePdf(byte[] pdfBytes);
    Task<List<string>> ParsePdfPerPage(byte[] pdfBytes);
}