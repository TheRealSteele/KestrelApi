using System.Security.Cryptography;
using KestrelApi.Security;

namespace KestrelApi.Secrets;

public class SecretsService : ISecretsService
{
    private readonly ISecretsRepository _repository;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<SecretsService> _logger;

    public SecretsService(
        ISecretsRepository repository,
        IEncryptionService encryptionService,
        ILogger<SecretsService> logger)
    {
        _repository = repository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<string> AddSecretAsync(string userId, string secret)
    {
        try
        {
            _logger.LogInformation("Adding secret for user {UserId}", userId);
            var encrypted = await _encryptionService.EncryptAsync(secret).ConfigureAwait(false);
            var result = await _repository.AddAsync(userId, encrypted).ConfigureAwait(false);
            _logger.LogInformation("Secret added successfully for user {UserId}", userId);
            return result;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Null argument provided when adding secret for user {UserId}", userId);
            throw;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Encryption failed when adding secret for user {UserId}", userId);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when adding secret for user {UserId}", userId);
            throw;
        }
    }
    
    public async Task<IEnumerable<string>> GetSecretsAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Retrieving secrets for user {UserId}", userId);
            var encryptedSecrets = await _repository.GetByUserIdAsync(userId).ConfigureAwait(false);
            
            var decryptedSecrets = new List<string>();
            foreach (var encryptedSecret in encryptedSecrets)
            {
                var decrypted = await _encryptionService.DecryptAsync(encryptedSecret).ConfigureAwait(false);
                decryptedSecrets.Add(decrypted);
            }
            
            _logger.LogInformation("Retrieved {Count} secrets for user {UserId}", decryptedSecrets.Count, userId);
            return decryptedSecrets;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Null argument provided when retrieving secrets for user {UserId}", userId);
            throw;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Decryption failed when retrieving secrets for user {UserId}", userId);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when retrieving secrets for user {UserId}", userId);
            throw;
        }
    }
}