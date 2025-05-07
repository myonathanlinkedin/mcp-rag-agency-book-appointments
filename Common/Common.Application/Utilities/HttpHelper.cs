using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

public static class HttpHelper
{
    public static async Task<HttpResponseMessage> GetAsync(HttpClient client, string url)
    {
        // Clear any existing headers first (optional, depends on usage)
        client.DefaultRequestHeaders.Clear();

        // Set realistic Chrome user-agent and other headers
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/123.0.0.0 Safari/537.36");

        client.DefaultRequestHeaders.Accept.ParseAdd("application/pdf, application/xhtml+xml, text/html;q=0.9, */*;q=0.8");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        client.DefaultRequestHeaders.Connection.Add("keep-alive");

        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        return response;
    }
}
