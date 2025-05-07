using Newtonsoft.Json;

public class EmbeddingData
{
    [JsonProperty("embedding")]
    public float[] Embedding { get; set; } = default!;
}