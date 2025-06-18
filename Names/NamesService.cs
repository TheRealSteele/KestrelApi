namespace KestrelApi.Names;

public class NamesService : INamesService
{
    private readonly INamesRepository _repository;
    private readonly ILogger<NamesService> _logger;

    public NamesService(
        INamesRepository repository,
        ILogger<NamesService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<string> AddNameAsync(string userId, string name)
    {
        try
        {
            _logger.LogInformation("Adding name for user {UserId}", userId);
            var result = await Task.Run(() => _repository.Add(userId, name));
            _logger.LogInformation("Name added successfully for user {UserId}", userId);
            return result;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Null argument provided when adding name for user {UserId}", userId);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when adding name for user {UserId}", userId);
            throw;
        }
    }
    
    public async Task<IEnumerable<string>> GetNamesAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Retrieving names for user {UserId}", userId);
            var names = await Task.Run(() => _repository.GetByUserId(userId));
            var namesList = names.ToList();
            _logger.LogInformation("Retrieved {Count} names for user {UserId}", namesList.Count, userId);
            return namesList;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Null argument provided when retrieving names for user {UserId}", userId);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when retrieving names for user {UserId}", userId);
            throw;
        }
    }
}