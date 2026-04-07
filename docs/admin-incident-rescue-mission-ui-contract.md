# Admin Incident & Rescue Mission UI Contract

Tài liệu này tổng hợp JSON model cho 4 màn admin chính:

1. Incident list
2. Incident detail
3. Rescue mission list
4. Rescue mission detail

Mục tiêu là để UI implement trực tiếp mà không cần đọc backend code.

---

## 1. Admin Incident List

### Endpoint

`GET /api/incidents/admin/list`

### Response Model

`PagedData<OperatorIncidentSummaryResponse>`

### JSON Shape

```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "status": "Assigned",
      "locationCoordinates": {
        "latitude": 10.77689,
        "longitude": 106.70098
      },
      "createdAt": "2026-04-05T10:30:00+07:00",
      "address": "District 1, Ho Chi Minh City",
      "assignedRescuerId": "550e8400-e29b-41d4-a716-446655440001",
      "assignedRescuerName": "Nguyen Van A",
      "activeMissionStatus": "EnRoute",
      "needsRedispatch": false,
      "handlingOperatorId": "550e8400-e29b-41d4-a716-446655440002",
      "handlingOperatorName": "Tran Thi B"
    }
  ],
  "meta": {
    "total_items": 150,
    "total_pages": 3,
    "current_page": 1,
    "page_size": 50
  }
}
```

### Field Notes

| Field | Type | Notes |
|-------|------|-------|
| `id` | uuid | Incident ID |
| `status` | string | `SnakebiteIncidentStatus` |
| `locationCoordinates` | object | Geo point `{ latitude, longitude }` |
| `createdAt` | datetime | Thời điểm tạo incident |
| `address` | string | Địa chỉ text |
| `assignedRescuerId` | uuid/null | Rescuer đang được assign |
| `assignedRescuerName` | string/null | Tên rescuer đang được assign |
| `activeMissionStatus` | string/null | Mission hiện tại nếu có |
| `needsRedispatch` | boolean | Có cần điều phối lại không |
| `handlingOperatorId` | uuid/null | Operator đang claim case |
| `handlingOperatorName` | string/null | Tên operator đang xử lý |

---

## 2. Admin Incident Detail

### Endpoint

`GET /api/incidents/admin/{incidentId}`

### Response Model

`AdminDetailSnakebiteIncidentResponse`

### JSON Shape

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "locationCoordinates": {
    "latitude": 10.77689,
    "longitude": 106.70098
  },
  "address": "District 1, Ho Chi Minh City",
  "symptomsReport": [
    {
      "symptomId": 1,
      "symptomName": "Pain",
      "symptomDescription": "Severe pain at bite site"
    }
  ],
  "status": "Verified",
  "createdAt": "2026-04-05T10:30:00+07:00",
  "handlingOperatorId": "550e8400-e29b-41d4-a716-446655440002",
  "handlingOperatorName": "Tran Thi B",
  "operatorNotes": "Called member twice, no answer on first call.",
  "dispatchedAt": "2026-04-05T10:45:00+07:00",
  "confirmedAt": "2026-04-05T10:35:00+07:00",
  "assignedAt": "2026-04-05T10:48:00+07:00",
  "assignedRescuerId": "550e8400-e29b-41d4-a716-446655440001",
  "cancellationReason": null,
  "severityLevel": 3,
  "incidentOccurredAt": "2026-04-05T10:20:00+07:00",
  "identifiedSnake": {
    "id": 1,
    "scientificName": "Naja siamensis",
    "commonName": "Monocled Cobra",
    "slug": "monocled-cobra",
    "imageUrl": "https://..."
  },
  "identificationContext": {
    "method": "AIDetection",
    "identifiedAt": "2026-04-05T10:40:00+07:00",
    "aiConfidence": 0.93
  },
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440010",
    "fullName": "Pham Van C"
  },
  "assignedRescuer": {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "fullName": "Nguyen Van A"
  },
  "totalRescueAttempts": 3,
  "failedAttemptsCount": 1,
  "totalDispatchRequests": 4,
  "acceptedDispatchCount": 1,
  "declinedDispatchCount": 2,
  "cancelledDispatchCount": 1,
  "incidentMedia": [
    {
      "id": "560e8400-e29b-41d4-a716-446655440011",
      "mediaUrl": "https://res.cloudinary.com/...",
      "fileName": "snake-1.jpg",
      "contentType": "image/jpeg",
      "fileSize": 2048000,
      "referenceType": "SnakebiteIncident",
      "purpose": "SnakeIdentification",
      "requiresAIProcessing": true
    }
  ],
  "paymentSummary": {
    "payOsOrderCode": 17200012345,
    "paymentState": "Paid",
    "paidAmount": 450000,
    "paidAt": "2026-04-05T12:30:00+07:00",
    "paymentMethod": "PayOS",
    "paymentExternalTransactionId": "payos_tx_123",
    "totalRefundedAmount": 0,
    "latestRefundedAt": null
  },
  "missionHistory": [
    {
      "missionId": "660e8400-e29b-41d4-a716-446655440020",
      "rescuerId": "550e8400-e29b-41d4-a716-446655440001",
      "rescuerName": "Nguyen Van A",
      "rescuerPhone": "0909000001",
      "status": "MissionCompleted",
      "price": 500000,
      "actualCost": 450000,
      "createdAt": "2026-04-05T10:48:00+07:00",
      "startedAt": "2026-04-05T10:50:00+07:00",
      "arrivedAt": "2026-04-05T11:05:00+07:00",
      "completedAt": "2026-04-05T11:35:00+07:00",
      "notes": "Handled with caution",
      "cancellationReason": null,
      "media": [
        {
          "id": "570e8400-e29b-41d4-a716-446655440021",
          "mediaUrl": "https://res.cloudinary.com/...",
          "fileName": "evidence-1.jpg",
          "contentType": "image/jpeg",
          "fileSize": 1856000,
          "referenceType": "RescueMission",
          "purpose": "Evidence",
          "requiresAIProcessing": false
        }
      ]
    }
  ],
  "dispatchRequests": [
    {
      "requestId": "770e8400-e29b-41d4-a716-446655440030",
      "rescuerId": "550e8400-e29b-41d4-a716-446655440001",
      "rescuerName": "Nguyen Van A",
      "rescuerPhone": "0909000001",
      "operatorId": "550e8400-e29b-41d4-a716-446655440002",
      "operatorName": "Tran Thi B",
      "status": "Pending",
      "dispatchedAt": "2026-04-05T10:45:00+07:00",
      "responseAt": null,
      "declineReason": null
    }
  ]
}
```

### Field Notes

| Field | Type | Notes |
|-------|------|-------|
| `handlingOperatorId` | uuid/null | Operator đang claim case |
| `handlingOperatorName` | string/null | Tên operator đang xử lý |
| `operatorNotes` | string/null | Ghi chú điều phối của operator |
| `dispatchedAt` | datetime/null | Thời điểm dispatch |
| `confirmedAt` | datetime/null | Thời điểm incident được confirm |
| `incidentMedia` | array | Media của member gửi để xác minh rắn |
| `missionHistory` | array | Toàn bộ mission history của incident |
| `missionHistory[].media` | array | Media evidence của rescuer theo mission |
| `dispatchRequests` | array | Toàn bộ lịch sử dispatch request |
| `paymentSummary` | object | Tóm tắt payment audit theo incident |

---

## 3. Admin Rescue Mission List

### Endpoint

`GET /api/rescue-missions/admin/list`

### Response Model

`PagedData<AdminRescueMissionSummaryResponse>`

### JSON Shape

```json
{
  "items": [
    {
      "id": "660e8400-e29b-41d4-a716-446655440020",
      "incidentId": "550e8400-e29b-41d4-a716-446655440000",
      "rescuerId": "550e8400-e29b-41d4-a716-446655440001",
      "status": "MissionCompleted",
      "price": 500000,
      "actualCost": 450000,
      "costFromCenter": 150000,
      "createdAt": "2026-04-05T10:48:00+07:00",
      "updatedAt": "2026-04-05T11:35:00+07:00",
      "startedAt": "2026-04-05T10:50:00+07:00",
      "arrivedAt": "2026-04-05T11:05:00+07:00",
      "completedAt": "2026-04-05T11:35:00+07:00",
      "incidentStatus": "Finished",
      "incidentAddress": "District 1, Ho Chi Minh City",
      "rescuerName": "Nguyen Van A"
    }
  ],
  "meta": {
    "total_items": 32,
    "total_pages": 1,
    "current_page": 1,
    "page_size": 50
  }
}
```

### Field Notes

| Field | Type | Notes |
|-------|------|-------|
| `id` | uuid | Mission ID |
| `incidentId` | uuid | Incident ID liên quan |
| `rescuerId` | uuid | Rescuer được gán |
| `status` | string | `RescueMissionStatus` |
| `price` | decimal | Giá mission |
| `actualCost` | decimal/null | Chi phí thực tế nếu có |
| `costFromCenter` | decimal/null | Chi phí tính từ trung tâm |
| `createdAt` | datetime | Thời điểm tạo mission |
| `updatedAt` | datetime/null | Thời điểm cập nhật gần nhất |
| `startedAt` | datetime/null | Bắt đầu di chuyển |
| `arrivedAt` | datetime/null | Đã đến hiện trường |
| `completedAt` | datetime/null | Hoàn thành mission |
| `incidentStatus` | string | Trạng thái incident tại thời điểm list |
| `incidentAddress` | string | Địa chỉ incident |
| `rescuerName` | string/null | Tên rescuer |

---

## 4. Mission Detail

### Endpoint

`GET /api/rescue-missions/{missionId}`

### Response Model

`DetailRescueMissionResponse`

### JSON Shape

```json
{
  "id": "660e8400-e29b-41d4-a716-446655440020",
  "incidentId": "550e8400-e29b-41d4-a716-446655440000",
  "rescuerId": "550e8400-e29b-41d4-a716-446655440001",
  "status": "MissionCompleted",
  "price": 500000,
  "createdAt": "2026-04-05T10:48:00+07:00",
  "startedAt": "2026-04-05T10:50:00+07:00",
  "arrivedAt": "2026-04-05T11:05:00+07:00",
  "completedAt": "2026-04-05T11:35:00+07:00",
  "updatedAt": "2026-04-05T11:36:00+07:00",
  "notes": "Handled with caution",
  "cancellationReason": null,
  "distanceFromCenterKm": 12.5,
  "costFromCenter": 150000,
  "actualCost": 450000,
  "distanceKm": 8.2,
  "missionMedia": [
    {
      "id": "570e8400-e29b-41d4-a716-446655440021",
      "mediaUrl": "https://res.cloudinary.com/...",
      "fileName": "evidence-1.jpg",
      "contentType": "image/jpeg",
      "fileSize": 1856000,
      "referenceType": "RescueMission",
      "purpose": "Evidence",
      "requiresAIProcessing": false
    }
  ],
  "incident": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "locationCoordinates": {
      "latitude": 10.77689,
      "longitude": 106.70098
    },
    "address": "District 1, Ho Chi Minh City",
    "status": "Finished",
    "symptomsReport": [
      {
        "symptomId": 1,
        "symptomName": "Pain",
        "symptomDescription": "Severe pain at bite site"
      }
    ],
    "severityLevel": 3,
    "incidentOccurredAt": "2026-04-05T10:20:00+07:00",
    "assignedAt": "2026-04-05T10:48:00+07:00",
    "identifiedSnake": {
      "id": 1,
      "scientificName": "Naja siamensis",
      "commonName": "Monocled Cobra",
      "slug": "monocled-cobra",
      "imageUrl": "https://..."
    },
    "identificationContext": {
      "method": "AIDetection",
      "identifiedAt": "2026-04-05T10:40:00+07:00",
      "aiConfidence": 0.93
    },
    "media": [
      {
        "id": "560e8400-e29b-41d4-a716-446655440011",
        "mediaUrl": "https://res.cloudinary.com/...",
        "fileName": "snake-1.jpg",
        "contentType": "image/jpeg",
        "fileSize": 2048000,
        "referenceType": "SnakebiteIncident",
        "purpose": "SnakeIdentification",
        "requiresAIProcessing": true
      }
    ]
  },
  "rescuer": {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "fullName": "Nguyen Van A"
  },
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440010",
    "fullName": "Pham Van C"
  }
}
```

### Field Notes

| Field | Type | Notes |
|-------|------|-------|
| `missionMedia` | array | Toàn bộ media của mission |
| `incident.media` | array | Media của incident/member |
| `distanceKm` | decimal/null | Khoảng cách rescuer tới incident, nếu có query location |
| `incident.identifiedSnake` | object/null | Loài rắn đã xác định |
| `incident.identificationContext` | object/null | Context xác định loài rắn |

---

## Ghi chú triển khai UI

1. **Incident list** là dashboard chính cho case management.
2. **Incident detail** là màn điều tra chính cho admin, chứa đầy đủ audit theo incident.
3. **Mission list** dùng cho tracking vận hành theo mission.
4. **Mission detail** phù hợp cho rescuer flow; nếu admin cần audit sâu hơn thì nên dùng incident detail trước.

---

## Response Wrapper

Các endpoint trên đều trả theo wrapper:

```json
{
  "success": true,
  "message": "...",
  "data": {
    "items": [],
    "meta": {}
  }
}
```

Hoặc với detail endpoint:

```json
{
  "success": true,
  "message": "...",
  "data": {}
}
```
