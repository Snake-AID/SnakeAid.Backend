# AI Vision Detection - Source Code Reference

> **Status:** ✅ Implemented  
> **Last Updated:** 2026-01-29

---

## File Structure

```
SnakeAid.Backend/
├── SnakeAid.Core/
│   ├── Requests/AIVision/
│   │   └── SnakeAIDetectRequest.cs
│   └── Responses/AIVision/
│       └── SnakeAIDetectResponse.cs
├── SnakeAid.Service/
│   ├── Interfaces/External/
│   │   ├── ISnakeAIApi.cs
│   │   └── ISnakeAIService.cs
│   └── Implements/External/
│       └── SnakeAIService.cs
└── SnakeAid.Api/
    ├── Controllers/
    │   └── AIVisionController.cs
    ├── DI/
    │   └── DependencyInjection.cs (updated)
    └── appsettings.json (updated)
```

---

## DTOs

### [SnakeAIDetectRequest.cs](file:///d:/SourceCode/Snake_AID/SnakeAid.Backend/SnakeAid.Core/Requests/AIVision/SnakeAIDetectRequest.cs)

Request DTO for SnakeAI FastAPI `/detect/url` endpoint.

```csharp
public class SnakeAIDetectRequest
{
    [JsonPropertyName("image_url")]
    public required string ImageUrl { get; set; }
    
    [JsonPropertyName("conf")]
    public float Confidence { get; set; } = 0.25f;
    
    // ... other properties
}
```

### [SnakeAIDetectResponse.cs](file:///d:/SourceCode/Snake_AID/SnakeAid.Backend/SnakeAid.Core/Responses/AIVision/SnakeAIDetectResponse.cs)

Response DTO including detections, warnings, and health check.

```csharp
public class SnakeAIDetectResponse
{
    public string ModelVersion { get; set; }
    public List<SnakeAIDetection> Detections { get; set; }
    public SnakeAIWarnings? Warnings { get; set; }
}

public class SnakeAIDetection
{
    public int ClassId { get; set; }
    public string ClassName { get; set; }
    public float Confidence { get; set; }
    public BoundingBox Bbox { get; set; }
}
```

---

## Service Layer

### [ISnakeAIApi.cs](file:///d:/SourceCode/Snake_AID/SnakeAid.Backend/SnakeAid.Service/Interfaces/External/ISnakeAIApi.cs)

Refit interface for SnakeAI FastAPI.

```csharp
public interface ISnakeAIApi
{
    [Post("/detect/url")]
    Task<SnakeAIDetectResponse> DetectByUrlAsync([Body] SnakeAIDetectRequest request);
    
    [Get("/health")]
    Task<SnakeAIHealthResponse> HealthCheckAsync();
}
```

### [ISnakeAIService.cs](file:///d:/SourceCode/Snake_AID/SnakeAid.Backend/SnakeAid.Service/Interfaces/External/ISnakeAIService.cs)

Service interface with business logic.

```csharp
public interface ISnakeAIService
{
    Task<SnakeAIDetectResponse> DetectAsync(string imageUrl, float confidence = 0.25f);
    Task<bool> IsHealthyAsync();
}
```

### [SnakeAIService.cs](file:///d:/SourceCode/Snake_AID/SnakeAid.Backend/SnakeAid.Service/Implements/External/SnakeAIService.cs)

Implementation with logging and error handling.

```csharp
public class SnakeAIService : ISnakeAIService
{
    public async Task<SnakeAIDetectResponse> DetectAsync(string imageUrl, float confidence = 0.25f)
    {
        _logger.LogInformation("Calling SnakeAI detect for URL: {Url}", imageUrl);
        var response = await _api.DetectByUrlAsync(request);
        return response;
    }

    public async Task<bool> IsHealthyAsync()
    {
        try {
            var health = await _api.HealthCheckAsync();
            return health.Status == "ok" && health.ModelLoaded;
        } catch {
            return false;
        }
    }
}
```

---

## API Controller

### [AIVisionController.cs](file:///d:/SourceCode/Snake_AID/SnakeAid.Backend/SnakeAid.Api/Controllers/AIVisionController.cs)

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIVisionController : ControllerBase
{
    [HttpPost("detect")]
    public async Task<IActionResult> Detect([FromBody] AIVisionDetectRequest request)
    {
        // 1. Check AI service health
        if (!await _snakeAIService.IsHealthyAsync())
            return StatusCode(503, ...);

        // 2. Call detection
        var result = await _snakeAIService.DetectAsync(request.ImageUrl, request.Confidence);

        // 3. Return response
        return Ok(new AIVisionDetectResponse { ... });
    }
}
```

---

## DI Registration

### [DependencyInjection.cs](file:///d:/SourceCode/Snake_AID/SnakeAid.Backend/SnakeAid.Api/DI/DependencyInjection.cs#L38-L68)

Refit + Polly registration with retry and circuit breaker.

```csharp
services
    .AddRefitClient<ISnakeAIApi>()
    .ConfigureHttpClient(c => {
        c.BaseAddress = new Uri(snakeAIBaseUrl);
        c.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

services.AddScoped<ISnakeAIService, SnakeAIService>();
```

---

## Configuration

### [appsettings.json](file:///d:/SourceCode/Snake_AID/SnakeAid.Backend/SnakeAid.Api/appsettings.json#L140-L143)

```json
{
  "SnakeAI": {
    "BaseUrl": "http://localhost:8000"
  }
}
```
