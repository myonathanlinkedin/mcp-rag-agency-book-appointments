using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

public class RsaKeyProviderService : IRsaKeyProvider
{
    private const string SignatureUse = "sig";
    private RSA? rsa;
    private JsonWebKey jsonWebKey = null!;
    private string keyId = null!;
    private byte[] encryptedPrivateKey = null!;
    private readonly byte[] encryptionKey;
    private readonly byte[] encryptionIV;

    public RsaKeyProviderService(ApplicationSettings appSettings)
    {
        var rotationInterval = TimeSpan.FromSeconds(appSettings.KeyRotationIntervalSeconds);

        // Secure AES encryption key and IV (would be ideally stored securely, e.g., using KMS)
        this.encryptionKey = new byte[32];  // 256 bits for AES
        this.encryptionIV = new byte[16];   // AES block size for IV
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(encryptionKey);
            rng.GetBytes(encryptionIV);
        }

        GenerateKeys();

        new Timer(_ => GenerateKeys(), null, rotationInterval, rotationInterval);
    }

    private void GenerateKeys()
    {
        DisposeKeys();

        rsa = RSA.Create(2048);
        keyId = Guid.NewGuid().ToString();

        var rsaParameters = rsa.ExportParameters(true);
        encryptedPrivateKey = EncryptPrivateKey(rsaParameters);

        var rsaSecurityKey = new RsaSecurityKey(rsa.ExportParameters(false))
        {
            KeyId = keyId
        };

        jsonWebKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(rsaSecurityKey);
        jsonWebKey.Use = SignatureUse;
    }

    public RSA GetPrivateKey()
    {
        if (rsa == null)
        {
            var rsaParameters = DecryptPrivateKey(encryptedPrivateKey);
            rsa = RSA.Create();
            rsa.ImportParameters(rsaParameters);
        }

        return rsa;
    }

    public JsonWebKey GetPublicJwk() => jsonWebKey;

    private byte[] EncryptPrivateKey(RSAParameters rsaParameters)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = encryptionKey;
            aes.IV = encryptionIV;

            using (var encryptor = aes.CreateEncryptor())
            {
                var privateKeyBytes = rsa.ExportRSAPrivateKey();
                return encryptor.TransformFinalBlock(privateKeyBytes, 0, privateKeyBytes.Length);
            }
        }
    }

    private RSAParameters DecryptPrivateKey(byte[] encryptedPrivateKey)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = encryptionKey;
            aes.IV = encryptionIV;

            using (var decryptor = aes.CreateDecryptor())
            {
                var decryptedPrivateKey = decryptor.TransformFinalBlock(encryptedPrivateKey, 0, encryptedPrivateKey.Length);

                RSAParameters rsaParameters = new RSAParameters();
                using (var rsa = RSA.Create())
                {
                    rsa.ImportRSAPrivateKey(decryptedPrivateKey, out _);
                    rsaParameters = rsa.ExportParameters(true);
                }

                return rsaParameters;
            }
        }
    }

    private void DisposeKeys()
    {
        if (rsa != null)
        {
            Array.Clear(encryptedPrivateKey, 0, encryptedPrivateKey.Length);
            rsa.Dispose();
            rsa = null;
        }
    }

    ~RsaKeyProviderService()
    {
        DisposeKeys();
    }
}
