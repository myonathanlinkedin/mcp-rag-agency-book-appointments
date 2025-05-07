public interface IDocumentParserService
{
    string ParseHtml(string htmlContent);
    string ParsePdf(byte[] pdfBytes);
    List<string> ParsePdfPerPage(byte[] pdfBytes);
}