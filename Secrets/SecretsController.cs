using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KestrelApi.Secrets;

[Route("secrets")]
[Authorize]
[EnableRateLimiting("api")]
public class SecretsController : SecretsBaseController
{
    private readonly ISecretsService _secretsService;

    public SecretsController(ISecretsService secretsService, ILogger<SecretsController> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(secretsService);
        _secretsService = secretsService;
    }

    [HttpPost]
    [Authorize(Policy = "WriteSecrets")]
    public async Task<IActionResult> AddSecret([FromBody] SecretRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }
        
        var validationResult = ValidateUserIdAndRequest(request);
        if (validationResult is not OkObjectResult okResult)
        {
            return validationResult;
        }
        
        var userId = (string)okResult.Value!;

        try
        {
            await _secretsService.AddSecretAsync(userId, request.Secret);
            return Created(new Uri("/secrets", UriKind.Relative), null);
        }
        catch (ArgumentNullException ex)
        {
            return HandleException(ex, userId, "adding secret");
        }
        catch (CryptographicException ex)
        {
            return HandleException(ex, userId, "adding secret");
        }
        catch (InvalidOperationException ex)
        {
            return HandleException(ex, userId, "adding secret");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetSecrets()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Problem(
                detail: "User ID not found in token",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request"
            );
        }

        try
        {
            var secrets = await _secretsService.GetSecretsAsync(userId);
            return Ok(secrets.ToArray());
        }
        catch (ArgumentNullException ex)
        {
            return HandleException(ex, userId, "retrieving secrets");
        }
        catch (CryptographicException ex)
        {
            return HandleException(ex, userId, "retrieving secrets");
        }
        catch (InvalidOperationException ex)
        {
            return HandleException(ex, userId, "retrieving secrets");
        }
    }
}