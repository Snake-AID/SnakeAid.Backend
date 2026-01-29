# AI Vision Detection - Implementation Prompt

> **Dành cho:** AI Agent (Antigravity, Copilot)  
> **Mục tiêu:** Implement endpoints `POST /api/aivision/detect` và `GET /api/aivision/{id}`

---

## Context

Bạn đang implement endpoint nhận diện rắn bằng AI cho SnakeAid Backend. Endpoint này nhận `imageUrl` từ Cloudinary và gọi SnakeAI FastAPI service để detect.

**Tham khảo:**
- [Implementation Plan](aivision-detect.plan.md) - Architecture, entity, packages
- [SnakeAI API Reference](../../../02-layers/ai/SankeAi.introduction.md) - FastAPI endpoint details

---

## Step 1: Thêm Polly packages

Thêm vào `Directory.Packages.props`:

```xml
<PackageVersion Include="Polly" Version="8.5.2" />
<PackageVersion Include="Microsoft.Extensions.Http.Polly" Version="8.0.0" />
```

Thêm vào `SnakeAid.Infrastructure/SnakeAid.Infrastructure.csproj`:

```xml
<PackageReference Include="Polly" />
<PackageReference Include="Microsoft.Extensions.Http.Polly" />
```

---

## Step 2: Tạo DTOs cho SnakeAI

### `SnakeAid.Core/DTOs/AIVision/SnakeAIDetectRequest.cs`

```csharp
using System.Text.Json.Serialization;

namespace SnakeAid.Core.DTOs.AIVision;

public class SnakeAIDetectRequest
{
    [JsonPropertyName("image_url")]
    public required string ImageUrl { get; set; }
    
    [JsonPropertyName("imgsz")]
    public int ImageSize { get; set; } = 640;
    
    [JsonPropertyName("conf")]
    public float Confidence { get; set; } = 0.25f;
    
    [JsonPropertyName("iou")]
    public float Iou { get; set; } = 0.5f;
    
    [JsonPropertyName("topk")]
    public int TopK { get; set; } = 100;
    
    [JsonPropertyName("save_image")]
    public bool SaveImage { get; set; } = false;
}
```

### `SnakeAid.Core/DTOs/AIVision/SnakeAIDetectResponse.cs`

```csharp
using System.Text.Json.Serialization;

namespace SnakeAid.Core.DTOs.AIVision;

public class SnakeAIDetectResponse
{
    [JsonPropertyName("model_version")]
    public string ModelVersion { get; set; }
    
    [JsonPropertyName("image_width")]
    public int ImageWidth { get; set; }
    
    [JsonPropertyName("image_height")]
    public int ImageHeight { get; set; }
    
    [JsonPropertyName("warnings")]
    public SnakeAIWarnings? Warnings { get; set; }
    
    [JsonPropertyName("detections")]
    public List<SnakeAIDetection> Detections { get; set; } = new();
}

public class SnakeAIWarnings
{
    [JsonPropertyName("blur")]
    public float Blur { get; set; }
    
    [JsonPropertyName("brightness")]
    public float Brightness { get; set; }
    
    [JsonPropertyName("too_small")]
    public float TooSmall { get; set; }
}

public class SnakeAIDetection
{
    [JsonPropertyName("class_id")]
    public int ClassId { get; set; }
    
    [JsonPropertyName("class_name")]
    public string ClassName { get; set; }
    
    [JsonPropertyName("confidence")]
    public float Confidence { get; set; }
    
    [JsonPropertyName("bbox")]
    public BoundingBox Bbox { get; set; }
}

public class BoundingBox
{
    public int X1 { get; set; }
    public int Y1 { get; set; }
    public int X2 { get; set; }
    public int Y2 { get; set; }
}
```

---

## Step 3: Tạo Refit Interface

### `SnakeAid.Infrastructure/External/ISnakeAIApi.cs`

```csharp
using Refit;
using SnakeAid.Core.DTOs.AIVision;

namespace SnakeAid.Infrastructure.External;

public interface ISnakeAIApi
{
    [Post("/detect/url")]
    Task<SnakeAIDetectResponse> DetectByUrlAsync([Body] SnakeAIDetectRequest request);
    
    [Get("/health")]
    Task<SnakeAIHealthResponse> HealthCheckAsync();
}

public class SnakeAIHealthResponse
{
    public string Status { get; set; }
    public bool ModelLoaded { get; set; }
    public string ModelVersion { get; set; }
}
```

---

## Step 4: Tạo Service Layer

### `SnakeAid.Infrastructure/External/SnakeAIService.cs`

```csharp
using Microsoft.Extensions.Logging;
using SnakeAid.Core.DTOs.AIVision;

namespace SnakeAid.Infrastructure.External;

public interface ISnakeAIService
{
    Task<SnakeAIDetectResponse> DetectAsync(string imageUrl);
    Task<bool> IsHealthyAsync();
} 

public class SnakeAIService : ISnakeAIService
{
    private readonly ISnakeAIApi _api;
    private readonly ILogger<SnakeAIService> _logger;
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
        
        _logger.LogInformation("SnakeAI detected {Count} objects", response.Detections.Count);
        
        return response;
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var health = await _api.HealthCheckAsync();
            return health.Status == "ok" && health.ModelLoaded;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SnakeAI health check failed");
            return false;
        }
    }
}
```

---

## Step 5: Register Services

### Thêm vào `Program.cs` hoặc DI extension

```csharp
using Polly;
using Polly.Extensions.Http;
using Refit;
using SnakeAid.Infrastructure.External;

// ... trong ConfigureServices

var snakeAIBaseUrl = builder.Configuration["SnakeAI:BaseUrl"] ?? "http://localhost:8000";

builder.Services
    .AddRefitClient<ISnakeAIApi>()
    .ConfigureHttpClient(c => 
    {
        c.BaseAddress = new Uri(snakeAIBaseUrl);
        c.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddScoped<ISnakeAIService, SnakeAIService>();

// Helper methods
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
```

### Thêm config vào `appsettings.json`

```json
{
  "SnakeAI": {
    "BaseUrl": "http://localhost:8000"
  }
}
```

---

## Step 6: Tạo Carter Endpoints

### `SnakeAid.API/Endpoints/AIVisionEndpoints.cs`

```csharp
using Carter;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Domains;
using SnakeAid.Infrastructure.External;

namespace SnakeAid.API.Endpoints;

public class AIVisionEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/aivision")
            .WithTags("AI Vision")
            .RequireAuthorization();

        group.MapPost("/detect", DetectSnake)
            .WithName("DetectSnake")
            .WithSummary("Detect snake species from image URL");

        group.MapGet("/{id:guid}", GetResult)
            .WithName("GetDetectionResult")
            .WithSummary("Get detection result by ID");
    }

    private static async Task<IResult> DetectSnake(
        [FromBody] DetectRequest request,
        ISnakeAIService snakeAIService,
        AppDbContext db,
        CancellationToken ct)
    {
        // 1. Check AI service health (internal use only)
        if (!await snakeAIService.IsHealthyAsync())
        {
            return Results.Problem(
                statusCode: 503,
                title: "AI Service Unavailable",
                detail: "Snake detection service is currently unavailable. Please try again later.");
        }

        // 2. Call SnakeAI
        var aiResponse = await snakeAIService.DetectAsync(request.ImageUrl);

        // 3. Get top detection
        var topDetection = aiResponse.Detections
            .OrderByDescending(d => d.Confidence)
            .FirstOrDefault();

        // 4. Save to database
        var result = new SnakeAIRecognitionResult
        {
            Id = Guid.NewGuid(),
            ReportMediaId = request.ReportMediaId,
            AIModelId = 1, // TODO: Get from config or AIModel table
            YoloClassName = topDetection?.ClassName ?? "none",
            Confidence = (decimal)(topDetection?.Confidence ?? 0),
            AllDetections = System.Text.Json.JsonSerializer.Serialize(aiResponse.Detections),
            Status = topDetection != null ? RecognitionStatus.Completed : RecognitionStatus.Failed,
            IsMapped = false // TODO: Map YOLO class to SnakeSpecies
        };

        db.SnakeAIRecognitionResults.Add(result);
        await db.SaveChangesAsync(ct);

        // 5. Return response
        return Results.Ok(new DetectResponse
        {
            Id = result.Id,
            ModelVersion = aiResponse.ModelVersion,
            TopClassName = result.YoloClassName,
            TopConfidence = (float)result.Confidence,
            Detections = aiResponse.Detections,
            Status = result.Status.ToString()
        });
    }

    private static async Task<IResult> GetResult(
        Guid id,
        AppDbContext db,
        CancellationToken ct)
    {
        var result = await db.SnakeAIRecognitionResults
            .Include(r => r.DetectedSpecies)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (result == null)
            return Results.NotFound();

        return Results.Ok(new DetectResponse
        {
            Id = result.Id,
            TopClassName = result.YoloClassName,
            TopConfidence = (float)result.Confidence,
            DetectedSpeciesId = result.DetectedSpeciesId,
            Status = result.Status.ToString()
        });
    }
}

// Request/Response DTOs for API
public record DetectRequest(
    string ImageUrl,
    Guid ReportMediaId,
    float Confidence = 0.25f
);

public class DetectResponse
{
    public Guid Id { get; set; }
    public string ModelVersion { get; set; }
    public string TopClassName { get; set; }
    public float TopConfidence { get; set; }
    public int? DetectedSpeciesId { get; set; }
    public string Status { get; set; }
    public List<SnakeAIDetection> Detections { get; set; }
}
```

---

## Step 7: Integration Testing

1. **Chạy SnakeAI service:**
   ```bash
   cd SnakeAI.ModelEndpoint
   python main.py
   ```

2. **Upload ảnh test:**
   ```http
   POST /api/media/upload-image
   Content-Type: multipart/form-data
   
   → Response: { "url": "https://res.cloudinary.com/.../snake.jpg" }
   ```

3. **Gọi detect:**
   ```http
   POST /api/aivision/detect
   Content-Type: application/json
   Authorization: Bearer {token}
   
   {
     "imageUrl": "https://res.cloudinary.com/.../snake.jpg",
     "reportMediaId": "guid-of-report-media"
   }
   ```

4. **Expected response:**
   ```json
   {
     "id": "detection-result-guid",
     "modelVersion": "snake-yolo12-v1.0",
     "topClassName": "naja_kaouthia",
     "topConfidence": 0.89,
     "status": "Completed",
     "detections": [...]
   }
   ```

---

## Step 8: Unit Tests

### `SnakeAid.Tests/Services/SnakeAIServiceTests.cs`

```csharp
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SnakeAid.Core.DTOs.AIVision;
using SnakeAid.Infrastructure.External;

namespace SnakeAid.Tests.Services;

public class SnakeAIServiceTests
{
    private readonly Mock<ISnakeAIApi> _mockApi;
    private readonly Mock<ILogger<SnakeAIService>> _mockLogger;
    private readonly SnakeAIService _service;

    public SnakeAIServiceTests()
    {
        _mockApi = new Mock<ISnakeAIApi>();
        _mockLogger = new Mock<ILogger<SnakeAIService>>();
        _service = new SnakeAIService(_mockApi.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task DetectAsync_ValidUrl_ReturnsDetections()
    {
        // Arrange
        _mockApi.Setup(x => x.DetectByUrlAsync(It.IsAny<SnakeAIDetectRequest>()))
            .ReturnsAsync(new SnakeAIDetectResponse
            {
                ModelVersion = "snake-yolo12-v1.0",
                Detections = new List<SnakeAIDetection>
                {
                    new() { ClassName = "naja_kaouthia", Confidence = 0.89f }
                }
            });

        // Act
        var result = await _service.DetectAsync("https://cloudinary.com/snake.jpg");

        // Assert
        result.Detections.Should().HaveCount(1);
        result.Detections[0].ClassName.Should().Be("naja_kaouthia");
        result.Detections[0].Confidence.Should().Be(0.89f);
    }

    [Fact]
    public async Task DetectAsync_ApiThrows_PropagatesException()
    {
        // Arrange
        _mockApi.Setup(x => x.DetectByUrlAsync(It.IsAny<SnakeAIDetectRequest>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            _service.DetectAsync("https://cloudinary.com/snake.jpg"));
    }

    [Fact]
    public async Task DetectAsync_NoDetections_ReturnsEmptyList()
    {
        // Arrange
        _mockApi.Setup(x => x.DetectByUrlAsync(It.IsAny<SnakeAIDetectRequest>()))
            .ReturnsAsync(new SnakeAIDetectResponse
            {
                ModelVersion = "snake-yolo12-v1.0",
                Detections = new List<SnakeAIDetection>()
            });

        // Act
        var result = await _service.DetectAsync("https://cloudinary.com/no-snake.jpg");

        // Assert
        result.Detections.Should().BeEmpty();
    }

    [Fact]
    public async Task IsHealthyAsync_ServiceUp_ReturnsTrue()
    {
        // Arrange
        _mockApi.Setup(x => x.HealthCheckAsync())
            .ReturnsAsync(new SnakeAIHealthResponse 
            { 
                Status = "ok", 
                ModelLoaded = true,
                ModelVersion = "snake-yolo12-v1.0"
            });

        // Act
        var result = await _service.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsHealthyAsync_ModelNotLoaded_ReturnsFalse()
    {
        // Arrange
        _mockApi.Setup(x => x.HealthCheckAsync())
            .ReturnsAsync(new SnakeAIHealthResponse 
            { 
                Status = "ok", 
                ModelLoaded = false 
            });

        // Act
        var result = await _service.IsHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsHealthyAsync_ServiceDown_ReturnsFalse()
    {
        // Arrange
        _mockApi.Setup(x => x.HealthCheckAsync())
            .ThrowsAsync(new HttpRequestException());

        // Act
        var result = await _service.IsHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }
}
```

### Run Tests

```bash
dotnet test --filter "FullyQualifiedName~SnakeAIServiceTests"
```

---

## Checklist

- [ ] Step 1: Thêm Polly packages
- [ ] Step 2: Tạo DTOs
- [ ] Step 3: Tạo Refit interface
- [ ] Step 4: Tạo Service layer
- [ ] Step 5: Register DI + config
- [ ] Step 6: Tạo Carter endpoints
- [ ] Step 7: Integration testing
- [ ] Step 8: Unit tests

---

## Notes

- **TODO sau khi implement:**
  - Map YOLO class → SnakeSpecies (cần bảng `AISnakeClassMapping`)
  - Thêm validation cho imageUrl (phải là Cloudinary URL)
  - Xử lý warnings từ AI (blur, brightness)
