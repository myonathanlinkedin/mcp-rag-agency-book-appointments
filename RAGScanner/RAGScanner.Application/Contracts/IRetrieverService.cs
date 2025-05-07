public interface IRetrieverService
{
    Task<List<DocumentVector>> RetrieveAllDocumentsAsync(CancellationToken cancellationToken);
    Task<List<DocumentVector>> RetrieveDocumentsByQueryAsync(string queryText, CancellationToken cancellationToken);
}
