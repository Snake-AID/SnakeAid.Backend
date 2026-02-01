# Snake Detection - Source Code Reference

> **Status:** ✅ Implemented  
> **Last Updated:** 2026-01-29

---

## Architecture Overview

**Frontend-facing API:**
- `SnakeDetectionController` at `/api/detection`
- Simple `SnakeDetectionRequest` with just `ImageUrl`
- Returns `ApiResponse<SnakeDetectionResponse>` wrapper

**External ML Integration:**
- `SnakeAIService` calls external SnakeAI FastAPI
- Detailed `SnakeAIDetectRequest` with ML parameters
- Data mapping between frontend ↔ external formats

---

## File Structure

```
SnakeAid.Backend/
├── SnakeAid.Core/
│   ├── Requests/SnakeDetection/
│   │   └── SnakeDetectionRequest.cs       (Frontend)
│   ├── Requests/SnakeAI/
│   │   └── SnakeAIDetectRequest.cs         (External)
│   ├── Responses/SnakeDetection/
│   │   └── SnakeDetectionResponse.cs       (Frontend)
│   └── Responses/SnakeAI/
│       └── SnakeAIDetectResponse.cs        (External)
├── SnakeAid.Service/
│   ├── Interfaces/
│   │   ├── ISnakeAIApi.cs                  (Refit interface)
│   │   └── ISnakeAIService.cs              (Service interface)
│   └── Implements/
│       └── SnakeAIService.cs               (Data mapping)
└── SnakeAid.Api/
    └── Controllers/
        └── SnakeDetectionController.cs     (BaseController pattern)
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
    Task<SnakeAIDetectResponse> DetectAsync(string imageUrl);
    Task<bool> IsHealthyAsync();
} 
```

### [SnakeAIService.cs](file:///d:/SourceCode/Snake_AID/SnakeAid.Backend/SnakeAid.Service/Implements/External/SnakeAIService.cs)

Implementation with logging and error handling.

```csharp
public class SnakeAIService : ISnakeAIService
{
    private readonly SnakeAISettings _settings;

    public SnakeAIService(ISnakeAIApi api, ILogger<SnakeAIService> logger, SnakeAISettings settings)
    {
        _api = api;
        _logger = logger;
        _settings = settings;
    }

    public async Task<SnakeAIDetectResponse> DetectAsync(string imageUrl)
    {
        var request = new SnakeAIDetectRequest
        {
            ImageUrl = imageUrl,
            Confidence = _settings.Confidence,
            ImageSize = _settings.ImageSize,
            Iou = _settings.IouThreshold
        };

        _logger.LogInformation("Calling SnakeAI detect for URL: {Url} with confidence {Conf}", imageUrl, _settings.Confidence);
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
        var result = await _snakeAIService.DetectAsync(request.ImageUrl);

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
