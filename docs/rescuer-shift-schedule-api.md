# Rescuer Shift Schedule API (FE Contract)

Tai lieu nay mo ta endpoint de FE lay lich lam viec cua rescuer.

---

## 1. Endpoint

### GET `/api/shifts/rescuer/{id}/my-assignments-today`

Muc dich:

- Lay danh sach shift assignment cua rescuer trong ngay hien tai.
- Van bao gom ca overnight giao cat sang ngay hien tai.

Path param:

| Param | Type | Required | Ghi chu |
|---|---|---:|---|
| `id` | guid | ✅ | RescuerId |

Auth:

- Hien tai controller chua bat `[Authorize]` cho endpoint nay.

Response:

- `ApiResponse<List<ShiftAssignmentResponse>>`

Response wrapper:

```json
{
  "status_code": 200,
  "message": "Success",
  "is_success": true,
  "data": []
}
```

---

## 2. ShiftAssignmentResponse

```json
{
  "id": "guid",
  "rescuerId": "guid",
  "shiftId": "guid",
  "shiftStartLocal": "2026-04-07T08:00:00",
  "shiftEndLocal": "2026-04-07T12:00:00",
  "checkInAtUtc": "2026-04-07T01:01:00Z",
  "checkOutAtUtc": null,
  "status": "Active",
  "checkInAt": "2026-04-07T08:01:00",
  "checkOutAt": null,
  "notes": "Checked in on time",
  "shift": {
    "id": "guid",
    "name": "Morning Shift",
    "startTime": "08:00:00",
    "endTime": "12:00:00",
    "requiredRescuers": 2,
    "isActive": true
  }
}
```

Field notes:

- `shiftStartLocal`, `shiftEndLocal`: local schedule time theo shift assignment.
- `checkInAtUtc`, `checkOutAtUtc`: thoi diem UTC luu trong he thong.
- `checkInAt`, `checkOutAt`: localized value duoc map cho UI hien thi nhanh.
- Backend hien tai loc theo ngay local hien tai va overlap overnight, khong tra ve lich cua cac ngay khac.

---

## 3. Enum Types

### ShiftAssignmentStatus

```csharp
public enum ShiftAssignmentStatus
{
    Scheduled = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3,
    NoShow = 4
}
```

---

## 4. Verified Behavior Notes

- Endpoint ten la `my-assignments-today` va implementation hien tai da loc dung theo ngay hien tai.
- Query overlap overnight duoc tinh theo `ShiftStartLocal < dayEnd` va `ShiftEndLocal > dayStart`.
- Ket qua duoc sort `ShiftStartLocal` tang dan de FE render theo thu tu ca lam viec.

---

## 5. FE Integration Goi y

1. Goi endpoint voi `rescuerId` dang dang nhap.
2. Hien status badge theo `ShiftAssignmentStatus`.
3. Hien thi card theo thu tu da tra ve (ca lam viec som -> muon).
