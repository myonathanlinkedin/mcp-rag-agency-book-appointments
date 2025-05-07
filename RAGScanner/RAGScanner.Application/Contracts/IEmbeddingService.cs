using System.Threading.Tasks;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string input, CancellationToken cancellationToken);
}
