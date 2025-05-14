using Microsoft.IdentityModel.Tokens;

public interface IJwksProviderService
{
    Task<JsonWebKey> GetPublicKeyAsync();
}