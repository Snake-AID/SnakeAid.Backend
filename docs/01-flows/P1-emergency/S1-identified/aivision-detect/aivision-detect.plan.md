# AI Vision Detection - Implementation Plan

## Phân tích hiện trạng

### Đã có sẵn ✅
- `POST /api/media/upload-image` - Upload ảnh lên Cloudinary
- SnakeAI FastAPI service running at `http://localhost:8000`
- SnakeAI endpoints: `/detect/url`, `/detect/file`, `/detect/base64`, `/health`
- Entity `SnakeAIRecognitionResult` đã định nghĩa trong `SnakeAid.Core.Domains`
- Package `Refit` + `Refit.HttpClientFactory` đã cài sẵn

### Cần implement 📝
- `POST /api/aivision/detect` - Wrapper endpoint trong ASP.NET backend
- `GET /api/aivision/{id}` - Lấy kết quả detection đã lưu
- Service layer gọi SnakeAI FastAPI (sử dụng Refit)

> **Note:** `/health` endpoint từ FastAPI chỉ dùng nội bộ trong service layer để kiểm tra trước khi gọi AI, không expose ra client.

## Architecture

```
┌─────────────┐      ┌──────────────────┐      ┌─────────────────┐
│   Client    │ ---> │  ASP.NET Backend │ ---> │  SnakeAI FastAPI│
│  (Mobile)   │      │  /api/aivision   │      │  /detect/url    │
└─────────────┘      └──────────────────┘      └─────────────────┘
                              │                        │
                              v                        v
                     ┌──────────────────┐      ┌──────────────┐
                     │   PostgreSQL DB  │      │   /health    │
                     │ SnakeAIRecognition│      │ (internal)   │
                     └──────────────────┘      └──────────────┘
```

## Files to Create/Modify

### New Files
| File | Description |
|------|-------------|
| `SnakeAid.Infrastructure/External/ISnakeAIApi.cs` | Refit interface cho SnakeAI FastAPI |
| `SnakeAid.Infrastructure/External/SnakeAIService.cs` | Service implementation với Polly retry |
| `SnakeAid.API/Endpoints/AIVisionEndpoints.cs` | Carter module cho API endpoints |
| `SnakeAid.Core/DTOs/AIVision/DetectRequest.cs` | Request DTO |
| `SnakeAid.Core/DTOs/AIVision/DetectResponse.cs` | Response DTO |

### Modify Files
| File | Changes |
|------|---------|
| `Program.cs` | Register Refit client + Polly policies |
| `appsettings.json` | Add SnakeAI base URL config |

## Existing Entity

Entity `SnakeAIRecognitionResult` đã tồn tại:

```csharp
// SnakeAid.Core/Domains/SnakeAIRecognitionResult.cs
public class SnakeAIRecognitionResult : BaseEntity
{
    public Guid Id { get; set; }
    public Guid ReportMediaId { get; set; }           // FK → ReportMedia
    public int AIModelId { get; set; }                 // FK → AIModel
    public string YoloClassName { get; set; }          // Raw YOLO class
    public decimal Confidence { get; set; }            // 0.0 - 1.0
    public int? DetectedSpeciesId { get; set; }        // FK → SnakeSpecies (mapped)
    public bool IsMapped { get; set; }                 // YOLO → Species mapped?
    public string? AllDetections { get; set; }         // JSONB all results
    public RecognitionStatus Status { get; set; }      // Processing/Completed/Failed/ExpertVerified
    
    // Expert verification fields
    public Guid? ExpertId { get; set; }
    public DateTime? ExpertVerifiedAt { get; set; }
    public int? ExpertCorrectedSpeciesId { get; set; }
    public string? ExpertNotes { get; set; }
}
```

## Dependencies

Sử dụng packages đã cài sẵn trong `Directory.Packages.props`:

```xml
<!-- HTTP Client với type-safe interface -->
<PackageReference Include="Refit" />
<PackageReference Include="Refit.HttpClientFactory" />

<!-- Cần thêm: Retry & Circuit Breaker patterns -->
<PackageVersion Include="Polly" Version="8.5.2" />
<PackageVersion Include="Microsoft.Extensions.Http.Polly" Version="8.0.0" />
```

## Refit Interface

```csharp
// ISnakeAIApi.cs
public interface ISnakeAIApi
{
    [Post("/detect/url")]
    Task<SnakeAIDetectResponse> DetectByUrlAsync([Body] SnakeAIDetectRequest request);
    
    [Get("/health")]
    Task<SnakeAIHealthResponse> HealthCheckAsync();
}
```

## SnakeAI Endpoint Parameters

### POST `/detect/url` - Request Body

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `image_url` | string | ✅ Yes | - | URL công khai của ảnh (Cloudinary URL) |
| `imgsz` | int | No | 640 | Kích thước inference (longest side). Larger = slower but more accurate |
| `conf` | float | No | 0.25 | Confidence threshold (0.0 - 1.0). Higher = stricter matching |
| `iou` | float | No | 0.5 | NMS IoU threshold (0.0 - 1.0). Để loại bỏ duplicate boxes |
| `topk` | int | No | 100 | Số lượng detections tối đa trả về |
| `save_image` | bool | No | false | Lưu ảnh đã xử lý (bounding boxes) vào disk |

### Response Object

```json
{
  "model_version": "snake-yolo12-v1.0",
  "image_width": 1280,
  "image_height": 720,
  "warnings": {
    "blur": 0.05,          // 0.0-1.0: mức độ blur
    "brightness": 0.45,    // 0.0-1.0: độ sáng 
    "too_small": 0.0       // 0.0-1.0: đối tượng quá nhỏ
  },
  "detections": [
    {
      "class_id": 5,
      "class_name": "naja_kaouthia",
      "confidence": 0.89,
      "bbox": { "x1": 100, "y1": 200, "x2": 300, "y2": 400 }
    }
  ],
  "saved_image_path": null  // Chỉ có nếu save_image=true
}
```

### Error Codes từ SnakeAI

| HTTP Status | Error Code | Mô tả |
|-------------|------------|-------|
| 400 | `INVALID_CONTENT_TYPE` | File upload không phải ảnh |
| 400 | `INVALID_IMAGE` | Không decode được ảnh |
| 400 | `URL_FETCH_ERROR` | Không tải được ảnh từ URL |
| 413 | `DOWNLOAD_TOO_LARGE` | Ảnh quá lớn |
| 429 | `RATE_LIMITED` | Bị rate limit |
| 503 | `MODEL_NOT_LOADED` | Model chưa load xong |
| 504 | `URL_FETCH_TIMEOUT` | Timeout khi tải ảnh |

> **Tham khảo chi tiết:** [SnakeAI API Reference](../../../02-layers/ai/SankeAi.introduction.md)

## Polly Retry Configuration

```csharp
// Program.cs
builder.Services
    .AddRefitClient<ISnakeAIApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(snakeAIConfig.BaseUrl))
    .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, 
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
    .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

## Configuration

```json
// appsettings.json
{
  "SnakeAI": {
    "BaseUrl": "http://localhost:8000",
    "TimeoutSeconds": 30,
    "DefaultConfidence": 0.25,
    "DefaultImageSize": 640,
    "RetryCount": 3,
    "CircuitBreakerThreshold": 5
  }
}
```

## Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| SnakeAI service down | High | Polly Circuit Breaker + graceful error response |
| Slow image download | Medium | Timeout + Polly retry với exponential backoff |
| Invalid image URL | Low | Validate URL format trước khi gọi |
| Rate limiting từ SnakeAI | Medium | Polly rate limiting policy |

## Unit Tests

### Test Files to Create

| File | Tests |
|------|-------|
| `SnakeAid.Tests/Services/SnakeAIServiceTests.cs` | Service layer tests |
| `SnakeAid.Tests/Endpoints/AIVisionEndpointsTests.cs` | API endpoint tests |

### SnakeAIService Tests

```csharp
public class SnakeAIServiceTests
{
    private readonly Mock<ISnakeAIApi> _mockApi;
    private readonly SnakeAIService _service;

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
    public async Task IsHealthyAsync_ServiceUp_ReturnsTrue()
    {
        // Arrange
        _mockApi.Setup(x => x.HealthCheckAsync())
            .ReturnsAsync(new SnakeAIHealthResponse { Status = "ok", ModelLoaded = true });

        // Act
        var result = await _service.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();
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

### Test Coverage Requirements

| Component | Min Coverage | Critical Tests |
|-----------|--------------|----------------|
| `SnakeAIService.DetectAsync` | 80% | Success, API error, timeout |
| `SnakeAIService.IsHealthyAsync` | 80% | Healthy, unhealthy, exception |
| `AIVisionEndpoints.DetectSnake` | 70% | Success, AI unavailable, invalid input |
| `AIVisionEndpoints.GetResult` | 70% | Found, not found |

---

## Timeline

| Phase | Task | Estimate |
|-------|------|----------|
| 1 | Thêm Polly packages vào Directory.Packages.props | 5m |
| 2 | Tạo Refit interface + DTOs | 30m |
| 3 | Implement SnakeAIService với Polly | 1h |
| 4 | Tạo AIVisionEndpoints (Carter) | 1h |
| 5 | Mapping YOLO class → SnakeSpecies | 30m |
| 6 | Unit tests | 1h |
| 7 | Integration testing | 30m |
| **Total** | | **~5h** |
