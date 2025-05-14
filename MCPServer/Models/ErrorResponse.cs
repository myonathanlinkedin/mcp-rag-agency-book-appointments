using System.Text.Json.Serialization;

public class ErrorResponse
{
    [JsonPropertyName("errors")]
    public string[] Errors { get; set; } = Array.Empty<string>();
} 