namespace KestrelApi.Security;

public interface IEncryptionService
{
    Task<string> EncryptAsync(string plainText);
    Task<string> DecryptAsync(string cipherText);
}