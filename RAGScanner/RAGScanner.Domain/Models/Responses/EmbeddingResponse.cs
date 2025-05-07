using Newtonsoft.Json;

public class EmbeddingResponse
{
    [JsonProperty("data")]
    public List<EmbeddingData> Data { get; set; } = new();
}
