using KestrelApi.Infrastructure;

namespace KestrelApi.Names;

public class InMemoryNamesRepository : InMemoryRepository<string>, INamesRepository
{
}