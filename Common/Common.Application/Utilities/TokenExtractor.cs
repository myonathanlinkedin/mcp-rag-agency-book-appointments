using System.Text.Json;

public static class TokenExtractor
{
    public static string ExtractTokenFromResponse(string responseBody)
    {
        var jsonResponse = JsonDocument.Parse(responseBody);
        return jsonResponse.RootElement.GetProperty("accessToken").GetString();
    }
}
