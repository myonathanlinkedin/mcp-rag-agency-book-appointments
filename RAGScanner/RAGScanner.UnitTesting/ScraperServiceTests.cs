using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using Xunit;

public class ScraperServiceTests
{
    private readonly Mock<IHttpClientFactory> mockHttpClientFactory;
    private readonly Mock<ILogger<ScraperService>> mockLogger;
    private readonly ScraperService scraperService;
    private readonly Mock<HttpMessageHandler> mockHttpMessageHandler;
    private readonly HttpClient httpClient;

    public ScraperServiceTests()
    {
        this.mockHttpClientFactory = new Mock<IHttpClientFactory>();
        this.mockLogger = new Mock<ILogger<ScraperService>>();
        this.mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        this.httpClient = new HttpClient(this.mockHttpMessageHandler.Object);

        this.mockHttpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(this.httpClient);

        this.scraperService = new ScraperService(
            this.mockHttpClientFactory.Object,
            this.mockLogger.Object
        );
    }

    [Fact]
    public async Task ScrapeUrlsAsync_ShouldReturnDocuments_WhenUrlsAreValid()
    {
        // Arrange
        var urls = new List<string> { "https://example.com" };
        var htmlContent = "<html><body><h1>Test Content</h1><p>This is a test page.</p></body></html>";

        var successResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(htmlContent, Encoding.UTF8, "text/html")
        };

        this.mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(successResponse);

        // Act
        var result = await this.scraperService.ScrapeUrlsAsync(urls);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].Url.Should().Be("https://example.com");
        result[0].ContentText.Should().Contain("Test Content");
    }

    [Fact]
    public async Task ScrapeUrlsAsync_ShouldHandleErrors_WhenUrlIsInvalid()
    {
        // Arrange
        var urls = new List<string> { "https://invalid-url.com" };

        this.mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Invalid URL"));

        // Act
        var result = await this.scraperService.ScrapeUrlsAsync(urls);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(0);

        // Verify that the logger was called with an error
        this.mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to scrape URL")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ScrapeUrlsAsync_ShouldHandlePdfContent()
    {
        // Arrange
        var urls = new List<string> { "https://example.com/document.pdf" };
        var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic number

        var successResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new ByteArrayContent(pdfContent)
        };

        successResponse.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        this.mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(successResponse);

        // Act
        var result = await this.scraperService.ScrapeUrlsAsync(urls);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].IsPdf.Should().BeTrue();
        result[0].ContentBytes.Should().NotBeNull();
        result[0].ContentText.Should().BeNull();
    }
}
