namespace KestrelApi.Secrets;

public interface ISecretsRepository
{
    string Add(string userId, string encryptedSecret);
    IEnumerable<string> GetByUserId(string userId);
}