# Snake Detection - Implementation Prompt ✅ COMPLETED

> **Loại file:** `prompt.md` - Yeu cầu chi tiết từng bước cho Agent  
> **Timeline:** Phase 2 Implementation ✅ COMPLETED  
> **Context doc:** `snake-detection-with-snakelibs.plan.md`
> **Status:** Implementation completed successfully with Service Layer Pattern

---

## ✅ COMPLETED OBJECTIVES

**Successfully implemented** Phase 2 của tính năng Snake Detection, bao gồm:
1. ✅ Endpoint upload media mới: `POST /api/media/report`
2. ✅ Refactor endpoint detect: `POST /api/detection/detect` nhận `ReportMediaId` thay vì URL
3. ✅ Service Layer Pattern implementation với MediaService và SnakeAIService
4. ✅ Persistence logic: Lưu kết quả nhận diện vào DB với species mapping
5. ✅ NullReferenceException fixes và ClaimsPrincipal handling

---

## TASK 1: Create Media Upload Endpoint

### 1.1 Create Request DTO

**File:** `SnakeAid.Core/Requests/Media/UploadReportMediaRequest.cs`

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.Media;

public class UploadReportMediaRequest
{
    [Required]
    public IFormFile File { get; set; }

    [Required]
    public Guid ReferenceId { get; set; }
}
```

### 1.2 Create Response DTO

**File:** `SnakeAid.Core/Responses/Media/ReportMediaResponse.cs`

```csharp
using System;

namespace SnakeAid.Core.Responses.Media;

public class ReportMediaResponse
{
    public Guid Id { get; set; }
    public string MediaUrl { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long FileSize { get; set; }
}
```

### 1.3 Create Endpoint in MediaController

**File:** `SnakeAid.Api/Controllers/MediaController.cs`

Thêm method mới vào controller hiện có:

```csharp
[HttpPost("report")]
[Consumes("multipart/form-data")]
[ValidateFile(maxSizeInMB: 10, allowedExtensions: new[] { ".jpg", ".jpeg", ".png", ".webp" }, formFieldName: "file")]
public async Task<IActionResult> UploadReportMedia(
    [FromQuery] MediaReferenceType type,
    [FromQuery] MediaPurpose purpose = MediaPurpose.SnakeIdentification,
    [FromForm] UploadReportMediaRequest request,
    CancellationToken ct)
{
    // 1. Upload to Cloudinary
    var uploadResult = await _cloudinaryService.UploadImageAsync(request.File, User, "report-media", ct);
    
    // 2. Create ReportMedia entity
    var reportMedia = new ReportMedia
    {
        Id = Guid.NewGuid(),
        ReferenceId = request.ReferenceId,
        ReferenceType = type,
        FileName = request.File.FileName,
        MediaUrl = uploadResult.Url,
        ContentType = request.File.ContentType,
        FileSize = request.File.Length,
        Purpose = purpose,
        RequiresAIProcessing = purpose == MediaPurpose.SnakeIdentification
    };
    
    // 3. Save to DB
    await _unitOfWork.Repository<ReportMedia>().AddAsync(reportMedia, ct);
    await _unitOfWork.CommitAsync(ct);
    
    // 4. Return response with URL
    var response = _mapper.Map<ReportMediaResponse>(reportMedia);
    return Ok(ApiResponseBuilder.BuildSuccessResponse(response, "Media uploaded successfully."));
}
```

**Lưu ý:**
- Inject `IUnitOfWork` vào constructor của `MediaController`
- Thêm using cho `SnakeAid.Core.Domains`

---

## TASK 2: Refactor Snake Detection Endpoint

### 2.1 Update Request DTO

**File:** `SnakeAid.Core/Requests/Detection/SnakeDetectionRequest.cs`

```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Detection;

public class SnakeDetectionRequest
{
    [Required]
    public Guid ReportMediaId { get; set; }
}
```

### 2.2 Update Response DTO

**File:** `SnakeAid.Core/Responses/Detection/SnakeAIDetection.cs`

Thêm các field mới cho Species info:

```csharp
public class SnakeAIDetection
{
    // Existing YOLO fields
    public string ClassName { get; set; }
    public float Confidence { get; set; }
    public int[] BoundingBox { get; set; }
    
    // NEW: Species info from mapping
    public int? SpeciesId { get; set; }
    public string? SpeciesName { get; set; }        // CommonName
    public string? ScientificName { get; set; }
    public bool? IsVenomous { get; set; }
    public float? RiskLevel { get; set; }
}
```

### 2.3 Update Controller

**File:** `SnakeAid.Api/Controllers/SnakeDetectionController.cs`

```csharp
[HttpPost("detect/{reportMediaId:guid}")]
public async Task<IActionResult> Detect([FromRoute] Guid reportMediaId, CancellationToken ct = default)
{
    // 1. Health Check (fail-fast)
    if (!await _snakeAIService.IsHealthyAsync(ct))
    {
        return StatusCode(503, ApiResponseBuilder.BuildErrorResponse("AI Service is currently unavailable."));
    }
    
    // 2. Call Service directly with ID
    var result = await _snakeAIService.DetectFromReportMediaAsync(reportMediaId, ct);
    
    return StatusCode(result.StatusCode, result);
}
```

---

## TASK 3: Implement Mapping & Persistence in Service

### 3.1 Update ISnakeAIService Interface

**File:** `SnakeAid.Service/Interfaces/ISnakeAIService.cs`

```csharp
Task<SnakeDetectionResponse> DetectAsync(string imageUrl, Guid reportMediaId, CancellationToken ct = default);
```

### 3.2 Update SnakeAIService Implementation

**File:** `SnakeAid.Service/Implements/SnakeAIService.cs`

```csharp
public async Task<SnakeDetectionResponse> DetectAsync(string imageUrl, Guid reportMediaId, CancellationToken ct = default)
{
    // 1. Call AI Service (existing logic)
    var aiResponse = await _snakeAIClient.DetectAsync(new { image_url = imageUrl }, ct);
    
    // 2. Get Active AIModel
    var activeModel = await _unitOfWork.Repository<AIModel>()
        .FindAsync(m => m.IsActive && m.IsDefault, ct);
    
    if (activeModel == null)
    {
        throw new InvalidOperationException("No active AI model found.");
    }
    
    // 3. Process each detection result
    var enrichedDetections = new List<SnakeAIDetection>();
    
    foreach (var detection in aiResponse.Detections)
    {
        var enriched = new SnakeAIDetection
        {
            ClassName = detection.ClassName,
            Confidence = detection.Confidence,
            BoundingBox = detection.BoundingBox
        };
        
        // 4. Map to SnakeSpecies
        var mapping = await _unitOfWork.Repository<AISnakeClassMapping>()
            .FindAsync(m => m.AIModelId == activeModel.Id && m.YoloClassName == detection.ClassName, ct);
        
        if (mapping != null)
        {
            var species = await _unitOfWork.Repository<SnakeSpecies>()
                .GetByIdAsync(mapping.SnakeSpeciesId, ct);
            
            if (species != null)
            {
                enriched.SpeciesId = species.Id;
                enriched.SpeciesName = species.CommonName;
                enriched.ScientificName = species.ScientificName;
                enriched.IsVenomous = species.IsVenomous;
                enriched.RiskLevel = species.RiskLevel;
            }
        }
        
        // 5. Save Recognition Result
        var recognitionResult = new SnakeAIRecognitionResult
        {
            Id = Guid.NewGuid(),
            ReportMediaId = reportMediaId,
            AIModelId = activeModel.Id,
            YoloClassName = detection.ClassName,
            Confidence = (decimal)detection.Confidence,
            DetectedSpeciesId = enriched.SpeciesId,
            IsMapped = enriched.SpeciesId.HasValue,
            Status = RecognitionStatus.Completed
        };
        
        await _unitOfWork.Repository<SnakeAIRecognitionResult>().AddAsync(recognitionResult, ct);
        
        enrichedDetections.Add(enriched);
    }
    
    await _unitOfWork.CommitAsync(ct);
    
    return new SnakeDetectionResponse
    {
        Detections = enrichedDetections,
        ModelVersion = activeModel.Version
    };
}
```

---

## TASK 4: Dependencies & Registration

### 4.1 Inject IUnitOfWork into Controllers

Đảm bảo các controller được inject `IUnitOfWork`:
- `MediaController`
- `SnakeDetectionController`

### 4.2 Mapster Configuration

**File:** `SnakeAid.Api/Configurations/MapsterConfig.cs` (hoặc file cấu hình tương tự)

```csharp
config.NewConfig<ReportMedia, ReportMediaResponse>();
```

---

## VERIFICATION STEPS

1. **Build Solution**: `dotnet build`
2. **Run Tests**: `dotnet test`
3. **Manual Test Flow**:
   - Upload image via `POST /api/media/report?type=SnakebiteIncident&purpose=SnakeIdentification`
   - Use returned `Id` to call `POST /api/detection/detect`
   - Verify response includes Species info

---

## EXPECTED OUTPUT

- ✅ Endpoint `POST /api/media/report` returns `ReportMediaResponse` with `Id` and `MediaUrl`
- ✅ Endpoint `POST /api/detection/detect` accepts `ReportMediaId`
- ✅ Detection response includes `SpeciesName`, `ScientificName`, `IsVenomous`, `RiskLevel`
- ✅ Data persisted in `ReportMedia` and `SnakeAIRecognitionResult` tables

---

## TASK 5: Response Optimization (Phase 2.1)

### 5.1 Redesign Response (V3 Strict)

Implement the response structure exactly matching the JSON below. Do not omit any fields.

**File:** `SnakeAid.Core/Responses/SnakeDetection/SnakeDetectionResponse.cs`

```json
{
  // 1. GLOBAL METADATA (Nguồn: FastAPI)
  "ai_metadata": {
    "model_version": "snake-yolo12-v1.0",
    "image_width": 1280,
    "image_height": 720,
    "detection_count": 1,
    "warnings": {
      "blur": 0.05,        // Cảnh báo ảnh mờ
      "brightness": 0.45,  // Độ sáng
      "too_small": 0.0     // Vật thể quá nhỏ
    }
  },

  // 2. DETECTION RESULTS list
  "results": [
    {
      // 2.1 AI INFO (Nguồn: FastAPI - Specific Detection)
      "ai_detection": {
        "class_id": 0,
        "class_name": "king_cobra",
        "confidence": 0.94,
        "bbox": {
          "x1": 100, "y1": 200, "x2": 300, "y2": 400
        }
      },

      // 2.2 ENTITY INFO (Nguồn: SnakeSpecies.cs - Full Entity)
      "snake": {
        // --- Scalar Fields ---
        "id": 101,
        "scientificName": "Ophiophagus hannah",
        "commonName": "Rắn hổ mang chúa",
        "slug": "ran-ho-mang-chua",
        "imageUrl": "https://...",
        "description": "Loài rắn độc lớn nhất thế giới...",
        "identificationSummary": "Cổ bành rộng, mắt đen, vảy trơn...",
        "isVenomous": true,
        "riskLevel": 9.5,
        "isActive": true,

        // --- JSONB Fields (Lưu ý: Map trực tiếp Object/List, không được stringify thủ công) ---
        "identification": { // [JSONB] Maps to 'Identification'
          "physicalTraits": ["Cổ bành", "Mắt đen"],
          "behaviors": ["Chủ động tấn công khi bị đe dọa"],
          "habitat": "Rừng nhiệt đới, khu dân cư ven rừng"
        },
        
        "symptomsByTime": [ // [JSONB] Maps to 'SymptomsByTime'
          { 
             "timeRange": "0-15p", 
             "signs": ["Đau buốt", "Sưng to"], 
             "isCritical": false 
          }
        ],

        // --- SPECIAL LOGIC: FIRST AID ---
        // Nếu DB null -> Tự động điền từ VenomType (Fallback)
        "firstAidGuidelineOverride": { // [JSONB] Maps to 'FirstAidGuidelineOverride'
          "mode": 0, // Append/Replace
          "steps": [
             "Trấn an nạn nhân", 
             "Bất động chi bị cắn"
          ]
        },

        // --- Navigation Props (Có thể null để tránh loop/heavy payload) ---
        "primaryVenomType": 0 // Enum Value
      }
    }
  ]
}
```

### 5.2 Implementation Requirements

1.  **DTO Structure**:
    *   `SnakeDetectionResponse`: Root object.
    *   `AiMetadata`: Maps to `ai_metadata`.
    *   `DetectionResult`: Item in `results`.
    *   `AiDetection`: Maps to `ai_detection`.
    *   `SnakeSpecies`: Maps to `snake` (Reuse existing Entity or DTO, but ensure all fields are present).

2.  **First Aid Logic**:
    *   Field name must be `firstAidGuidelineOverride`.
    *   **Logic**: If `SnakeSpecies.FirstAidGuidelineOverride` is null, look up `SpeciesVenoms -> VenomType -> FirstAidGuideline`.
    *   If found, create a temporary `FirstAidOverride` object with `Mode = 0` (Append) and populate `Steps`.

3.  **Serialization**:
    *   Ensure Enums serialize as Integers (or Strings if requested, but JSON shows `0` for Mode). *Correction from previous plan: JSON above shows `0` for Mode and `0` for primaryVenomType. Use default int serialization unless specified otherwise.*
    *   Use `[JsonPropertyName("...")]` to match snake_case keys exactly.

