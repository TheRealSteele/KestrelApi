using System.Collections.Concurrent;

namespace KestrelApi.Infrastructure;

public interface IRepository<T>
{
    T Add(string userId, T item);
    IEnumerable<T> GetByUserId(string userId);
}

public class InMemoryRepository<T> : IRepository<T>
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<T>> _storage = new();
    
    public T Add(string userId, T item)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(item);
        
        var userItems = _storage.GetOrAdd(userId, _ => new ConcurrentBag<T>());
        userItems.Add(item);
        return item;
    }
    
    public IEnumerable<T> GetByUserId(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        
        if (_storage.TryGetValue(userId, out var userItems))
        {
            return userItems.ToArray();
        }
        
        return Array.Empty<T>();
    }
}