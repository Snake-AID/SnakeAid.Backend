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

    // Update HandleExceptionAsync to use the new ApiResponse format
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

        // Generate error message and code based on exception type
        var (errorMessage, errorCode, validationErrors) = exception switch
        {
            ValidationException validationEx => (
                validationEx.Message,
                "VALIDATION_ERROR",
                validationEx.ValidationErrors.Any() ? validationEx.ValidationErrors : null
            ),
            NotFoundException notFoundEx => (
                notFoundEx.Message,
                "NOT_FOUND",
                null
            ),
            BusinessException businessEx => (
                businessEx.Message,
                "BUSINESS_ERROR",
                null
            ),
            TooManyRequestsException rateLimitEx => (
                rateLimitEx.Message,
                "RATE_LIMIT_EXCEEDED",
                null
            ),
            UnauthorizedException => (
                "Unauthorized access",
                "UNAUTHORIZED",
                null
            ),
            _ => (
                _env.IsDevelopment() ? exception.Message : "An unexpected error occurred",
                "INTERNAL_SERVER_ERROR",
                null
            )
        };

        // Create the new ApiResponse format
        var apiResponse = new ApiResponse<object>
        {
            StatusCode = statusCode,
            Message = errorMessage,
            IsSuccess = false,
            Data = null,
            Error = new ClientErrorResponse
            {
                ErrorCode = errorCode,
                Timestamp = DateTime.UtcNow,
                ValidationErrors = validationErrors
            }
        };

        // Add development-specific details
        if (_env.IsDevelopment())
        {
            // In development, we can add more details to help debugging
            var devDetails = new
            {
                ErrorId = errorId,
                ExceptionType = exception.GetType().Name,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message
            };

            // Note: You might want to add a Details property to ClientErrorResponse for this
            // For now, we'll log it and keep the response clean
            _logger.LogDebug("Development error details: {@DevDetails}", devDetails);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        if (!context.Response.HasStarted)
            await context.Response.WriteAsJsonAsync(apiResponse);
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