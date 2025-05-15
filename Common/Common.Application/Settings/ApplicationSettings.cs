public class ApplicationSettings
{
    public string Issuer { get; init; }
    public string Audience { get; init; }
    public int KeyRotationIntervalSeconds { get; init; }
    public int TokenExpirationSeconds { get; init; }
    public int ResetTokenExpirationSeconds { get; init; }
    public RedisSettings Redis { get; init; } = new();
    public ApiSettings Api { get; init; } = new();
    public QdrantSettings Qdrant { get; init; } = new();
    public ConnectionStringsSettings ConnectionStrings { get; init; } = new();
    public LoggingSettings Logging { get; init; } = new();
    public JwtSettings Jwt { get; init; } = new();
    public MCPSettings MCP { get; init; } = new();
    public MailHogSettings MailHog { get; init; } = new();
    public KafkaSettings Kafka { get; init; } = new();
    public string AllowedHosts { get; init; } = "*";

    public record ApiSettings(string ApiKey = "", string Endpoint = "", string LlmModel = "", string EmbeddingModel = "");
    public record QdrantSettings(string Endpoint = "", string CollectionName = "", float SimilarityThreshold = 0f, string ApiKey = "", string CerCertificateThumbprint = "");
    public record ConnectionStringsSettings(string IdentityDBConnection = "", string RAGDBConnection = "", string AgencyBookDBConnection = "", string EventSourcingConnection = "");
    public record LoggingSettings(LogLevelSettings LogLevel = null)
    {
        public LoggingSettings() : this(new LogLevelSettings()) { }
    }
    public record LogLevelSettings(string Default = "Information", string Microsoft = "Warning", string MicrosoftHostingLifetime = "Warning");
    public record JwtSettings(string JwksUrl = "");
    public record MCPSettings(string ServerName = "", string Endpoint = "");
    public record MailHogSettings(string SmtpServer = "", int SmtpPort = 587, string FromAddress = "");
    public record KafkaSettings(string BootstrapServers = "", string GroupId = "", string Topic = "");
    public record RedisSettings(string ConnectionString = "", string InstanceName = "", int CacheChatExpirationMinutes = 1);
}
