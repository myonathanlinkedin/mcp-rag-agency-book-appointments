using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

public static class RedisCacheExtensions
{
    public static async Task ClearRedisCache(this IDistributedCache cache, string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return;
        }

        try
        {
            var redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
            var endpoints = redis.GetEndPoints();
            
            foreach (var endpoint in endpoints)
            {
                var server = redis.GetServer(endpoint);
                await server.FlushDatabaseAsync();
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw to prevent application startup failure
            Console.WriteLine($"Error clearing Redis cache: {ex.Message}");
        }
    }
} 