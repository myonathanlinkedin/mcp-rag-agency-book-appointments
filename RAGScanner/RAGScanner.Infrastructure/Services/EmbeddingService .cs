using Newtonsoft.Json;
using System.Text;

public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient httpClient;
    private readonly string embeddingModel;
    private readonly string endpoint;

    public EmbeddingService(HttpClient httpClient, ApplicationSettings applicationSettings)
    {
        this.httpClient = httpClient;
        embeddingModel = applicationSettings.Api.EmbeddingModel;
        endpoint = $"{applicationSettings.Api.Endpoint}/embeddings";
    }

    public async Task<float[]> GenerateEmbeddingAsync(string input, CancellationToken cancellationToken)
    {
        var requestBody = new { model = embeddingModel, input };
        var contentRequest = CreateJsonContent(requestBody);

        var response = await httpClient.PostAsync(endpoint, contentRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Embedding request failed. Status: {response.StatusCode}. Response: {responseBody}");

        return ParseEmbeddingResponse(responseBody);
    }

    private static StringContent CreateJsonContent(object requestBody) =>
        new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

    private static float[] ParseEmbeddingResponse(string responseBody)
    {
        var embeddingResponse = JsonConvert.DeserializeObject<EmbeddingResponse>(responseBody)
            ?? throw new JsonException("Failed to deserialize embedding response.");

        return embeddingResponse.Data?.FirstOrDefault()?.Embedding
            ?? throw new InvalidOperationException("Embedding response contains no data.");
    }
}