using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KestrelApi.Names;

[ApiController]
[Route("names")]
[Authorize]
public class NamesController : ControllerBase
{
    private readonly INamesService _namesService;
    private readonly ILogger<NamesController> _logger;

    public NamesController(INamesService namesService, ILogger<NamesController> logger)
    {
        _namesService = namesService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> AddName([FromBody] NameRequest request)
    {
        if (request == null)
        {
            return BadRequest("Request cannot be null");
        }
        
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name cannot be null or empty");
        }

        // Get user ID from claims
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("User ID not found in token");
        }

        try
        {
            await _namesService.AddNameAsync(userId, request.Name);
            return Created(new Uri("/names", UriKind.Relative), null);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Null argument while adding name for user {UserId}", userId);
            return BadRequest("Invalid request data");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while adding name for user {UserId}", userId);
            return StatusCode(500, "An error occurred while storing the name");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetNames()
    {
        // Get user ID from claims
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("User ID not found in token");
        }

        try
        {
            var names = await _namesService.GetNamesAsync(userId);
            return Ok(names.ToArray());
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Null argument while retrieving names for user {UserId}", userId);
            return BadRequest("Invalid request");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while retrieving names for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving names");
        }
    }
}