using Microsoft.IdentityModel.Tokens;

public interface IJwtGeneratorService
{
    Task<(string AccessToken, string RefreshToken)> GenerateToken(User user);
    JsonWebKey GetPublicKey();
    Task<string> RefreshToken(string userId, string providedRefreshToken);
}