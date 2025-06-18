using System.Collections.Concurrent;

namespace KestrelApi.Secrets;

public class InMemorySecretsRepository : ISecretsRepository
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _storage = new();
    
    public Task<string> AddAsync(string userId, string encryptedSecret)
    {
        var userSecrets = _storage.GetOrAdd(userId, _ => new ConcurrentBag<string>());
        userSecrets.Add(encryptedSecret);
        return Task.FromResult(encryptedSecret);
    }
    
    public Task<IEnumerable<string>> GetByUserIdAsync(string userId)
    {
        if (_storage.TryGetValue(userId, out var userSecrets))
        {
            return Task.FromResult<IEnumerable<string>>(userSecrets.ToArray());
        }
        
        return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
    }
}