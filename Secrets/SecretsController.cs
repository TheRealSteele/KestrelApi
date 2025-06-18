using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KestrelApi.Secrets;

[ApiController]
[Route("secrets")]
[Authorize]
public class SecretsController : ControllerBase
{
    private readonly ISecretsService _secretsService;
    private readonly ILogger<SecretsController> _logger;

    public SecretsController(ISecretsService secretsService, ILogger<SecretsController> logger)
    {
        _secretsService = secretsService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> AddSecret([FromBody] SecretRequest request)
    {
        if (request == null)
        {
            return BadRequest("Request cannot be null");
        }
        
        if (string.IsNullOrWhiteSpace(request.Secret))
        {
            return BadRequest("Secret cannot be null or empty");
        }

        // Get user ID from claims
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("User ID not found in token");
        }

        try
        {
            await _secretsService.AddSecretAsync(userId, request.Secret);
            return Created(new Uri("/secrets", UriKind.Relative), null);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Null argument while adding secret for user {UserId}", userId);
            return BadRequest("Invalid request data");
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Encryption failed while adding secret for user {UserId}", userId);
            return StatusCode(500, "Failed to encrypt secret");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while adding secret for user {UserId}", userId);
            return StatusCode(500, "An error occurred while storing the secret");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetSecrets()
    {
        // Get user ID from claims
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("User ID not found in token");
        }

        try
        {
            var secrets = await _secretsService.GetSecretsAsync(userId);
            return Ok(secrets.ToArray());
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Null argument while retrieving secrets for user {UserId}", userId);
            return BadRequest("Invalid request");
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Decryption failed while retrieving secrets for user {UserId}", userId);
            return StatusCode(500, "Failed to decrypt secrets");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while retrieving secrets for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving secrets");
        }
    }
}