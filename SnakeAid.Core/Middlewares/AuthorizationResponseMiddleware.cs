using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SnakeAid.Core.Meta;
using System.Text.Json;

namespace SnakeAid.Core.Middlewares;

public class AuthorizationResponseMiddleware
{
    private readonly RequestDelegate _next;

    public AuthorizationResponseMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // Handle authorization responses after all other middlewares
        if (!context.Response.HasStarted)
        {
            if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
            {
                await HandleUnauthorizedAsync(context);
            }
            else if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
            {
                await HandleForbiddenAsync(context);
            }
        }
    }

    private async Task HandleUnauthorizedAsync(HttpContext context)
    {
        var response = ApiResponseBuilder.BuildUnauthorizedResponse("Authentication required. Please provide a valid token.");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }

    private async Task HandleForbiddenAsync(HttpContext context)
    {
        var response = ApiResponseBuilder.BuildForbiddenResponse("Access denied. You don't have permission to access this resource.");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status403Forbidden;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

// Extension method for easy registration
public static class AuthorizationResponseMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthorizationResponseHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthorizationResponseMiddleware>();
    }
}