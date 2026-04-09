# Expert AI Recognition Review API

Tai lieu nay chi mo ta luong expert review cho AI recognition.
Muc tieu la de UI expert implement nhanh: lay queue, xem chi tiet, verify, reject.

---

## 1. Pham vi

Expert review duoc dung cho cac ReportMedia co AI confidence thap.
Nguong confidence lay tu SystemSetting:

- `AI.Recognition.LowConfidenceThreshold`
- `ValueType`: `Decimal`
- `Value`: `0 -> 1` (vi du `0.70`)

Expert review luon bat mac dinh, khong co config enable/disable.

---

## 2. Shared Response Wrapper

Tat ca endpoint tra ve theo wrapper chung:

```json
{
  "status_code": 200,
  "message": "...",
  "is_success": true,
  "data": {}
}
```

---

## 3. Request Models

### 3.1 `ExpertVerifyRecognitionRequest`

Dung cho action verify cua expert.

```json
{
  "correctedSpeciesId": 14,
  "expertNotes": "Pattern and head shape are consistent with species #14"
}
```

Field notes:

- `correctedSpeciesId`: bat buoc.
- `expertNotes`: khong bat buoc.

### 3.2 `ExpertRejectRecognitionRequest`

Dung cho action reject cua expert.

```json
{
  "expertNotes": "Image too blurry for reliable species labeling"
}
```

Field notes:

- `expertNotes`: khong bat buoc.

---

## 4. Response Models

### 4.1 `ReportMediaResponse`

Dung trong queue expert de hien thong tin anh.

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

Field notes:

- `requiresAIProcessing`: media co can AI xu ly hay khong.

### 4.2 `SnakeAIRecognitionResultResponse`

Dung trong queue expert de hien ket qua AI.

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

Field notes:

- `confidence`: diem tin cay cua AI.
- `detectedSpecies`: species ma AI map duoc, neu co.

### 4.3 `ExpertReviewItemResponse`

Dung cho queue list cua expert.

```json
{
  "media": {
    "id": "guid",
    "mediaUrl": "https://...",
    "fileName": "snake-01.jpg",
    "contentType": "image/jpeg",
    "fileSize": 2048000,
    "referenceType": "SnakeCatchingRequest",
    "purpose": "SnakeIdentification",
    "requiresAIProcessing": true
  },
  "aiResult": {
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
}
```

### 4.4 `AIRecognitionReviewDetailResponse`

Dung cho review detail cua expert.

```json
{
  "recognitionResultId": "guid",
  "media": {
    "id": "guid",
    "mediaUrl": "https://...",
    "fileName": "snake-01.jpg",
    "contentType": "image/jpeg",
    "fileSize": 2048000,
    "referenceType": "SnakeCatchingRequest",
    "purpose": "SnakeIdentification",
    "requiresAIProcessing": true
  },
  "aiResult": {
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
}
```

Field notes:

- `AIRecognitionReviewDetailResponse` da duoc toi gian cho UI review nhanh.
- UI chi can render anh (`media`) va ket qua AI (`aiResult`) de verify/reject.

### 4.5 `ExpertReviewActionResponse`

Dung sau khi expert verify/reject.

```json
{
  "recognitionResultId": "guid",
  "status": "ExpertVerified",
  "expertId": "guid",
  "expertVerifiedAt": "2026-04-07T10:05:00Z",
  "expertCorrectedSpeciesId": 14,
  "isTrainingReady": true
}
```

---

## 5. Enum Types

### 5.1 `RecognitionStatus`

Dung cho field `aiResult.status` va trang thai sau action verify/reject.

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

### 5.2 `MediaReferenceType`

Dung cho field `media.referenceType`.

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

### 5.3 `MediaPurpose`

Dung cho field `media.purpose`.

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

---

## 6. Endpoints

### 6.1 GET `/api/experts/ai-recognition/review-queue`

Muc dich:

- Lay danh sach item can expert review.
- UI chi can anh va AI result.

Auth:

- `Expert`

Query params:

| Param | Type | Required | Ghi chu |
|---|---|---:|---|
| `page` | int | ❌ | Mac dinh `1` |
| `pageSize` | int | ❌ | Mac dinh `20` |

Response:

- `ApiResponse<PagedData<ExpertReviewItemResponse>>`

---

### 6.2 GET `/api/experts/ai-recognition/review-queue/{recognitionResultId}`

Muc dich:

- Lay chi tiet 1 item cho context expert hien tai.
- Khong con gioi han chi trang thai `Completed`, de expert co the xem lai item da xu ly truoc do.

Auth:

- `Expert`

Response:

- `ApiResponse<AIRecognitionReviewDetailResponse>`

---

### 6.3 GET `/api/experts/ai-recognition/review-history`

Muc dich:

- Lay danh sach recognition ma expert hien tai da thuc hien (`ExpertVerified`/`ExpertRejected`).

Auth:

- `Expert`

Query params:

| Param | Type | Required | Ghi chu |
|---|---|---:|---|
| `page` | int | ❌ | Mac dinh `1` |
| `pageSize` | int | ❌ | Mac dinh `20` |

Response:

- `ApiResponse<PagedData<ExpertReviewedRecognitionItemResponse>>`

---

### 6.4 POST `/api/experts/ai-recognition/review-queue/{recognitionResultId}/verify`

Muc dich:

- Expert verify ket qua va chon species dung.

Auth:

- `Expert`

Request:

- `ExpertVerifyRecognitionRequest`

Response:

- `ApiResponse<ExpertReviewActionResponse>`

---

### 6.5 POST `/api/experts/ai-recognition/review-queue/{recognitionResultId}/reject`

Muc dich:

- Expert reject neu anh khong du dieu kien labeling.

Auth:

- `Expert`

Request:

- `ExpertRejectRecognitionRequest`

Response:

- `ApiResponse<ExpertReviewActionResponse>`

---

## 7. UI flow goi y

1. Load queue bang `GET /api/experts/ai-recognition/review-queue`.
2. Render card/list item tu `media.mediaUrl` va `aiResult`.
3. Khi click item, goi `GET /api/experts/ai-recognition/review-queue/{recognitionResultId}` de lay detail.
4. Neu expert chon species dung, goi `POST .../verify`.
5. Neu khong dung de labeling, goi `POST .../reject`.
6. De xem lai cong viec da xu ly, goi `GET /api/experts/ai-recognition/review-history`.

---

## 8. Changelog

- 2026-04-07: Tach doc rieng cho expert review flow.
- 2026-04-07: Them endpoint `review-history` va noi long detail de expert xem lai item da xu ly.
