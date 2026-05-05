# Operator Rescuer Selection Flow: Primary On-Duty + Backup Off-Duty

## Overview

Hệ thống này cung cấp **2 pool rescuer riêng biệt** để operator có thể:

- **Ưu tiên 1**: Chọn rescuer trong ca trực (on-duty pool) để dispatch chính.
- **Fallback 2**: Khi pool 1 không đủ, mở rộng sang rescuer online + available nhưng không nhất thiết trong ca trực (off-duty backup pool).

---

## 🎯 API Contract

### 1️⃣ **Primary: On-Duty Rescuers Pool**

#### Endpoint

```http
GET /api/monitoring/on-duty?incidentId=...&onlyAvailable=true&maxDistanceKm=...
```

#### Purpose

- Lấy **danh sách rescuer đang trong ca trực**
- Luôn **online** và **available**
- Sắp xếp theo **khoảng cách** từ incident

#### Query Parameters

| Param | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `incidentId` | Guid | Optional | - | Để tính distance và lọc rescuer đã từ chối/hủy bỏ ca này |
| `catchingRequestId` | Guid | Optional | - | Alternative: cho snake catching request |
| `onlyAvailable` | bool | No | `true` | Chỉ lấy rescuer available (không busy) |
| `maxDistanceKm` | double | No | - | Chỉ lấy rescuer trong vùng (VD: 10 km), không yêu cầu input |

#### Response (Success)

```json
{
  "statusCode": 200,
  "message": "On-duty rescuers snapshot",
  "data": {
    "contextId": "550e8400-e29b-41d4-a716-446655440000",
    "date": "2026-05-05",
    "snapshotAt": "2026-05-05T14:30:00Z",
    "rescuers": [
      {
        "rescuerId": "550e8400-e29b-41d4-a716-446655440000",
        "fullName": "Nguyễn Văn A",
        "phoneNumber": "0912345678",
        "isOnline": true,
        "isAvailable": true,
        "isOnDutyNow": true,
        "assignmentStatus": "Active",
        "shiftAssignmentId": "660f9511-f30c-52e5-b827-557766551111",
        "shiftId": "770g0622-g41d-63f6-c938-668877662222",
        "shiftName": "Morning Shift (6:00 - 14:00)",
        "shiftStartTime": "06:00:00",
        "shiftEndTime": "14:00:00",
        "shiftDate": "2026-05-05",
        "latitude": 10.8391267,
        "longitude": 106.8413534,
        "lastLocationUpdate": "2026-05-05T14:29:00Z",
        "distanceKm": 2.3
      }
    ]
  }
}
```

#### 📌 Key Fields Explained

- `isOnDutyNow`: ✅ **Luôn true** trong response này (định nghĩa của on-duty pool)
- `distanceKm`: Khoảng cách tính từ incident location
- `shiftName`, `shiftStartTime`, `shiftEndTime`: Thông tin ca trực để UI hiển thị

---

### 2️⃣ **Fallback: Off-Duty Backup Rescuers Pool**

#### Endpoint

```http
GET /api/monitoring/off-duty?incidentId=...&maxDistanceKm=...
```

#### Purpose

- Lấy **danh sách rescuer online + available nhưng KHÔNG trong ca trực**
- Dùng khi primary pool không đủ
- Operator có thể **liên lạc thủ công** để yêu cầu họ vào ca hoặc bật app

#### Query Parameters

| Param | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `incidentId` | Guid | Optional | - | Để tính distance |
| `catchingRequestId` | Guid | Optional | - | Alternative cho catching request |
| `maxDistanceKm` | double | No | - | Chỉ lấy rescuer trong vùng (ko yêu cầu input) |

#### Response (Success)

```json
{
  "statusCode": 200,
  "message": "Off-duty backup rescuers snapshot",
  "data": {
    "contextId": "550e8400-e29b-41d4-a716-446655440000",
    "snapshotAt": "2026-05-05T14:30:00Z",
    "rescuers": [
      {
        "rescuerId": "660f9511-f30c-52e5-b827-557766551111",
        "fullName": "Trần Thị B",
        "phoneNumber": "0987654321",
        "isOnline": true,
        "isAvailable": true,
        "latitude": 10.8500,
        "longitude": 106.8500,
        "lastLocationUpdate": "2026-05-05T14:25:00Z",
        "distanceKm": 3.5
      }
    ]
  }
}
```

#### 📌 Key Fields Explained

- **Không có**: `isOnDutyNow`, `shiftAssignmentId`, `shiftName`, `shiftStartTime`, `shiftEndTime`
- ✅ **Có**: `isOnline`, `isAvailable`, `distanceKm`, `phoneNumber` (quan trọng để liên hệ)
- Rescuer này **không nằm trong ca trực bất kỳ** vào thời điểm snapshot

---

## 🎬 UI Flow for Operator Dashboard

### Step 1: Load Primary Pool (On-Duty)

```
1. Operator mở modal "Chọn cứu hộ viên" (Select Rescuer)
2. Frontend gọi: GET /api/monitoring/on-duty?incidentId={incidentId}&onlyAvailable=true
3. Hiển thị danh sách (theo khoảng cách)
   - Label: "🟢 Cứu hộ viên trong ca" (On-duty Rescuers)
   - Hiển thị: [Tên] | [Ca trực] | [Khoảng cách]
   - Ví dụ: "Nguyễn Văn A | Morning Shift 06:00-14:00 | 2.3 km"
```

### Step 2: Operator Dispatches (Primary)

```
1. Operator chọn rescuer từ list
2. Frontend gọi: POST /api/incidents/{incidentId}/dispatch
   {
     "rescuerId": "550e8400-e29b-41d4-a716-446655440000"
   }
3. ✅ Dispatch thành công → Modal đóng
```

### Step 3: Fallback – No Available On-Duty Rescuers

```
1. Nếu danh sách on-duty trống HOẶC operator muốn mở rộng:
2. Frontend hiển thị button: "🔓 Mở rộng tìm kiếm" (Expand Search)
3. Khi operator bấm:
   - Frontend gọi: GET /api/monitoring/off-duty?incidentId={incidentId}
   - Hiển thị danh sách mới với label: "⚠️ Cứu hộ viên ngoài ca" (Off-duty Backup)
   - Hiển thị: [Tên] | [SĐT] | [Khoảng cách] | [Trạng thái]
   - Ví dụ: "Trần Thị B | 0987654321 | 3.5 km | Online & Available"
```

### Step 4: Operator Contacts Backup Rescuer (Manual Process)

```
1. Operator chọn rescuer từ backup pool
2. Frontend hiển thị thông tin:
   - Tên, SĐT (ready to call)
   - Khoảng cách
   - Lần cuối update vị trí
3. Operator thực hiện:
   a. Gọi điện (manual) hoặc
   b. Gửi notification để yêu cầu họ vào app
4. ⏸️ Chờ rescuer phản hồi (không auto-dispatch vào pool này)
5. Khi rescuer ready, operator dispatch như bình thường
```

---

## 📊 UI Wireframe/Mockup Description

### Modal: "Chọn Cứu Hộ Viên" (Select Rescuer)

```
┌─────────────────────────────────────────────────┐
│ Chọn Cứu Hộ Viên - Sự Cố #12345             [X] │
├─────────────────────────────────────────────────┤
│                                                 │
│ 🟢 Cứu Hộ Viên Trong Ca (3 available)          │
│ ┌──────────────────────────────────────────┐  │
│ │ ☐ Nguyễn Văn A                           │  │
│ │   Morning Shift 06:00-14:00 | 2.3 km    │  │
│ │   📱 Call 0912345678                     │  │
│ │                                          │  │
│ │ ☐ Lê Thị C                              │  │
│ │   Morning Shift 06:00-14:00 | 4.5 km    │  │
│ │   📱 Call 0923456789                     │  │
│ │                                          │  │
│ │ ☐ Phạm Văn D                            │  │
│ │   Afternoon Shift 14:00-22:00 | 5.1 km  │  │
│ │   📱 Call 0934567890                     │  │
│ └──────────────────────────────────────────┘  │
│                                                 │
│ [Dispatch Selected] [Cancel]                   │
│                                                 │
│ ─────────────────────────────────────────────  │
│                                                 │
│ ⚠️ Không có cứu hộ viên trong ca?              │
│ [🔓 Mở Rộng Tìm Kiếm (Backup Pool)]            │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Modal Expanded: Backup Pool

```
┌─────────────────────────────────────────────────┐
│ Chọn Cứu Hộ Viên - Sự Cố #12345             [X] │
├─────────────────────────────────────────────────┤
│ [← Quay Lại] 🟢 Trong Ca [🔓 Backup Pool ✓]   │
│                                                 │
│ ⚠️ Cứu Hộ Viên Ngoài Ca (2 available)          │
│ ┌──────────────────────────────────────────┐  │
│ │ ☐ Trần Thị B                            │  │
│ │   3.5 km | 📱 0987654321 (Click to call) │  │
│ │   Last location: 2 min ago               │  │
│ │   Status: Online & Available ✓           │  │
│ │                                          │  │
│ │ ☐ Hoàng Văn E                           │  │
│ │   6.2 km | 📱 0945678901 (Click to call) │  │
│ │   Last location: 5 min ago               │  │
│ │   Status: Online & Available ✓           │  │
│ └──────────────────────────────────────────┘  │
│                                                 │
│ ℹ️ Hãy liên hệ người này để yêu cầu vào ca     │
│    hoặc bật app để nhận dispatch.              │
│                                                 │
│ [Liên Hệ & Dispatch] [Cancel]                  │
│                                                 │
└─────────────────────────────────────────────────┘
```

---

## 🔄 Implementation Checklist for Frontend

### Phase 1: Basic UI

- [ ] Load on-duty pool on modal open
- [ ] Display with shift info and distance
- [ ] Dispatch button sends request
- [ ] Handle empty on-duty response

### Phase 2: Fallback Experience

- [ ] Add "Expand Search" button when on-duty pool is empty or user opts
- [ ] Load off-duty pool with different styling/color
- [ ] Display contact info prominently (phone number clickable)
- [ ] Tab or section to switch between Primary/Backup pools

### Phase 3: Refinements

- [ ] Add loading states during API calls
- [ ] Error handling (incident not found, network error)
- [ ] Real-time location updates (if needed)
- [ ] Sort options: by distance, by rating, by last activity

---

## 🚀 API Call Examples

### Call 1: Get On-Duty List

```bash
curl -X GET "https://api.snakeaid.com/api/monitoring/on-duty?incidentId=550e8400-e29b-41d4-a716-446655440000&onlyAvailable=true" \
  -H "Authorization: Bearer {token}"
```

### Call 2: Get Off-Duty Backup List

```bash
curl -X GET "https://api.snakeaid.com/api/monitoring/off-duty?incidentId=550e8400-e29b-41d4-a716-446655440000&maxDistanceKm=15" \
  -H "Authorization: Bearer {token}"
```

### Call 3: Dispatch to Selected Rescuer

```bash
curl -X POST "https://api.snakeaid.com/api/incidents/550e8400-e29b-41d4-a716-446655440000/dispatch" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "rescuerId": "660f9511-f30c-52e5-b827-557766551111"
  }'
```

---

## 📝 Important Notes

### ✅ Do's

- **Always show on-duty first** as primary option
- **Make backup pool visually distinct** (different color/icon)
- **Show phone number prominently** in backup pool for manual contact
- **Indicate the difference clearly**: on-duty vs backup
- **Track which pool rescuer came from** for audit purposes

### ❌ Don'ts

- ❌ Auto-merge both pools into one list
- ❌ Show backup rescuers with fake shift info
- ❌ Allow direct dispatch to backup rescuer without operator acknowledgment
- ❌ Hide the fact that backup rescuer is not in scheduled shift

### 🎯 Backend Guarantees

- On-duty pool will **always** have `isOnDutyNow = true`
- Off-duty pool will **exclude** any rescuer currently on shift
- Both pools exclude rescuers who already declined or aborted this incident
- Distance calculation happens server-side using PostGIS for accuracy

---

## 🏥 Medical/Operational Context

This is part of the **emergency medical dispatch system** for snakebite incidents. The two-pool approach ensures:

- **Speed**: Primary pool is curated and predictable
- **Resilience**: Backup pool available when primary not sufficient
- **Safety**: Operator maintains full control; no auto-fallback that could mask understaffing

---

## 📞 Support & Questions

For questions about API contract or UX flow, refer to:

- Backend API docs: [Swagger/OpenAPI endpoint]
- Operator dashboard specs: [Link to operator manual]
- Incident management flow: [Link to incident workflow doc]
