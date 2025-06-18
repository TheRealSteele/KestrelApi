namespace KestrelApi.Names;

public interface INamesRepository
{
    string Add(string userId, string name);
    IEnumerable<string> GetByUserId(string userId);
}