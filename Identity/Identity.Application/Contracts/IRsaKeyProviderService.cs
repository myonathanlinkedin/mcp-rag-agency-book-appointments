using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

public interface IRsaKeyProviderService
{
    RSA GetPrivateKey();
    JsonWebKey GetPublicJwk();
}
