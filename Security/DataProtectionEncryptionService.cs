using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace KestrelApi.Security;

public class DataProtectionEncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;
    private readonly ILogger<DataProtectionEncryptionService> _logger;

    public DataProtectionEncryptionService(
        IDataProtectionProvider provider,
        ILogger<DataProtectionEncryptionService> logger)
    {
        ArgumentNullException.ThrowIfNull(provider);
        
        _protector = provider.CreateProtector("SecretProtection");
        _logger = logger;
    }

    public Task<string> EncryptAsync(string plainText)
    {
        try
        {
            _logger.LogDebug("Encrypting data");
            var encrypted = _protector.Protect(plainText);
            return Task.FromResult(encrypted);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Null argument provided for encryption");
            throw;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error during encryption");
            throw;
        }
    }
    
    public Task<string> DecryptAsync(string cipherText)
    {
        try
        {
            _logger.LogDebug("Decrypting data");
            var decrypted = _protector.Unprotect(cipherText);
            return Task.FromResult(decrypted);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Null argument provided for decryption");
            throw;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error during decryption - data may be corrupted or tampered");
            throw;
        }
    }
}