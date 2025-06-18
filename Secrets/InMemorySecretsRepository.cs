using KestrelApi.Infrastructure;

namespace KestrelApi.Secrets;

public class InMemorySecretsRepository : InMemoryRepository<string>, ISecretsRepository
{
}