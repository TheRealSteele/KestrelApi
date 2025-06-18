namespace KestrelApi.Secrets;

public interface ISecretsService
{
    Task<string> AddSecretAsync(string userId, string secret);
    Task<IEnumerable<string>> GetSecretsAsync(string userId);
}