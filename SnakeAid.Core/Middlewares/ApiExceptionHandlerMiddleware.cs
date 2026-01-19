using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SnakeAid.Core.Middlewares;

public class ApiExceptionHandlerMiddleware
{
    // Fields
    private readonly ILogger<ApiExceptionHandlerMiddleware> _logger;
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;

    //constructor
    public ApiExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _env = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestStartTime = DateTime.UtcNow;
        var requestId = context.TraceIdentifier;

        try
        {
            // Initial request logging
            _logger.LogInformation(
                "ApiExceptionMiddleware: Starting request processing - RequestId: {RequestId}, Path: {Path}, Method: {Method}",
                requestId,
                context.Request.Path,
                context.Request.Method);

            // Check API Gateway
            var isFromGateway = context.Request.Headers.TryGetValue("Api-Gateway", out var gatewayHeader);
            _logger.LogInformation(
                "Request source: {Source}",
                isFromGateway ? "API Gateway" : "Direct Client");

            // Authentication status
            var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;
            _logger.LogInformation(
                "Authentication Status - IsAuthenticated: {IsAuthenticated}, Identity: {IdentityName}",
                isAuthenticated,
                context.User?.Identity?.Name ?? "none");

            await _next(context);

            // Log response status
            var requestDuration = DateTime.UtcNow - requestStartTime;
            _logger.LogInformation(
                "Request completed - RequestId: {RequestId}, StatusCode: {StatusCode}, Duration: {Duration}ms",
                requestId,
                context.Response.StatusCode,
                requestDuration.TotalMilliseconds);
        }
        catch (Exception exception)
        {
            var errorId = Guid.NewGuid().ToString();
            _logger.LogError(
                exception,
                "Unhandled exception - RequestId: {RequestId}, ErrorId: {ErrorId}, Message: {Message}",
                requestId,
                errorId,
                exception.Message);

            LogError(errorId, context, exception);
            await HandleExceptionAsync(context, errorId, exception);
        }
    }

    private void LogError(string errorId, HttpContext context, Exception exception)
    {
        var error = new
        {
            ErrorId = errorId,
            Timestamp = DateTime.UtcNow,
            RequestPath = context.Request.Path,
            RequestMethod = context.Request.Method,
            ExceptionType = exception.GetType().Name,
            ExceptionMessage = exception.Message
        };

        var logLevel = exception switch
        {
            BusinessException => LogLevel.Warning,
            ValidationException => LogLevel.Warning,
            NotFoundException => LogLevel.Information,
            _ => LogLevel.Error
        };

        _logger.Log(logLevel, exception,
            "Error ID: {ErrorId} - Path: {Path} - Method: {Method} - {@error}",
            errorId,
            context.Request.Path,
            context.Request.Method,
            error);
    }

    // Update HandleExceptionAsync to use the new ErrorResponse format
    private async Task HandleExceptionAsync(HttpContext context, string errorId, Exception exception)
    {
        var statusCode = exception switch
        {
            ApiException apiEx => (int)apiEx.StatusCode,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        // Add specific handling for TooManyRequestsException
        if (exception is TooManyRequestsException tooManyRequestsEx && tooManyRequestsEx.RetryAfter.HasValue)
        {
            // Add Retry-After header in seconds
            var retryAfterSeconds =
                Math.Max(1, (int)(tooManyRequestsEx.RetryAfter.Value - DateTime.UtcNow).TotalSeconds);
            context.Response.Headers.Append("Retry-After", retryAfterSeconds.ToString());
            context.Response.Headers.Append("X-RateLimit-Reset", tooManyRequestsEx.RetryAfter.Value.ToString("r"));

            // Add additional rate limit headers if information is available
            if (tooManyRequestsEx.Limit.HasValue)
            {
                context.Response.Headers.Append("X-RateLimit-Limit", tooManyRequestsEx.Limit.Value.ToString());
            }

            if (!string.IsNullOrEmpty(tooManyRequestsEx.Period))
            {
                context.Response.Headers.Append("X-RateLimit-Period", tooManyRequestsEx.Period);
            }
        }

        // Generate a general message based on status code
        var generalMessage = statusCode switch
        {
            400 => "Bad Request - The request was invalid or cannot be served",
            401 => "Unauthorized - Authentication credentials are missing or invalid",
            403 => "Forbidden - You don't have permission to access this resource",
            404 => "Not Found - The requested resource does not exist",
            409 => "Conflict - The request conflicts with current state",
            429 => "Too Many Requests - Rate limit exceeded",
            500 => "Internal Server Error - Something went wrong on our end",
            _ => "An error occurred"
        };

        // Get specific error message from the exception
        var errorMessage = _env.IsDevelopment() || exception is ApiException
            ? exception.Message
            : "An unexpected error occurred";

        // Collect specific error details
        var errors = new List<string>();

        // Add rate limiting specific errors
        if (exception is TooManyRequestsException tooManyReq && tooManyReq.RetryAfter.HasValue)
        {
            var retryAfterSeconds = Math.Max(1, (int)(tooManyReq.RetryAfter.Value - DateTime.UtcNow).TotalSeconds);

            if (tooManyReq.Limit.HasValue && !string.IsNullOrEmpty(tooManyReq.Period))
            {
                errors.Add($"Rate limit of {tooManyReq.Limit} requests per {tooManyReq.Period} exceeded. Please retry after {retryAfterSeconds} seconds.");

                if (!string.IsNullOrEmpty(tooManyReq.Endpoint))
                {
                    _logger.LogDebug("Rate limit exceeded for endpoint: {Endpoint}", tooManyReq.Endpoint);
                }
            }
            else
            {
                errors.Add($"Too many requests. Please retry after {retryAfterSeconds} seconds.");
            }
        }

        // Add validation errors if present
        if (exception is ValidationException validationEx && validationEx.Errors != null && validationEx.Errors.Any())
            errors.AddRange(validationEx.Errors);

        // Create the enhanced ErrorResponse format
        var errorResponse = new ErrorResponse
        {
            Message = generalMessage,
            Reason = errorMessage,
            Errors = errors.Count > 0 ? errors : new List<string>()
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        if (!context.Response.HasStarted) await context.Response.WriteAsJsonAsync(errorResponse);
    }
}

// Extension method remains the same
public static class ExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseApiExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiExceptionHandlerMiddleware>();
    }
}