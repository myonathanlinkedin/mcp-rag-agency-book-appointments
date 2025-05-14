using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

public class RedisChatMessageStore : IChatMessageStore
{
    private readonly IDistributedCache cache;
    private readonly string cacheKeyPrefix;
    private readonly DistributedCacheEntryOptions cacheOptions;

    public RedisChatMessageStore(
        IDistributedCache cache,
        ApplicationSettings appSettings)
    {
        this.cache = cache;
        this.cacheKeyPrefix = appSettings.Redis.InstanceName;
        this.cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(appSettings.Redis.CacheChatExpirationDays)
        };
    }

    public List<ChatMessage> GetMessages(string sessionId)
    {
        var cacheKey = $"{cacheKeyPrefix}chat_{sessionId}";
        var cachedMessages = cache.GetString(cacheKey);

        if (string.IsNullOrEmpty(cachedMessages))
        {
            return new List<ChatMessage>();
        }

        return JsonSerializer.Deserialize<List<ChatMessage>>(cachedMessages) ?? new List<ChatMessage>();
    }

    public void SaveMessages(string sessionId, List<ChatMessage> messages)
    {
        if (messages == null)
        {
            return;
        }

        var cacheKey = $"{cacheKeyPrefix}chat_{sessionId}";
        var serializedMessages = JsonSerializer.Serialize(messages);
        cache.SetString(cacheKey, serializedMessages, cacheOptions);
    }
} 