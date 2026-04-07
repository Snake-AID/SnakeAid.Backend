# AI Recognition UI Contract

Tai lieu nay tong hop API va model can cho UI implement luong AI nhan dien rắn:

- Admin xem danh sach report media co AI recognition.
- Expert xem review queue de verify / reject ket qua confidence thap.

Khong bao gom dataset management day du hay statistics module.

---

## 1. SystemSetting

### Low confidence threshold

- `SettingKey`: `AI.Recognition.LowConfidenceThreshold`
- `ValueType`: `Decimal`
- `Value`: `0 -> 1` (vi du `0.70`)
- Muc dich: xac dinh recognition nao vao review queue cua expert.

Expert review luon bat mac dinh, khong co key enable/disable.

---

## 2. Shared response models

Tat ca endpoint van tra theo wrapper chung:

```json
{
  "status_code": 200,
  "message": "...",
  "is_success": true,
  "data": {}
}
```

### 2.1 SnakeSpeciesResponse

Model species dung chung trong response AI result.

```json
{
  "id": 12,
  "scientificName": "Naja kaouthia",
  "slug": "ran-ho-mang-mot-mat-kinh",
  "commonName": "Ran ho mang mot mat kinh",
  "imageUrl": "https://...",
  "description": "...",
  "identificationSummary": "...",
  "primaryVenomType": "Neurotoxic",
  "riskLevel": 8.5,
  "isVenomous": true,
  "isActive": true
}
```

### 2.2 ReportMediaResponse

Model media goc dung cho queue expert.

```json
{
  "id": "guid",
  "mediaUrl": "https://...",
  "fileName": "snake-01.jpg",
  "contentType": "image/jpeg",
  "fileSize": 2048000,
  "referenceType": "SnakeCatchingRequest",
  "purpose": "SnakeIdentification",
  "requiresAIProcessing": true
}
```

### 2.3 SnakeAIRecognitionResultResponse

AI result dung trong queue expert.

```json
{
  "id": "guid",
  "reportMediaId": "guid",
  "yoloClassName": "cobra",
  "confidence": 0.64,
  "detectedSpeciesId": 12,
  "isMapped": true,
  "status": "Completed",
  "detectedSpecies": {
    "id": 12,
    "scientificName": "Naja kaouthia",
    "slug": "ran-ho-mang-mot-mat-kinh",
    "commonName": "Ran ho mang mot mat kinh",
    "imageUrl": "https://...",
    "description": "...",
    "identificationSummary": "...",
    "primaryVenomType": "Neurotoxic",
    "riskLevel": 8.5,
    "isVenomous": true,
    "isActive": true
  }
}
```

---

## 3. Request models

### 3.1 ExpertVerifyRecognitionRequest

```json
{
  "correctedSpeciesId": 14,
  "expertNotes": "Pattern and head shape are consistent with species #14"
}
```

Field notes:

- `correctedSpeciesId` bat buoc.
- `expertNotes` khong bat buoc.

### 3.2 ExpertRejectRecognitionRequest

```json
{
  "expertNotes": "Image too blurry for reliable species labeling"
}
```

Field notes:

- `expertNotes` khong bat buoc.

---

## 4. Admin API

### 4.1 GET `/api/admin/ai-recognition-report-media`

Muc dich:

- Admin xem danh sach report media co ket qua AI recognition.
- Muc tieu la audit nhanh media, AI species va expert species.

Auth:

- `Admin`

Query params:

| Param | Type | Required | Ghi chu |
|---|---|---:|---|
| `page` | int | ❌ | Mac dinh `1` |
| `pageSize` | int | ❌ | Mac dinh `20` |
| `status` | string | ❌ | Loc theo `RecognitionStatus` |
| `minConfidence` | decimal | ❌ | Loc nguong confidence thap nhat |
| `maxConfidence` | decimal | ❌ | Loc nguong confidence cao nhat |
| `referenceType` | string | ❌ | Loc theo loai reference |
| `from` | datetimeoffset | ❌ | Loc theo createdAt tu ngay |
| `to` | datetimeoffset | ❌ | Loc theo createdAt den ngay |

Response:

- `ApiResponse<PagedData<AIRecognitionAdminReportMediaListItemResponse>>`

### 4.2 Admin list item model: AIRecognitionAdminReportMediaListItemResponse

```json
{
  "recognitionResultId": "guid",
  "reportMediaId": "guid",
  "mediaUrl": "https://...",
  "contentType": "image/jpeg",
  "referenceId": "guid",
  "referenceType": "SnakeCatchingRequest",
  "purpose": "SnakeIdentification",
  "aiModelId": 1,
  "yoloClassName": "cobra",
  "confidence": 0.64,
  "detectedSpecies": {
    "id": 12,
    "commonName": "Ran ho mang"
  },
  "expertCorrectedSpecies": {
    "id": 14,
    "commonName": "Ran ho mang chua"
  },
  "expertReviewerName": "Dr. A",
  "status": "ExpertVerified",
  "needsExpertReview": false,
  "expertVerifiedAt": "2026-04-07T10:05:00Z",
  "createdAt": "2026-04-07T09:20:00Z",
  "updatedAt": "2026-04-07T10:05:00Z"
}
```

Field notes:

- `detectedSpecies`: species AI map duoc.
- `expertCorrectedSpecies`: species expert chon neu da review.
- `expertReviewerName`: ten expert da verify/reject.
- `needsExpertReview`: true neu confidence thap va chua duoc expert xu ly.

---

## 5. Enum Types (Shared)

### 5.1 RecognitionStatus

Dung cho:

- Query param `status` cua admin list endpoint.
- Field `status` trong `SnakeAIRecognitionResultResponse` va `AIRecognitionAdminReportMediaListItemResponse`.

```csharp
public enum RecognitionStatus
{
  Processing = 0,
  Completed = 1,
  Failed = 2,
  ExpertVerified = 3,
  ExpertRejected = 4
}
```

### 5.2 MediaReferenceType

Dung cho field `referenceType` trong `ReportMediaResponse` va admin list item.

```csharp
public enum MediaReferenceType
{
  CommunityReport = 0,
  SnakebiteIncident = 1,
  RescueMission = 2,
  SnakeCatchingRequest = 3,
  SnakeCatchingMission = 4
}
```

### 5.3 MediaPurpose

Dung cho field `purpose` trong `ReportMediaResponse` va admin list item.

```csharp
public enum MediaPurpose
{
  Evidence = 0,
  SnakeIdentification = 1,
  LocationProof = 2,
  InjuryPhoto = 3,
  BeforeAfter = 4,
  SnakeOthers = 5
}
```
