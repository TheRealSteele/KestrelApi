using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace KestrelApi.Infrastructure;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (LogAndReturnTrue(ex, context.TraceIdentifier))
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private bool LogAndReturnTrue(Exception ex, string correlationId)
    {
        _logger.LogError(ex, "An unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);
        return true;
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "An error occurred while processing your request",
            Status = StatusCodes.Status500InternalServerError,
            Instance = context.Request.Path,
            Extensions = { ["correlationId"] = correlationId }
        };

        // Don't expose sensitive exception details in production
        if (!context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsProduction())
        {
            problemDetails.Detail = exception.Message;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problemDetails, JsonOptions);

        await context.Response.WriteAsync(json);
    }
}