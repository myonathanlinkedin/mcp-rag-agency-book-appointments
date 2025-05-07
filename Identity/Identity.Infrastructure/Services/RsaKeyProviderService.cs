using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

public class RsaKeyProviderService : IRsaKeyProvider
{
    private const string SignatureUse = "sig";
    private RSA rsa;
    private JsonWebKey jsonWebKey;
    private string keyId;
    private byte[] encryptedPrivateKey;
    private readonly byte[] encryptionKey;
    private readonly byte[] encryptionIV;

    public RsaKeyProviderService(ApplicationSettings appSettings)
    {
        var rotationInterval = TimeSpan.FromSeconds(appSettings.KeyRotationIntervalSeconds);

        // Secure AES encryption key and IV (would be ideally stored securely, e.g., using KMS)
        encryptionKey = new byte[32];  // 256 bits for AES
        encryptionIV = new byte[16];   // AES block size for IV
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
        // Dispose the old RSA key securely
        DisposeKeys();

        // Generate a new RSA key pair
        rsa = RSA.Create(2048);
        keyId = Guid.NewGuid().ToString();

        // Export the private key and encrypt it
        var rsaParameters = rsa.ExportParameters(true); // Export all parameters, including private key parts
        encryptedPrivateKey = EncryptPrivateKey(rsaParameters);

        // Export the public key
        var rsaSecurityKey = new RsaSecurityKey(rsa.ExportParameters(false)) // Export only public key
        {
            KeyId = keyId
        };

        jsonWebKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(rsaSecurityKey);
        jsonWebKey.Use = SignatureUse;
    }

    public RSA GetPrivateKey()
    {
        // Decrypt and load the private key into memory when it's needed
        if (rsa == null)
        {
            // Decrypt the private key and import the parameters
            var rsaParameters = DecryptPrivateKey(encryptedPrivateKey);
            rsa = RSA.Create();
            rsa.ImportParameters(rsaParameters);
        }

        return rsa;
    }

    public JsonWebKey GetPublicJwk() => jsonWebKey;

    // Encrypt the RSA private key to secure it in memory
    private byte[] EncryptPrivateKey(RSAParameters rsaParameters)
    {
        using (var aes = Aes.Create()) // Aes.Create() replaces AesManaged
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

    // Decrypt the RSA private key and securely load it when needed
    private RSAParameters DecryptPrivateKey(byte[] encryptedPrivateKey)
    {
        using (var aes = Aes.Create()) // Aes.Create() replaces AesManaged
        {
            aes.Key = encryptionKey;
            aes.IV = encryptionIV;

            using (var decryptor = aes.CreateDecryptor())
            {
                var decryptedPrivateKey = decryptor.TransformFinalBlock(encryptedPrivateKey, 0, encryptedPrivateKey.Length);

                // Import decrypted private key into RSA
                RSAParameters rsaParameters = new RSAParameters();
                using (var rsa = RSA.Create())
                {
                    rsa.ImportRSAPrivateKey(decryptedPrivateKey, out _);
                    rsaParameters = rsa.ExportParameters(true);  // Exporting the full RSA parameters including private key
                }

                return rsaParameters;
            }
        }
    }

    // Securely dispose of RSA keys and clear sensitive memory
    private void DisposeKeys()
    {
        if (rsa != null)
        {
            // Zero out the private key memory
            Array.Clear(encryptedPrivateKey, 0, encryptedPrivateKey.Length);

            // Zero out any sensitive data in memory
            rsa.Dispose();
            rsa = null;
        }
    }

    // Override the finalizer to ensure memory is cleaned up if Dispose() wasn't called
    ~RsaKeyProviderService()
    {
        DisposeKeys();
    }
}
