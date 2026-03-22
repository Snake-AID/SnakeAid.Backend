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
  - `Id`, `ShiftId`, `RescuerId`, `Date`, `Status`, `CheckInAt`, `CheckOutAt`, `Notes`
- API đã chính:
  - `POST /api/shifts/{shiftId}/assign` (1 rescuer)
  - `POST /api/shifts/{shiftId}/assign/bulk` (multi rescuer)
  - `PUT /api/shifts/assignments/{assignmentId}` (update rescuer/date/status/notes)
  - `DELETE /api/shifts/assignments/{assignmentId}`
  - `PATCH /api/shifts/assignments/{assignmentId}/checkin`
  - `PATCH /api/shifts/assignments/{assignmentId}/checkout`
  - `GET /api/shifts/assignments?date=yyyy-MM-dd`

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
  - `Id`, `RescuerId`, `ShiftId`, `Date`, `Status`, `CheckInAt`, `CheckOutAt`, `Notes`,
  - `WorkShiftResponse Shift`

### Rescuer info (đã có)

- `BriefRescuerProfileResponse`
  - `AccountId`, `IsOnline`, `PhoneNumber`, `Rating`, `Type`, `LastLocationUpdate`, ...
  - `UserInfo Account` (có `FullName`, `AvatarUrl`, `Email`, `Role`, `IsActive`)
- đủ dùng để show ảnh + tên + trạng thái trên UI.

---

## 3. Review codebase xem có thiếu gì không

- `ShiftController` + `ShiftService` đã đầy đủ.
- `OperatorController` đã có route hỗ trợ top-level resale/monitoring.
- Validation:
  - Bulk assign: skip đang có assignment, return list mới gán.
  - Hiện chưa có “không cho assign quá khứ” (nếu cần thêm, bổ sung tại service `Assign...` và `Assign...Bulk`).
- Đã có kết nối assignment->shift->rescuer:
  - `GetAssignmentsByDateAsync` include `Shift` + `Rescuer`.

---

## 4. UI/UX kiến trúc đề xuất (định nghĩa bạn đã yêu)

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
     - item list huor:
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

- `selected date < DateTime.UtcNow`:
  - hiện màu xám, confirm nếu vẫn muốn assign.
  - default không cho assign new.
- `assignedCount > requiredRescuers`:
  - hiển thị warning (màu đỏ) và `overbooked`.
- ban đầu gap: nếu shift chưa active, disable assign.

### 4.5 UI components đề xuất

- Grid/table + sticky header (ký tự fixed column date/shift).
- Modal + slide-over panel.
- List items: avatar+name+status+actions.
- Tooltip/note: notes assignment.
- Batch key:
  - select nhiều ô -> `Bulk assign`, `Bulk remove`.

---

Tiếp theo mình có thể thêm một block sample JSON payload truyền giữa FE/BE cho mỗi thao tác nếu cần
---

## 5. Toàn bộ endpoint + flow

### 5.1 fetch template ca

- `GET /api/shifts`
- mapping view `WorkShift` danh sách

### 5.2 fetch assignments ngày / tuần (và range)

- `GET /api/shifts/assignments?date=2026-03-22` (truy vấn assignment cho một ngày cụ thể)
- `GET /api/shifts/assignments?startDate=2026-03-22&endDate=2026-03-28` (truy vấn assignment cho khoảng ngày, đã có)
- `GET /api/monitoring/shift-assignments/today` (nhanh cho dashboard toàn bộ ngày hôm nay)

### 5.3 assign

- `POST /api/shifts/{shiftId}/assign` `AssignWorkShiftRequest`
- `POST /api/shifts/{shiftId}/assign/bulk` `AssignWorkShiftBulkRequest`
- (endpoint này đã có)

### 5.4 update/xóa assignment

- `PUT /api/shifts/assignments/{assignmentId}` `UpdateShiftAssignmentRequest`
- `DELETE /api/shifts/assignments/{assignmentId}`
- `PATCH .../checkin`, `PATCH .../checkout` (từ status, check time)

### 5.5 rescuer reference / info

- `GET /api/monitoring/rescuers` => `BriefRescuerProfileResponse` có name/avatar
- `GET /api/monitoring/rescuers/{id}` => chi tiết

---

## 6. Nên làm ở web

1. load shift templates -> build rows
2. chọn range ngày -> build columns
3. load assignments cho date hiện tại (plunk iter)
4. theo ô shift+date:
   - list assignments tại key `(shiftId,date)`
   - show từng rescuer, status
   - nút action
5. thao tác xong: refresh assignment date (vì đã gán/xóa)
6. thiết kế responsive (desktop/tablet)
7. thêm sorting/grouping
8. chạy test e2e (assign/bulk/remove/checkin/checkout)

---

## 7. Ghi chú

- Với flow này, backend đã cơ bản hỗ trợ đầy đủ.
- Thêm checking “past date” nên đặt ở service/validate layer.
- Nếu muốn trong table show `avatar`+`name` phải `GET /api/monitoring/rescuers` hoặc `GET /api/monitoring/rescuers/{id}` (có `Account.FullName`, `AvatarUrl`).
- Đảm bảo token (Admin/Operator role) khi gọi các endpoint.

## ✅ Full API models (request + response) & web template

Xuất sắc: backend hiện tại đã đầy đủ các endpoint và chỉ cần map UI. Dưới đây là tổng hợp theo json schema mô phỏng (bạn copy/paste trực tiếp khi làm frontend).

---

# 1. `WorkShift` CRUD (template ca)

## 1.1. POST `/api/shifts`

- request: `CreateWorkShiftRequest`

```json
{
  "name": "𝐒𝐡𝐢𝐟𝐭 𝐌𝐨𝐫𝐧𝐢𝐧𝐠",
  "startTime": "06:00:00",
  "endTime": "12:00:00",
  "requiredRescuers": 4
}
```

- response: `ApiResponse<WorkShiftResponse>`

```json
{
  "success": true,
  "message": "Work shift created successfully.",
  "data": {
    "id": "guid",
    "name": "Shift Morning",
    "startTime": "06:00:00",
    "endTime": "12:00:00",
    "requiredRescuers": 4
  }
}
```

## 1.2. GET `/api/shifts`

- response: `ApiResponse<List<WorkShiftResponse>>`

## 1.3. GET `/api/shifts/{id}`

- response: `ApiResponse<WorkShiftResponse>`

## 1.4. PUT `/api/shifts/{id}`

- request: `UpdateWorkShiftRequest`

```json
{
  "name": "Shift Morning",
  "startTime": "06:00:00",
  "endTime": "12:00:00",
  "requiredRescuers": 3,
  "isActive": true
}
```

- response: `ApiResponse<WorkShiftResponse>`

## 1.5. DELETE `/api/shifts/{id}`

- response: `ApiResponse<bool>`

---

# 2. `ShiftAssignment` (ca theo ngày)

## 2.1. POST `/api/shifts/{shiftId}/assign`

- request: `AssignWorkShiftRequest`

```json
{
  "rescuerId": "rescuer-guid",
  "date": "2026-03-25",
  "notes": "Gán ca"
}
```

- response: `ApiResponse<ShiftAssignmentResponse>`

## 2.2. POST `/api/shifts/{shiftId}/assign/bulk`

- request: `AssignWorkShiftBulkRequest`

```json
{
  "rescuerIds": ["rescuer-guid-1","rescuer-guid-2","rescuer-guid-3"],
  "date": "2026-03-25",
  "notes": "Bulk assignment"
}
```

- response: `ApiResponse<List<ShiftAssignmentResponse>>`

## 2.3. PUT `/api/shifts/assignments/{assignmentId}`

- request: `UpdateShiftAssignmentRequest`

```json
{
  "rescuerId": "rescuer-guid",
  "date": "2026-03-25",
  "notes": "Ghi chú mới",
  "status": "Active" // enum: Scheduled, Active, Completed, Cancelled, NoShow
}
```

- response: `ApiResponse<ShiftAssignmentResponse>`

## 2.4. DELETE `/api/shifts/assignments/{assignmentId}`

- response: `ApiResponse<bool>`

## 2.5. PATCH `/api/shifts/assignments/{assignmentId}/checkin`

- response: `ApiResponse<ShiftAssignmentResponse>`

## 2.6. PATCH `/api/shifts/assignments/{assignmentId}/checkout`

- response: `ApiResponse<ShiftAssignmentResponse>`

## 2.7. GET `/api/shifts/assignments?date=yyyy-MM-dd`

- response: `ApiResponse<List<ShiftAssignmentResponse>>`

## 2.8. GET `/api/monitoring/shift-assignments/today`

- response: `ApiResponse<List<ShiftAssignmentResponse>>`

---

# 3. Rescuer info (image/name/độ/histories)

## 3.1 GET `/api/monitoring/rescuers`

- response: `ApiResponse<List<BriefRescuerProfileResponse>>`
- model:

```json
{
  "accountId": "guid",
  "isOnline": true,
  "phoneNumber": "0987...",
  "rating": 4.5,
  "ratingCount": 120,
  "type": "Emergency",
  "lastLocationUpdate": "2026-03-22T10:00:00Z",
  "totalMissions": 50,
  "completedMissions": 48,
  "account": {
    "id": "guid",
    "email": "r@a.com",
    "fullName": "Nguyen A",
    "avatarUrl": "https://...",
    "role": "Rescuer",
    "isActive": true
  }
}
```

## 3.2 GET `/api/monitoring/rescuers/{rescuerId}`

- response: `ApiResponse<BriefRescuerProfileResponse>`

---

# 4. `ShiftAssignmentResponse` cụ thể

- model:

```json
{
  "id": "guid-assignment",
  "rescuerId": "guid-rescuer",
  "shiftId": "guid-shift",
  "date": "2026-03-25",
  "status": "Scheduled",
  "checkInAt": null,
  "checkOutAt": null,
  "notes": "Notes",
  "shift": {
    "id": "guid-shift",
    "name": "Shift Morning",
    "startTime": "06:00:00",
    "endTime": "12:00:00",
    "requiredRescuers": 4
  }
}
```

---

# 5. UI template theo “thời khóa biểu”

## 5.1 Layout chính

- Table / Grid:
  - cột `Date` (hàng ngày hoặc tuần)
  - hàng `WorkShift` (sáng/trưa/tối)
- Mỗi ô `(shift,date)`:
  - list rescuer assigned (avatar + fullName + status tag)
  - `X / requiredRescuers`
  - button: `+Add`, `Edit`, `Delete`
  - `checkin/checkout` nếu thời điểm phù hợp

## 5.2 Modal / sidepanel “Chi tiết ca (shift + date)”

- shift infocard
- danh sách rescuer trong ca
- `Assign one` (select dropdown rescuer + button)
- `Assign many` (multi-select checkboxes / input list)
- `Remove` (xóa rescuer)
- `Update status` (Scheduled/Active/Completed/NoShow)
- validate date:
  - nếu date < today: show warning, tắt “assign future” (hoặc chỉ read-only)

## 5.3 UX cần có

- filter:
  - date range (week/month)
  - chỉ show shift “còn trống”/“filled”
  - status
- search rescuer trong grid
- tooltip hover mỗi rescuer (số mission + rating + online)
- batch actions (nhiều ô chọn + bulk assign/remove)

---

# 6. Lưu ý logic

- “id” trong path `http://.../api/shifts/{id}` là `workShift.Id` (template)
- `assignmentId` là `ShiftAssignment.Id`
- bulk assign giữ nguyên behavior:
  - skip nếu đã có assignment.
- nếu cần thêm “không assign quá khứ”, chặn ngay trong service `AssignWorkShiftAsync` + `AssignWorkShiftBulkAsync`:

```csharp
if (request.Date < DateOnly.FromDateTime(DateTime.UtcNow))
  throw new BadRequestException("Cannot assign shift in past date.");
```

---

## 7. Checklist để implement frontend

1. query `GET /api/shifts`
2. query `GET /api/shifts/assignments?date=...` (về grid)
3. query `GET /api/monitoring/rescuers` khi cần dropdown/avatars
4. event `POST /api/shifts/{shiftId}/assign` (single)
5. event `POST /api/shifts/{shiftId}/assign/bulk` (multi)
6. event `PUT /api/shifts/assignments/{assignmentId}` (edit)
7. event `DELETE /api/shifts/assignments/{assignmentId}` (remove)
8. event `PATCH .../checkin`, `PATCH .../checkout`
9. refresh sau mỗi mutation

---

## 8. Đề xuất schema JSON (dùng cho frontend)

### Table cell object

```json
{
  "shiftId": "guid",
  "date": "2026-03-25",
  "shiftName": "Shift Trưa",
  "requiredRescuers": 4,
  "assignments": [
    {
      "assignmentId": "guid",
      "rescuerId": "guid",
      "status": "Scheduled",
      "notes": "",
      "checkInAt": null,
      "checkOutAt": null,
      "rescuer": {
        "accountId": "guid",
        "fullName": "Nguyễn Văn A",
        "avatarUrl": "...",
        "rating": 4.7,
        "isOnline": true
      }
    }
  ]
}
```

---

> Kết luận: bạn đã có đủ endpoints + model để triển khai web hoàn chỉnh như thời khóa biểu. Chỉ cần mapping báo đúng từ API, UI thì tùy style (table/kanban/calendar).
