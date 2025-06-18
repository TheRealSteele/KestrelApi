namespace KestrelApi.Names;

public interface INamesRepository
{
    Task<string> AddAsync(string userId, string name);
    Task<IEnumerable<string>> GetByUserIdAsync(string userId);
}