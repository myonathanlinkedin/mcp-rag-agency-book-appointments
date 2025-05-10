using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Caching.Distributed;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

public class JwtGeneratorService : IJwtGenerator
{
    private readonly UserManager<User> userManager;
    private readonly IRsaKeyProvider keyProvider;
    private readonly IDistributedCache distributedCache;
    private readonly string audience;
    private readonly string issuer;
    private readonly int tokenExpirationSeconds;

    private const string RsaAlgorithm = SecurityAlgorithms.RsaSha256Signature;

    public JwtGeneratorService(
        UserManager<User> userManager,
        ApplicationSettings appSettings,
        IRsaKeyProvider keyProvider,
        IDistributedCache distributedCache)
    {
        this.userManager = userManager;
        this.keyProvider = keyProvider;
        this.distributedCache = distributedCache;
        this.audience = appSettings.Audience;
        this.issuer = appSettings.Issuer;
        this.tokenExpirationSeconds = appSettings.TokenExpirationSeconds;
    }

    public async Task<(string AccessToken, string RefreshToken)> GenerateToken(User user)
    {
        var rsa = keyProvider.GetPrivateKey();
        var tokenHandler = new JwtSecurityTokenHandler();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString())
        };

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddSeconds(tokenExpirationSeconds),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), RsaAlgorithm)
        };

        var accessToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

        var tokenExpiry = tokenExpirationSeconds * 1.5;
        var refreshTokenExpiry = DateTime.UtcNow.AddSeconds((int)tokenExpiry);
        var refreshToken = GenerateRefreshToken();

        var refreshTokenData = new RefreshTokenModel { RefreshToken = refreshToken, Expiry = refreshTokenExpiry };
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(tokenExpiry)
        };

        var cacheKey = GetRefreshTokenCacheKey(user.Id);
        var json = JsonSerializer.Serialize(refreshTokenData);
        await distributedCache.SetStringAsync(cacheKey, json, cacheOptions);

        return (accessToken, refreshToken);
    }

    public async Task<string> RefreshToken(string userId, string providedRefreshToken)
    {
        var cacheKey = GetRefreshTokenCacheKey(userId);
        var cachedTokenJson = await distributedCache.GetStringAsync(cacheKey);
        if (string.IsNullOrEmpty(cachedTokenJson))
        {
            throw new SecurityTokenException("Invalid or expired refresh token.");
        }

        var storedToken = JsonSerializer.Deserialize<RefreshTokenModel>(cachedTokenJson);
        if (storedToken == null || storedToken.RefreshToken != providedRefreshToken || storedToken.Expiry < DateTime.UtcNow)
        {
            throw new SecurityTokenException("Invalid or expired refresh token.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new SecurityTokenException("User not found.");
        }

        return (await GenerateToken(user)).AccessToken;
    }

    public JsonWebKey GetPublicKey() => keyProvider.GetPublicJwk();

    private static string GetRefreshTokenCacheKey(string userId) => $"refresh_token:{userId}";

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
