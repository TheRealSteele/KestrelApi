namespace KestrelApi.Names;

public interface INamesService
{
    Task<string> AddNameAsync(string userId, string name);
    Task<IEnumerable<string>> GetNamesAsync(string userId);
}