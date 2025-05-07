using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

public interface IRsaKeyProvider
{
    RSA GetPrivateKey();
    JsonWebKey GetPublicJwk();
}
