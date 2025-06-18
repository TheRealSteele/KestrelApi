namespace KestrelApi.Secrets;

public interface ISecretsRepository
{
    Task<string> AddAsync(string userId, string encryptedSecret);
    Task<IEnumerable<string>> GetByUserIdAsync(string userId);
}