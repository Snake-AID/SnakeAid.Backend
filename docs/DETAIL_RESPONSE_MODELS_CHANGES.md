# Detail Response Models - Changes Documentation

Mô tả các thay đổi được thêm vào response models `DetailSnakebiteIncidentResponse` và `DetailRescueMissionResponse` để hỗ trợ media loading riêng cho rescue mission.

---

## Tổng Quan Thay Đổi

### Vấn đề Cũ

- Media của incident và media của rescue mission được lưu chung, khiến khó phân biệt
- Không thể lấy tất cả media của một rescue mission cụ thể
- Frontend phải xử lý logic phức tạp để filter media

### Giải Pháp Mới

- **Tách riêng Media streams**: Incident media vs. Rescue mission media
- **Load all mission media**: Không lọc theo Purpose, lấy toàn bộ media của mission
- **Grouped structure**: Media được nhóm theo mission (nếu call detail incident)

---

## 1. DetailSnakebiteIncidentResponse

### Cấu trúc

```csharp
public class DetailSnakebiteIncidentResponse
{
    // ... existing fields ...
    
    /// Media uploaded for the incident itself (all purposes, no filter)
    public List<SnakeAIDetectMediaResponse> Media { get; set; }
    
    /// Grouped media uploaded by each rescue mission for this incident
    public List<RescueMissionMediaGroupResponse> RescueMissionMedia { get; set; }
}
```

### Trường Mới

| Trường | Type | Description |
|--------|------|-------------|
| `Media` | `List<SnakeAIDetectMediaResponse>` | **Media của incident** (hình ảnh member chụp khi báo cáo) |
| `RescueMissionMedia` | `List<RescueMissionMediaGroupResponse>` | **Media nhóm theo mission** (hình ảnh rescuer chụp khi thực hiện cứu hộ) |

### Example Response

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Finished",
  "address": "District 1, Ho Chi Minh City",
  "media": [
    {
      "id": "560e8400-e29b-41d4-a716-446655440001",
      "mediaUrl": "https://res.cloudinary.com/...",
      "referenceType": "SnakebiteIncident",
      "purpose": "SnakeIdentification",
      "isProcessed": true,
      "processedAt": "2026-04-05T10:35:00+07:00",
      "sequenceOrder": 1,
      "detectedSpecies": [
        {
          "id": 1,
          "scientificName": "Naja siamensis",
          "commonName": "Monocled Cobra",
          "slug": "monocled-cobra"
        }
      ]
    }
  ],
  "rescueMissionMedia": [
    {
      "missionId": "660e8400-e29b-41d4-a716-446655440000",
      "missionStatus": "MissionCompleted",
      "media": [
        {
          "id": "570e8400-e29b-41d4-a716-446655440002",
          "mediaUrl": "https://res.cloudinary.com/...",
          "referenceType": "RescueMission",
          "purpose": "Evidence",
          "fileName": "rescue-scene-01.jpg",
          "contentType": "image/jpeg",
          "fileSize": 2048000,
          "requiresAIProcessing": false
        },
        {
          "id": "580e8400-e29b-41d4-a716-446655440003",
          "mediaUrl": "https://res.cloudinary.com/...",
          "referenceType": "RescueMission",
          "purpose": "Evidence",
          "fileName": "rescue-scene-02.jpg",
          "contentType": "image/jpeg",
          "fileSize": 1856000,
          "requiresAIProcessing": false
        }
      ]
    }
  ]
}
```

### RescueMissionMediaGroupResponse

```csharp
public class RescueMissionMediaGroupResponse
{
    /// ID của rescue mission
    public Guid MissionId { get; set; }
    
    /// Trạng thái của mission
    public RescueMissionStatus MissionStatus { get; set; }
    
    /// Danh sách media của mission này (toàn bộ media, không lọc Purpose)
    public List<ReportMediaResponse> Media { get; set; }
}
```

**Lưu ý:** `RescueMissionMedia` được nhóm theo mission, cho phép frontend biết:

- Hình ảnh nào từ mission nào
- Mission ở trạng thái nào khi upload media

---

## 2. DetailRescueMissionResponse

### Cấu trúc

```csharp
public class DetailRescueMissionResponse
{
    // ... existing fields ...
    
    /// Media uploaded for this rescue mission (all purposes).
    public List<ReportMediaResponse> MissionMedia { get; set; }
    
    // Nested incident info
    public BriefIncidentResponse Incident { get; set; }
}
```

### Trường Mới

| Trường | Type | Description |
|--------|------|-------------|
| `MissionMedia` | `List<ReportMediaResponse>` | **Toàn bộ media của mission** (không lọc Purpose) |

### Example Response

```json
{
  "id": "660e8400-e29b-41d4-a716-446655440000",
  "incidentId": "550e8400-e29b-41d4-a716-446655440000",
  "rescuerId": "550e8400-e29b-41d4-a716-446655440001",
  "status": "MissionCompleted",
  "price": 500000,
  "actualCost": 450000,
  "createdAt": "2026-04-05T10:35:00+07:00",
  "startedAt": "2026-04-05T10:45:00+07:00",
  "arrivedAt": "2026-04-05T11:00:00+07:00",
  "completedAt": "2026-04-05T11:30:00+07:00",
  "missionMedia": [
    {
      "id": "570e8400-e29b-41d4-a716-446655440002",
      "mediaUrl": "https://res.cloudinary.com/...",
      "fileName": "rescue-evidence-01.jpg",
      "contentType": "image/jpeg",
      "fileSize": 2048000,
      "referenceType": "RescueMission",
      "purpose": "Evidence",
      "requiresAIProcessing": false
    },
    {
      "id": "580e8400-e29b-41d4-a716-446655440003",
      "mediaUrl": "https://res.cloudinary.com/...",
      "fileName": "rescue-evidence-02.jpg",
      "contentType": "image/jpeg",
      "fileSize": 1856000,
      "referenceType": "RescueMission",
      "purpose": "Evidence",
      "requiresAIProcessing": false
    }
  ],
  "incident": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "status": "Finished",
    "address": "District 1, Ho Chi Minh City",
    "severityLevel": 3,
    "identifiedSnake": {
      "id": 1,
      "scientificName": "Naja siamensis",
      "commonName": "Monocled Cobra"
    },
    "media": [
      {
        "id": "560e8400-e29b-41d4-a716-446655440001",
        "mediaUrl": "https://res.cloudinary.com/...",
        "referenceType": "SnakebiteIncident",
        "purpose": "SnakeIdentification",
        "isProcessed": true,
        "detectedSpecies": [...]
      }
    ]
  }
}
```

### BriefIncidentResponse

```csharp
public class BriefIncidentResponse
{
    public Guid Id { get; set; }
    public GeoPointResponse LocationCoordinates { get; set; }
    public string? Address { get; set; }
    public SnakebiteIncidentStatus Status { get; set; }
    public List<ReportSymptom>? SymptomsReport { get; set; }
    public int? SeverityLevel { get; set; }
    public DateTime? IncidentOccurredAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public SnakeSpeciesResponse? IdentifiedSnake { get; set; }
    public SnakeIdentificationContext? IdentificationContext { get; set; }
    
    // Media của incident
    public List<SnakeAIDetectMediaResponse> Media { get; set; }
}
```

---

## 3. Media Response Models

### ReportMediaResponse

Được sử dụng khi cần **thông tin chi tiết về file media**:

```csharp
public class ReportMediaResponse
{
    public Guid Id { get; set; }
    
    /// URL công khai để download/display media
    public string MediaUrl { get; set; }
    
    /// Tên file gốc
    public string FileName { get; set; }
    
    /// MIME type (image/jpeg, image/png, video/mp4, etc.)
    public string ContentType { get; set; }
    
    /// Kích thước file (bytes)
    public long FileSize { get; set; }
    
    /// Loại reference (SnakebiteIncident, RescueMission, CommunityReport)
    public MediaReferenceType ReferenceType { get; set; }
    
    /// Mục đích (SnakeIdentification, Evidence, Documentation)
    public MediaPurpose Purpose { get; set; }
    
    /// Có cần xử lý AI không
    public bool RequiresAIProcessing { get; set; }
}
```

**Sử dụng khi:**

- Hiển thị danh sách media với chi tiết file
- Download/xem media
- Hiển thị metadata (kích thước, loại MIME)

---

### SnakeAIDetectMediaResponse

Được sử dụng khi cần **thông tin về AI detection**:

```csharp
public class SnakeAIDetectMediaResponse
{
    public Guid Id { get; set; }
    
    /// URL media
    public string MediaUrl { get; set; }
    
    public MediaReferenceType ReferenceType { get; set; }
    
    public MediaPurpose Purpose { get; set; }
    
    /// Đã xử lý AI chưa
    public bool IsProcessed { get; set; }
    
    /// Thời điểm xử lý hoàn thành
    public DateTime? ProcessedAt { get; set; }
    
    /// Thứ tự sắp xếp ảnh
    public int? SequenceOrder { get; set; }
    
    /// Danh sách loài rắn phát hiện được (nếu có)
    public List<SnakeSpeciesResponse> DetectedSpecies { get; set; }
}
```

**Sử dụng khi:**

- Hiển thị ảnh phát hiện rắn
- Hiển thị danh sách loài rắn được AI nhận diện
- Cần thông tin về xử lý AI

---

## 4. Enum Values

### MediaReferenceType

```
SnakebiteIncident = 0    // Media của incident
RescueMission = 1        // Media của rescue mission
SnakeCatchingMission = 2 // Media của snake catching mission
CommunityReport = 3      // Media của community report
```

### MediaPurpose

```
Evidence = 0              // Bằng chứng
SnakeIdentification = 1   // Xác định loài rắn
Documentation = 2        // Tài liệu
Community = 3            // Báo cáo cộng đồng
```

### RescueMissionStatus

```
Preparing = 0
EnRoute = 1
RescuerArrived = 2
MissionCompleted = 3
Cancelled = 4
MissionAborted = 5
```

---

## 5. Frontend Usage Examples

### Lấy detail incident với tất cả media

```typescript
const response = await fetch('/api/snakebite-incidents/{incidentId}', {
  headers: { Authorization: `Bearer ${token}` }
});

const incident = await response.json();

// Hiển thị incident media (ảnh member chụp)
incident.data.media.forEach(m => {
  console.log(`${m.mediaUrl} - Detected: ${m.detectedSpecies.length} species`);
});

// Hiển thị mission media (ảnh rescuer chụp), nhóm theo mission
incident.data.rescueMissionMedia.forEach(group => {
  console.log(`Mission ${group.missionId} (${group.missionStatus})`);
  group.media.forEach(m => {
    console.log(`  - ${m.fileName} (${m.contentType})`);
  });
});
```

### Lấy detail mission

```typescript
const response = await fetch('/api/rescue-missions/{missionId}', {
  headers: { Authorization: `Bearer ${token}` }
});

const mission = await response.json();

// Tất cả media của mission
mission.data.missionMedia.forEach(m => {
  console.log(m.mediaUrl);
});

// Incident media (nested)
mission.data.incident.media.forEach(m => {
  console.log(`Detected species: ${m.detectedSpecies.map(s => s.commonName)}`);
});
```

### Phân loại media theo Purpose

```typescript
const evidenceMedia = mission.missionMedia.filter(m => m.purpose === 'Evidence');
const documentationMedia = mission.missionMedia.filter(m => m.purpose === 'Documentation');
```

### Tính toán kích thước media

```typescript
const totalSize = mission.missionMedia.reduce((sum, m) => sum + m.fileSize, 0);
const totalSizeMB = totalSize / (1024 * 1024);
console.log(`Total media size: ${totalSizeMB.toFixed(2)} MB`);
```

---

## 6. Migration Notes

### Thay đổi từ phiên bản cũ

**Trước:**

```json
{
  "media": [...] // Lẫn lộn incident + mission media
}
```

**Sau:**

```json
{
  "media": [...],              // Chỉ incident media
  "rescueMissionMedia": [...]  // Mission media, nhóm theo mission
}
```

### Breaking Changes

- Nếu frontend dùng `response.data.media` để hiển thị tất cả media, **cần update** để xử lý cả `rescueMissionMedia`
- Tên field không thay đổi, chỉ **thêm** field mới

### Compatibility

- ✅ Backward compatible - field `media` vẫn tồn tại
- ✅ Field mới không bắt buộc khi không cần (null/empty list)
- ✅ Không cần migration database

---

## 7. Performance Considerations

### Loading Strategy

- **Incident detail**: Tải toàn bộ media (incident + mission media)
- **Mission detail**: Tải mission media + nested incident media
- **Admin list**: Không tải media để tiết kiệm bandwidth

### Optimization Tips

1. **Lazy load images**: Dùng `loading="lazy"` cho `<img>` tags
2. **Paginate media**: Nếu media quá nhiều, paginate thay vì load tất cả
3. **Thumbnail**: Yêu cầu API return thumbnail URL thay vì full size

---

## 8. API Endpoints Liên Quan

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/snakebite-incidents/{id}` | GET | Lấy detail incident + media |
| `/api/rescue-missions/{id}` | GET | Lấy detail mission + media |
| `/api/incidents/admin/list` | GET | Lấy danh sách incident (không có media) |
| `/api/rescue-missions/admin/list` | GET | Lấy danh sách mission (không có media) |

---

**Last Updated**: 2026-04-05  
**API Version**: v1
