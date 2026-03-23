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
  - `Date` (compat), `Status`, `CheckInAt`, `CheckOutAt`, `Notes`,
  - `WorkShiftResponse Shift`

Lưu ý quan trọng:
- FE vẫn gửi `Date` khi tạo/cập nhật assignment.
- Backend sẽ tự build cửa sổ ca thực tế bằng `ShiftStartLocal` và `ShiftEndLocal`.
- FE nên xem `ShiftStartLocal/ShiftEndLocal` là nguồn dữ liệu chính để render lịch.

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

```json
{
  "id": "guid-assignment",
  "rescuerId": "guid-rescuer",
  "shiftId": "guid-shift",
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
    "id": "guid-shift",
    "name": "Shift Night",
    "startTime": "22:00:00",
    "endTime": "06:00:00",
    "requiredRescuers": 3
  }
}
```

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

## 8. Checklist FE rollout

1. Giữ nguyên layout UI/UX hiện tại (grid, modal, actions).
2. Đổi model consume assignment sang `ShiftStartLocal/ShiftEndLocal`.
3. Group cell theo `DateOnly(ShiftStartLocal)`.
4. Render checkin/checkout từ UTC fields.
5. Giữ nguyên request create/update (vẫn gửi `Date`).
6. Test 3 case bắt buộc: ca thường, ca qua đêm, update đổi ngày.

---

> Kết luận: principle và thiết kế UI/UX giữ nguyên. Chỉ cần cập nhật mapping model assignment và flow fill/create như trên để frontend match đúng backend mới.
