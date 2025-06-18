using System.Security.Claims;
using KestrelApi.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KestrelApi.Names;

[Route("names")]
[Authorize]
public class NamesController : BaseApiController
{
    private readonly INamesService _namesService;

    public NamesController(INamesService namesService, ILogger<NamesController> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(namesService);
        _namesService = namesService;
    }

    [HttpPost]
    public async Task<IActionResult> AddName([FromBody] NameRequest request)
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
            await _namesService.AddNameAsync(userId, request.Name);
            return Created(new Uri("/names", UriKind.Relative), null);
        }
        catch (ArgumentNullException ex)
        {
            return HandleException(ex, userId, "adding name");
        }
        catch (InvalidOperationException ex)
        {
            return HandleException(ex, userId, "adding name");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetNames()
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
            var names = await _namesService.GetNamesAsync(userId);
            return Ok(names.ToArray());
        }
        catch (ArgumentNullException ex)
        {
            return HandleException(ex, userId, "retrieving names");
        }
        catch (InvalidOperationException ex)
        {
            return HandleException(ex, userId, "retrieving names");
        }
    }
}