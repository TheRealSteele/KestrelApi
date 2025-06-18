using System.Security.Cryptography;
using KestrelApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace KestrelApi.Secrets;

public abstract class SecretsBaseController : BaseApiController
{
    protected SecretsBaseController(ILogger<SecretsBaseController> logger) : base(logger)
    {
    }

    protected override IActionResult HandleException(Exception ex, string userId, string operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        
        if (ex is CryptographicException)
        {
            Logger.LogError(ex, "Cryptographic error during {Operation} for user {UserId}", operation, userId);
            return Problem(
                detail: operation.Contains("adding", StringComparison.OrdinalIgnoreCase) ? "Failed to encrypt data" : "Failed to decrypt data",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Encryption Error"
            );
        }

        return base.HandleException(ex, userId, operation);
    }
}