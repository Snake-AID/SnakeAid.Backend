## 🎯 Tổng hợp hiện trạng Backend + khả năng UI

Tuyệt vời: toàn bộ yêu cầu bạn đang nói (thời khóa biểu = daily view + assignment management) đều đã có nền tảng backend ready, còn UI bạn làm theo mô hình phù hợp.

---

## 1. Backend đã hỗ trợ

### `WorkShift` (mẫu ca)

- `WorkShift` ở DB:
  - `Id`, `Name`, `StartTime`, `EndTime`, `RequiredRescuers`, `IsActive`
- API:
  - `GET /api/shifts`
  - `GET /api/shifts/{id}`
  - `POST /api/shifts`
  - `PUT /api/shifts/{id}`
  - `DELETE /api/shifts/{id}` (soft delete `IsActive=false`)

### `ShiftAssignment` (ca theo ngày)

- `ShiftAssignment`:
  - `Id`, `ShiftId`, `RescuerId`, `ShiftStartLocal`, `ShiftEndLocal`, `Status`, `CheckInAtUtc`, `CheckOutAtUtc`, `Notes`
- API đã chính:
  - `POST /api/shifts/{shiftId}/assign` (1 rescuer)
  - `POST /api/shifts/{shiftId}/assign/bulk` (multi rescuer)
  - `PUT /api/shifts/assignments/{assignmentId}` (update rescuer/date/status/notes)
  - `DELETE /api/shifts/assignments/{assignmentId}`
  - `PATCH /api/shifts/assignments/{assignmentId}/checkin`
  - `PATCH /api/shifts/assignments/{assignmentId}/checkout`
  - `GET /api/shifts/assignments?date=yyyy-MM-dd`
  - `GET /api/shifts/assignments?startDate=yyyy-MM-dd&endDate=yyyy-MM-dd`

### Đã có trạng thái

- `ShiftAssignmentStatus` enum: `Scheduled`, `Active`, `Completed`, `Cancelled`, `NoShow`

### Operator/Rescuer data

- `GET /api/monitoring/rescuers` (list tất cả rescuer)
- `GET /api/monitoring/rescuers/{rescuerId}` (chi tiết rescuer)
- `GET /api/monitoring/shift-assignments/today`

---

## 2. Request/Response model quan trọng

### WorkShift

- `CreateWorkShiftRequest`
  - `Name`, `StartTime`, `EndTime`, `RequiredRescuers`
- `UpdateWorkShiftRequest`
  - `Name`, `StartTime`, `EndTime`, `RequiredRescuers`, `IsActive`
- `WorkShiftResponse` (trong `SnakeAid.Core.Responses.Shift`)

### ShiftAssignment

- `AssignWorkShiftRequest`
  - `RescuerId`, `Date`, `Notes`
- `AssignWorkShiftBulkRequest`
  - `List<Guid> RescuerIds`, `Date`, `Notes`
- `UpdateShiftAssignmentRequest`
  - `RescuerId`, `Date`, `Notes`, `ShiftAssignmentStatus? Status`
- `ShiftAssignmentResponse`
  - `Id`, `RescuerId`, `ShiftId`, `ShiftStartLocal`, `ShiftEndLocal`,
  - `CheckInAtUtc`, `CheckOutAtUtc`,
  - `Status`, `CheckInAt`, `CheckOutAt`, `Notes`,
  - `WorkShiftResponse Shift`

Lưu ý quan trọng:

- FE vẫn gửi `Date` khi tạo/cập nhật assignment.
- Backend sẽ tự build cửa sổ ca thực tế bằng `ShiftStartLocal` và `ShiftEndLocal`.
- FE nên xem `ShiftStartLocal/ShiftEndLocal` là nguồn dữ liệu chính để render lịch.
- **Response không còn `Date` field** - FE dùng `DateOnly.Parse(ShiftStartLocal)` để xác định ngày.

### DateTime Handling (Backend Technical Details)

| Field | Type | DateTimeKind | Usage |
|-------|------|--------------|-------|
| `ShiftStartLocal` | `datetime` (PostgreSQL: `timestamp without time zone`) | `Unspecified` | **Primary for rendering** - local time without timezone conversion |
| `ShiftEndLocal` | `datetime` (PostgreSQL: `timestamp without time zone`) | `Unspecified` | **Primary for rendering** - local time without timezone conversion |
| `CheckInAtUtc` | `timestamptz` (PostgreSQL: `timestamp with time zone`) | `Utc` | Convert from UTC to local for display |
| `CheckOutAtUtc` | `timestamptz` (PostgreSQL: `timestamp with time zone`) | `Utc` | Convert from UTC to local for display |

**FE Implementation Notes:**

- When receiving `ShiftStartLocal` / `ShiftEndLocal`: parse as ISO 8601 string without timezone (e.g., `"2026-03-25T22:00:00"`)
- Treat these values as **already in local time** (Asia/Ho_Chi_Minh / UTC+7)
- Do NOT apply timezone conversion to `ShiftStartLocal` / `ShiftEndLocal`
- For `CheckInAtUtc` / `CheckOutAtUtc`: convert from UTC to local time for display (e.g., `"2026-03-25T15:05:00Z"` → `22:05 UTC+7`)
- Overnight shifts: `ShiftStartLocal` = `2026-03-25T22:00:00`, `ShiftEndLocal` = `2026-03-26T06:00:00` → display as `22:00 - 06:00 (+1)`

### Rescuer info (đã có)

- `BriefRescuerProfileResponse`
  - `AccountId`, `IsOnline`, `PhoneNumber`, `Rating`, `Type`, `LastLocationUpdate`, ...
  - `UserInfo Account` (có `FullName`, `AvatarUrl`, `Email`, `Role`, `IsActive`)
- đủ dùng để show ảnh + tên + trạng thái trên UI.

---

## 3. Review codebase xem có thiếu gì không

- `ShiftController` + `ShiftService` đã đầy đủ.
- Validation:
  - Bulk assign: skip đang có assignment, return list mới gán.
  - Duplicate check dựa trên `(RescuerId, ShiftId, ShiftStartLocal)`.
- Đã có kết nối assignment->shift->rescuer:
  - `GetAssignmentsByDateAsync` và `GetAssignmentsByDateRangeAsync` include `Shift` + `Rescuer`.

---

## 4. UI/UX kiến trúc đề xuất (giữ nguyên)

### 4.1 Mục tiêu UI

FE cần hiện thị một bảng lịch (calendar) rõ ràng và thao tác nhanh với shift assignment:

- `Trục ngang`: `Date` (date slot, tuần, hoặc range theo lựa chọn)
- `Trục dọc`: `WorkShift` (ca mẫu: sáng/trưa/tối)
- Mỗi ô (shift,date) chứa:
  - `x / requiredRescuers`
  - danh sách rescuer trong ca kèm `avatar`, `fullName`, `status` (scheduled/active/completed/no-show)
  - nút chơi nhanh: `+Add`, `Edit`, `Delete`, `Check-In`, `Check-Out`

### 4.2 Thành phần UI chính

1. `Header filter`:
   - Date range picker (tuần/tháng/custom)
   - Shift filter (tên ca, active)
   - status filter (scheduled/active/completed/no-show)
   - text search rescuer

2. `Date-Shift Grid`:
   - Dòng mỗi shift.
   - Cột mỗi ngày trong range.
   - Ô nội dung:
     - item list:
       - `Avatar` + `FullName` + `Role` + `Status badge`
       - `checkInAt` / `checkOutAt`
     - nhấn `+` để assign mới dòng này
     - nhấn item để mở modal detail

3. `Shift assignment detail panel/modal` (khi click ô):
   - thông tin: shift name, time, date, required rescuers, assigned count.
   - list rescuer hiện có: avatar, name, phone, rating, status.
   - action:
     - `Assign one` (select + submit)
     - `Assign bulk` (multi-select)
     - `Update status` (dropdown)
     - `Remove` (kéo, multi remove)
     - `Check-in` / `Check-out` trực tiếp với assignment.

4. `Rescuer list registry`:
   - nguồn: `GET /api/monitoring/rescuers`
   - map id->avatar/name để tốc độ lookup.

### 4.3 Dòng thao tác người dùng

- Bước 1: FE gọi `GET /api/shifts` + `GET /api/shifts/assignments?startDate=..&endDate=..` + `GET /api/monitoring/rescuers`
- Bước 2: xây grid theo range và dữ liệu assignment.
- Bước 3: khi user chọn ô (shift/date): show modal detail + list rescuer.
- Bước 4: assign one => `POST /api/shifts/{shiftId}/assign`.
- Bước 5: bulk assign => `POST /api/shifts/{shiftId}/assign/bulk`.
- Bước 6: edit/remove => `PUT/DELETE /api/shifts/assignments/{assignmentId}`.
- Bước 7: checkin/checkout => `PATCH /api/shifts/assignments/{assignmentId}/checkin/checkout`.
- Bước 8: refresh lại danh sách assignment cho range hiện tại.

### 4.4 Validate & edge cases

- `selected date < today`:
  - hiện màu xám, confirm nếu vẫn muốn assign.
  - default không cho assign new.
- `assignedCount > requiredRescuers`:
  - hiển thị warning (màu đỏ) và `overbooked`.
- nếu shift chưa active, disable assign.
- ca qua đêm:
  - vẫn hiển thị assignment tại cột ngày của `ShiftStartLocal`.
  - hiển thị khung giờ kiểu `22:00 - 06:00 (+1)`.

### 4.5 UI components đề xuất

- Grid/table + sticky header (ký tự fixed column date/shift).
- Modal + slide-over panel.
- List items: avatar+name+status+actions.
- Tooltip/note: notes assignment.
- Batch key:
  - select nhiều ô -> `Bulk assign`, `Bulk remove`.

---

## 5. Cách FE get data và fill lên lịch

Mục tiêu là đổ đúng assignment vào cell `(shiftId, dateColumn)`.

### 5.1 API fetch

- `GET /api/shifts` để lấy hàng (rows).
- `GET /api/shifts/assignments?startDate=...&endDate=...` để lấy assignments trong range.
- `GET /api/monitoring/rescuers` để enrich avatar/name/rating.

### 5.2 Chuẩn hóa dữ liệu assignment

Với mỗi assignment response:

1. Parse `ShiftStartLocal` và `ShiftEndLocal` về local datetime object.
2. Tạo `cellDate = DateOnly(ShiftStartLocal)`.
3. Tạo key `cellKey = ${shiftId}_${cellDate}`.
4. Push assignment vào danh sách của cellKey.

Khuyến nghị render:

- title/time trong item lấy từ `ShiftStartLocal` và `ShiftEndLocal`.
- trạng thái checkin/checkout ưu tiên `CheckInAtUtc`/`CheckOutAtUtc` (convert local để hiển thị).

### 5.3 Tại sao không group bằng Date cũ

Vì backend mới xử lý ca theo cửa sổ thời gian local. Với ca qua đêm, `ShiftEndLocal` sang ngày kế tiếp nhưng assignment vẫn thuộc ca bắt đầu trong ngày `ShiftStartLocal`.

---

## 6. Cách FE tạo assignment mới để match backend

### 6.1 Single assign

Endpoint: `POST /api/shifts/{shiftId}/assign`

Request:

```json
{
  "rescuerId": "rescuer-guid",
  "date": "2026-03-25",
  "notes": "Gan ca"
}
```

Frontend flow:

1. User click ô lịch tại `(shiftId, dateColumn)`.
2. FE gửi `date = dateColumn` (không tự gửi ShiftStartLocal/ShiftEndLocal).
3. Backend tự build `ShiftStartLocal/ShiftEndLocal` từ template `WorkShift` + `date`.
4. Sau khi success, FE re-fetch range hiện tại hoặc upsert record từ response.

### 6.2 Bulk assign

Endpoint: `POST /api/shifts/{shiftId}/assign/bulk`

Request:

```json
{
  "rescuerIds": ["rescuer-guid-1", "rescuer-guid-2"],
  "date": "2026-03-25",
  "notes": "Bulk assignment"
}
```

Behavior backend:

- tự skip rescuer đã được assign trùng ca/ngày.
- trả list assignment mới tạo.

### 6.3 Update assignment

Endpoint: `PUT /api/shifts/assignments/{assignmentId}`

Request vẫn dùng `Date`:

```json
{
  "rescuerId": "rescuer-guid",
  "date": "2026-03-26",
  "notes": "Doi ca",
  "status": "Scheduled"
}
```

Backend sẽ recalculate lại `ShiftStartLocal/ShiftEndLocal`.

---

## 7. Full API models (request + response) cho FE

### 7.1 ShiftAssignmentResponse mẫu (backend mới)

#### Example 1: Overnight Shift (Active, Checked-In)

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "rescuerId": "r1e2s3c4-u5e6-r7i8-d9a0-bc1234567890",
  "shiftId": "s1h2i3f4-t5i6-d7g8-u9i0-d1234567890",
  "shiftStartLocal": "2026-03-25T22:00:00",
  "shiftEndLocal": "2026-03-26T06:00:00",
  "checkInAtUtc": "2026-03-25T15:05:00Z",
  "checkOutAtUtc": null,
  "date": "2026-03-25",
  "status": "Active",
  "checkInAt": null,
  "checkOutAt": null,
  "notes": "Ca qua dem",
  "shift": {
    "id": "s1h2i3f4-t5i6-d7g8-u9i0-d1234567890",
    "name": "Shift Night",
    "startTime": "22:00:00",
    "endTime": "06:00:00",
    "requiredRescuers": 3,
    "isActive": true
  },
  "rescuer": {
    "accountId": "r1e2s3c4-u5e6-r7i8-d9a0-bc1234567890",
    "userInfo": {
      "fullName": "Nguyen Van A",
      "avatarUrl": "https://cdn.example.com/avatars/rescuer-a.jpg",
      "email": "rescuer.a@example.com",
      "role": "Rescuer",
      "isActive": true
    },
    "isOnline": true,
    "phoneNumber": "0901234567",
    "rating": 4.7,
    "type": "Volunteer",
    "lastLocationUpdate": "2026-03-25T14:30:00Z"
  }
}
```

**FE Display Logic:**

- **Time display**: `22:00 - 06:00 (+1)` (extract hours from `shiftStartLocal`/`shiftEndLocal`)
- **Status badge**: `Active` → green badge with checkmark (checked-in at `22:05 UTC+7` from `checkInAtUtc`)
- **Date column**: Use `DateOnly.Parse(shiftStartLocal)` = `2026-03-25` for grid cell placement

#### Example 2: Regular Shift (Scheduled, Not Checked-In)

```json
{
  "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "rescuerId": "r2e3s4c5-u6e7-r8i9-d0a1-bc2345678901",
  "shiftId": "s2h3i4f5-t6i7-d8g9-u0i1-d2345678901",
  "shiftStartLocal": "2026-03-26T06:00:00",
  "shiftEndLocal": "2026-03-26T14:00:00",
  "checkInAtUtc": null,
  "checkOutAtUtc": null,
  "date": "2026-03-26",
  "status": "Scheduled",
  "checkInAt": null,
  "checkOutAt": null,
  "notes": "",
  "shift": {
    "id": "s2h3i4f5-t6i7-d8g9-u0i1-d2345678901",
    "name": "Shift Morning",
    "startTime": "06:00:00",
    "endTime": "14:00:00",
    "requiredRescuers": 2,
    "isActive": true
  },
  "rescuer": {
    "accountId": "r2e3s4c5-u6e7-r8i9-d0a1-bc2345678901",
    "userInfo": {
      "fullName": "Tran Thi B",
      "avatarUrl": "https://cdn.example.com/avatars/rescuer-b.jpg",
      "email": "rescuer.b@example.com",
      "role": "Rescuer",
      "isActive": true
    },
    "isOnline": false,
    "phoneNumber": "0907654321",
    "rating": 4.9,
    "type": "Professional",
    "lastLocationUpdate": "2026-03-25T10:00:00Z"
  }
}
```

**FE Display Logic:**

- **Time display**: `06:00 - 14:00` (same day)
- **Status badge**: `Scheduled` → blue/gray badge (not yet checked-in)
- **Date column**: `2026-03-26`

#### Example 3: Status Badge Mapping

| Status | Badge Color | Icon | Description |
|--------|-------------|------|-------------|
| `Scheduled` | Blue/Gray | 📅 | Assigned but not yet started |
| `Active` | Green | ✅ | Checked-in and on duty |
| `Completed` | Gray | ✔️ | Shift finished normally |
| `Cancelled` | Red | ❌ | Assignment cancelled |
| `NoShow` | Orange/Red | ⚠️ | Rescuer didn't show up |

### 7.2 Table cell object de xay grid

```json
{
  "shiftId": "guid",
  "date": "2026-03-25",
  "shiftName": "Shift Toi",
  "requiredRescuers": 3,
  "assignments": [
    {
      "assignmentId": "guid",
      "rescuerId": "guid",
      "status": "Scheduled",
      "shiftStartLocal": "2026-03-25T22:00:00",
      "shiftEndLocal": "2026-03-26T06:00:00",
      "checkInAtUtc": null,
      "checkOutAtUtc": null,
      "notes": "",
      "rescuer": {
        "accountId": "guid",
        "fullName": "Nguyen Van A",
        "avatarUrl": "...",
        "rating": 4.7,
        "isOnline": true
      }
    }
  ]
}
```

---

## 8. Visual Layout Guide for Timetable Grid

### 8.1 Grid Structure

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Filter: [Date Range Picker ▼]  [Shift Filter ▼]  [Status ▼]  [Search...]  │
├─────────────┬──────────┬──────────┬──────────┬──────────┬──────────┬────────┤
│   Shift     │   Mon    │   Tue    │   Wed    │   Thu    │   Fri    │  ...   │
│   (Row)     │  25/03   │  26/03   │  27/03   │  28/03   │  29/03   │        │
├─────────────┼──────────┼──────────┼──────────┼──────────┼──────────┼────────┤
│ Shift       │  [Cell]  │  [Cell]  │  [Cell]  │  [Cell]  │  [Cell]  │        │
│ Morning     │  2/3 ✓   │  3/2 ⚠️  │  1/3     │  3/3 ✓   │  2/3     │        │
│ 06:00-14:00 │          │          │          │          │          │        │
├─────────────┼──────────┼──────────┼──────────┼──────────┼──────────┼────────┤
│ Shift       │  [Cell]  │  [Cell]  │  [Cell]  │  [Cell]  │  [Cell]  │        │
│ Afternoon   │  1/2     │  2/2 ✓   │  2/2 ✓   │  1/2     │  0/2 ❌  │        │
│ 14:00-22:00 │          │          │          │          │          │        │
├─────────────┼──────────┼──────────┼──────────┼──────────┼──────────┼────────┤
│ Shift       │  [Cell]  │  [Cell]  │  [Cell]  │  [Cell]  │  [Cell]  │        │
│ Night       │  3/3 ✓   │  2/3     │  3/3 ✓   │  2/3     │  3/3 ✓   │        │
│ 22:00-06:00 │          │          │          │          │          │        │
└─────────────┴──────────┴──────────┴──────────┴──────────┴──────────┴────────┘
```

### 8.2 Cell Content Hierarchy

Each cell `(shiftId, date)` should contain:

```
┌─────────────────────────────────────┐
│  Shift Morning          [ + Add ]   │  ← Header: shift name + quick action
│  06:00 - 14:00                      │
│  ─────────────────────────────────  │
│  2 / 3 Required           ✓ Full   │  ← Count indicator + status badge
│  ─────────────────────────────────  │
│  [Avatar] Nguyen Van A    [●] Active│  ← Rescuer item 1 (with status dot)
│  [Avatar] Tran Thi B      [○] Sched │  ← Rescuer item 2
│  ─────────────────────────────────  │
│  ⚠️ Overbooked (+1)                 │  ← Warning if assignedCount > required
└─────────────────────────────────────┘
```

### 8.3 Rescuer Item Layout (in cell or modal)

```
┌─────────────────────────────────────────────────────────┐
│  [Avatar]  Nguyen Van A                    [●] Active   │
│            📞 0901234567  |  ⭐ 4.7  |  🟢 Online       │
│            Checked-in: 22:05 (from checkInAtUtc)        │
│            Notes: "Ca qua dem"                          │
│                                         [Edit] [Remove] │
└─────────────────────────────────────────────────────────┘
```

### 8.4 Modal/Panel Layout (on cell click)

```
┌─────────────────────────────────────────────────────────────────┐
│  Shift Night - 25/03/2026                              [✕ Close]│
│  22:00 - 06:00 (+1)                                             │
│  ─────────────────────────────────────────────────────────────  │
│  Required: 3  |  Assigned: 2  |  Status: 🟡 Under-staffed       │
│  ─────────────────────────────────────────────────────────────  │
│                                                                 │
│  Assigned Rescuers:                                             │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ [Avatar] Nguyen Van A        [●] Active    [Edit][Remove]│   │
│  │ [Avatar] Tran Thi B          [○] Scheduled [Edit][Remove]│   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Quick Actions:                                                 │
│  [+ Assign One]  [+ Bulk Assign]  [Check-In]  [Check-Out]      │
│                                                                 │
│  ─────────────────────────────────────────────────────────────  │
│  Filter Rescuers: [Search...]  [Status ▼]  [Specialization ▼]  │
│  Available Rescuers (not assigned this shift):                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ [Avatar] Le Van C   📞 090...  ⭐ 4.5  [+ Add]          │   │
│  │ [Avatar] Pham Thi D 📞 091...  ⭐ 4.8  [+ Add]          │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### 8.5 Status Badge Mapping

| Status | Badge Color | Icon | Description |
|--------|-------------|------|-------------|
| `Scheduled` | Blue/Gray | 📅 | Assigned but not yet started |
| `Active` | Green | ✅ | Checked-in and on duty |
| `Completed` | Gray | ✔️ | Shift finished normally |
| `Cancelled` | Red | ❌ | Assignment cancelled |
| `NoShow` | Orange/Red | ⚠️ | Rescuer didn't show up |

---

## 9. Checklist FE rollout

1. Giữ nguyên layout UI/UX hiện tại (grid, modal, actions).
2. Đổi model consume assignment sang `ShiftStartLocal/ShiftEndLocal`.
3. Group cell theo `DateOnly(ShiftStartLocal)`.
4. Render checkin/checkout từ UTC fields.
5. Giữ nguyên request create/update (vẫn gửi `Date`).
6. Test 3 case bắt buộc: ca thường, ca qua đêm, update đổi ngày.

---

## 10. Summary

| Topic | Key Point |
|-------|-----------|
| **DateTime Handling** | `ShiftStartLocal/ShiftEndLocal` = Kind=Unspecified, no timezone conversion needed |
| **UTC Fields** | `CheckInAtUtc/CheckOutAtUtc` = convert from UTC to local for display |
| **Grid Placement** | Use `DateOnly(ShiftStartLocal)` for cell key, not the legacy `Date` field |
| **Overnight Shifts** | Display as `22:00 - 06:00 (+1)` when `ShiftEndLocal.Date > ShiftStartLocal.Date` |
| **Status Badges** | Scheduled=Blue, Active=Green, Completed=Gray, Cancelled=Red, NoShow=Orange |
| **Request Flow** | FE sends `Date` → Backend builds `ShiftStartLocal/ShiftEndLocal` from template |

> Kết luận: principle và thiết kế UI/UX giữ nguyên. Chỉ cần cập nhật mapping model assignment và flow fill/create như trên để frontend match đúng backend mới.
