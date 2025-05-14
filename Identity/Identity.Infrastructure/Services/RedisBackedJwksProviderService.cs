using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

public class RedisBackedJwksProviderService : IJwksProviderService
{
    private readonly IDistributedCache cache;
    private readonly IRsaKeyProviderService rsaKeyProvider;
    private readonly string cacheKeyPrefix;
    private readonly DistributedCacheEntryOptions cacheOptions;

    private const string JwksCacheKey = "jwks_key";

    public RedisBackedJwksProviderService(
        IDistributedCache cache,
        IRsaKeyProviderService rsaKeyProvider,
        ApplicationSettings appSettings)
    {
        this.cache = cache;
        this.rsaKeyProvider = rsaKeyProvider;
        this.cacheKeyPrefix = appSettings.Redis.InstanceName;
        this.cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(appSettings.KeyRotationIntervalSeconds)
        };
    }

    public async Task<JsonWebKey> GetPublicKeyAsync()
    {
        var cacheKey = $"{cacheKeyPrefix}{JwksCacheKey}";
        var cachedJwk = await cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedJwk))
        {
            return JsonSerializer.Deserialize<JsonWebKey>(cachedJwk);
        }

        var jwk = rsaKeyProvider.GetPublicJwk();
        var serializedJwk = JsonSerializer.Serialize(jwk);
        
        await cache.SetStringAsync(cacheKey, serializedJwk, cacheOptions);
        
        return jwk;
    }
}