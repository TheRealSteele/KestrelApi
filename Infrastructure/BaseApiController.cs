using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace KestrelApi.Infrastructure;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected ILogger<BaseApiController> Logger { get; }

    protected BaseApiController(ILogger<BaseApiController> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        Logger = logger;
    }

    protected string? GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    protected virtual IActionResult HandleException(Exception ex, string userId, string operation)
    {
        switch (ex)
        {
            case ArgumentNullException:
                Logger.LogError(ex, "Null argument during {Operation} for user {UserId}", operation, userId);
                return Problem(
                    detail: "Invalid request data",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request"
                );
            
            case InvalidOperationException:
                Logger.LogError(ex, "Invalid operation during {Operation} for user {UserId}", operation, userId);
                return Problem(
                    detail: "An error occurred while processing your request",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            
            default:
                Logger.LogError(ex, "Unexpected error during {Operation} for user {UserId}", operation, userId);
                return Problem(
                    detail: "An unexpected error occurred",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
        }
    }

    protected IActionResult ValidateUserIdAndRequest<T>(T? request) where T : class
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Problem(
                detail: "User ID not found in token",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request"
            );
        }

        return Ok(userId);
    }
}