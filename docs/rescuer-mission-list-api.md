# Rescuer Mission List API

Tài liệu này mô tả endpoint lấy danh sách rescue mission của rescuer hiện tại.

---

## Endpoint

`GET /api/rescue-missions/rescuer/list`

## Authentication

- Yêu cầu `Bearer token`
- Chỉ dành cho role: `Rescuer`

## Mô tả

Lấy danh sách cứu hộ mission do rescuer hiện tại đảm nhận.

- Nếu không truyền `status`, trả về tất cả mission của rescuer.
- Nếu truyền `status`, lọc theo giá trị trạng thái mission.
- Dữ liệu trả về tương tự định dạng danh sách của snake catching request nhưng chỉ chứa thông tin mission.

## Request

### Query parameters

| Tên | Loại | Bắt buộc | Mô tả |
|------|------|----------|-------|
| `status` | `RescueMissionStatus` | Không | Lọc mission theo trạng thái. Ví dụ: `Preparing`, `EnRoute`, `RescuerArrived`, `MissionCompleted`, `MissionUncompleted`, `MissionAborted`, `Cancelled` |

### Ví dụ request

```
GET /api/rescue-missions/rescuer/list
Authorization: Bearer {token}
```

```
GET /api/rescue-missions/rescuer/list?status=Preparing
Authorization: Bearer {token}
```

## Response model

`ApiResponse<List<ListRescueMissionResponse>>`

### `ListRescueMissionResponse`

| Field | Loại | Mô tả |
|-------|------|-------|
| `id` | `uuid` | ID của mission |
| `incidentId` | `uuid` | ID incident liên quan |
| `rescuerId` | `uuid` | ID rescuer đang được gán |
| `status` | `RescueMissionStatus` | Trạng thái mission |
| `price` | `decimal` | Giá cơ bản của mission |
| `actualCost` | `decimal?` | Giá thực tế nếu đã tính hoặc ghi nhận |
| `costFromCenter` | `decimal?` | Phí từ trung tâm cứu hộ đến vị trí |
| `distanceFromCenterKm` | `decimal?` | Khoảng cách từ trung tâm cứu hộ đến incident (km) |
| `createdAt` | `datetime` | Thời điểm mission được tạo |
| `updatedAt` | `datetime?` | Thời điểm cập nhật cuối cùng |
| `startedAt` | `datetime?` | Thời điểm cứu hộ bắt đầu (EnRoute) |
| `arrivedAt` | `datetime?` | Thời điểm rescuer đến nơi |
| `completedAt` | `datetime?` | Thời điểm mission hoàn thành |
| `incidentStatus` | `SnakebiteIncidentStatus` | Trạng thái hiện tại của incident liên quan |
| `incidentAddress` | `string` | Địa chỉ incident |
| `notes` | `string?` | Ghi chú mission |

## Ví dụ response

```json
{
  "success": true,
  "message": "Retrieved 2 rescue mission(s) successfully.",
  "data": [
    {
      "id": "660e8400-e29b-41d4-a716-446655440020",
      "incidentId": "550e8400-e29b-41d4-a716-446655440000",
      "rescuerId": "550e8400-e29b-41d4-a716-446655440001",
      "status": "Preparing",
      "price": 500000.00,
      "actualCost": 530000.00,
      "costFromCenter": 30000.00,
      "distanceFromCenterKm": 6.00,
      "createdAt": "2026-04-23T09:10:00+07:00",
      "updatedAt": null,
      "startedAt": null,
      "arrivedAt": null,
      "completedAt": null,
      "incidentStatus": "Assigned",
      "incidentAddress": "123 Le Loi, District 1, HCMC",
      "notes": "Yêu cầu đến nhanh vì nạn nhân đau nhiều"
    },
    {
      "id": "660e8400-e29b-41d4-a716-446655440021",
      "incidentId": "550e8400-e29b-41d4-a716-446655440002",
      "rescuerId": "550e8400-e29b-41d4-a716-446655440001",
      "status": "EnRoute",
      "price": 500000.00,
      "actualCost": 520000.00,
      "costFromCenter": 20000.00,
      "distanceFromCenterKm": 4.00,
      "createdAt": "2026-04-23T08:40:00+07:00",
      "updatedAt": "2026-04-23T08:45:00+07:00",
      "startedAt": "2026-04-23T08:50:00+07:00",
      "arrivedAt": null,
      "completedAt": null,
      "incidentStatus": "Assigned",
      "incidentAddress": "45 Nguyen Hue, District 1, HCMC",
      "notes": null
    }
  ]
}
```

## Lưu ý

- `status` là optional. Nếu không truyền, API trả về tất cả mission của rescuer hiện tại.
- `rescuerId` được lấy từ token bằng helper `GetCurrentUserId()` ở backend.
- Frontend không cần truyền `rescuerId` trong body hoặc query.
