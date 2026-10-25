using System.Security.Cryptography;
using System.Text;

namespace VianaHub.Global.Gerit.Domain.Tools.Cryptography;

/// <summary>
/// Helper para geração e manipulação de chaves RSA para JWT
/// </summary>
public static class CryptoRSA
{
    /// <summary>
    /// Gera um par de chaves RSA (pública e privada)
    /// </summary>
    /// <param name="keySize">Tamanho da chave em bits (2048, 3072 ou 4096)</param>
    /// <returns>Tuple com a chave pública e privada no formato PEM</returns>
    public static (string PublicKeyPem, string PrivateKeyPem) GenerateKeyPair(int keySize = 2048)
    {
        using var rsa = RSA.Create(keySize);

        // Exportar chave pública
        var publicKey = rsa.ExportSubjectPublicKeyInfo();
        var publicKeyPem = ConvertToPem(publicKey, "PUBLIC KEY");

        // Exportar chave privada
        var privateKey = rsa.ExportPkcs8PrivateKey();
        var privateKeyPem = ConvertToPem(privateKey, "PRIVATE KEY");

        return (publicKeyPem, privateKeyPem);
    }

    /// <summary>
    /// Criptografa a chave privada usando AES
    /// </summary>
    /// <param name="privateKeyPem">Chave privada em formato PEM</param>
    /// <param name="encryptionKey">Chave de criptografia (mínimo 32 bytes para AES-256)</param>
    /// <returns>Chave privada criptografada em Base64</returns>
    public static string EncryptPrivateKey(string privateKeyPem, string encryptionKey)
    {
        if (string.IsNullOrWhiteSpace(privateKeyPem))
            throw new ArgumentException("Private key cannot be null or empty", nameof(privateKeyPem));

        if (string.IsNullOrWhiteSpace(encryptionKey))
            throw new ArgumentException("Encryption key cannot be null or empty", nameof(encryptionKey));

        var keyBytes = Encoding.UTF8.GetBytes(encryptionKey);

        // Garantir que a chave tem 32 bytes (256 bits)
        var key = new byte[32];
        Array.Copy(keyBytes, key, Math.Min(keyBytes.Length, 32));

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = key;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(privateKeyPem);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Combinar IV + dados criptografados
        var combined = new byte[aes.IV.Length + encryptedBytes.Length];
        Array.Copy(aes.IV, 0, combined, 0, aes.IV.Length);
        Array.Copy(encryptedBytes, 0, combined, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(combined);
    }

    /// <summary>
    /// Descriptografa a chave privada
    /// </summary>
    /// <param name="encryptedPrivateKey">Chave privada criptografada em Base64</param>
    /// <param name="encryptionKey">Chave de criptografia</param>
    /// <returns>Chave privada em formato PEM</returns>
    public static string DecryptPrivateKey(string? encryptedPrivateKey, string encryptionKey)
    {
        if (string.IsNullOrWhiteSpace(encryptedPrivateKey))
            throw new ArgumentException("Encrypted private key cannot be null or empty", nameof(encryptedPrivateKey));

        if (string.IsNullOrWhiteSpace(encryptionKey))
            throw new ArgumentException("Encryption key cannot be null or empty", nameof(encryptionKey));

        var keyBytes = Encoding.UTF8.GetBytes(encryptionKey);

        // Garantir que a chave tem 32 bytes (256 bits)
        var key = new byte[32];
        Array.Copy(keyBytes, key, Math.Min(keyBytes.Length, 32));

        var combined = Convert.FromBase64String(encryptedPrivateKey);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Extrair IV (primeiros 16 bytes)
        var iv = new byte[16];
        Array.Copy(combined, 0, iv, 0, 16);
        aes.IV = iv;

        // Extrair dados criptografados
        var encryptedBytes = new byte[combined.Length - 16];
        Array.Copy(combined, 16, encryptedBytes, 0, encryptedBytes.Length);

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    /// <summary>
    /// Converte bytes para formato PEM
    /// </summary>
    private static string ConvertToPem(byte[] keyBytes, string label)
    {
        var base64 = Convert.ToBase64String(keyBytes);
        var sb = new StringBuilder();

        sb.AppendLine($"-----BEGIN {label}-----");

        // Quebrar em linhas de 64 caracteres
        for (int i = 0; i < base64.Length; i += 64)
        {
            var length = Math.Min(64, base64.Length - i);
            sb.AppendLine(base64.Substring(i, length));
        }

        sb.AppendLine($"-----END {label}-----");

        return sb.ToString();
    }

    /// <summary>
    /// Extrai os bytes do formato PEM
    /// </summary>
    public static byte[] ExtractBytesFromPem(string? pem, string label)
    {
        ArgumentNullException.ThrowIfNull(pem);
        var header = $"-----BEGIN {label}-----";
        var footer = $"-----END {label}-----";

        var start = pem.IndexOf(header);
        var end = pem.IndexOf(footer);

        if (start < 0 || end < 0)
            throw new ArgumentException($"Invalid PEM format for {label}");

        var base64 = pem.Substring(start + header.Length, end - start - header.Length)
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace(" ", "");

        return Convert.FromBase64String(base64);
    }

    /// <summary>
    /// Valida se a chave está no formato PEM correto
    /// </summary>
    public static bool IsValidPem(string pem, string label)
    {
        try
        {
            ExtractBytesFromPem(pem, label);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
