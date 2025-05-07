public interface IVectorStoreService
{
    Task SaveDocumentAsync(DocumentVector documentVector, int vectorSize);
}
