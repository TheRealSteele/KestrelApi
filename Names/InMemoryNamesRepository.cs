using System.Collections.Concurrent;

namespace KestrelApi.Names;

public class InMemoryNamesRepository : INamesRepository
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _storage = new();
    
    public Task<string> AddAsync(string userId, string name)
    {
        var userNames = _storage.GetOrAdd(userId, _ => new ConcurrentBag<string>());
        userNames.Add(name);
        return Task.FromResult(name);
    }
    
    public Task<IEnumerable<string>> GetByUserIdAsync(string userId)
    {
        if (_storage.TryGetValue(userId, out var userNames))
        {
            return Task.FromResult<IEnumerable<string>>(userNames.ToArray());
        }
        
        return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
    }
}