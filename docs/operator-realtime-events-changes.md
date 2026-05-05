# Tổng hợp event thay đổi cho operator dashboard

Tài liệu này chỉ liệt kê các event realtime **đã thay đổi payload** trong scope cập nhật gần nhất, để FE dùng khi update trạng thái rescuer.

---

## 1. DispatchRequested (rescue)

**Nguồn phát:** `SignalROperatorRealtimeNotificationService.NotifyDispatchRequestedAsync`

**Thay đổi:** thêm `IsAvailable = false` để FE set rescuer đang bận ngay khi dispatch.

**Payload:**

```json
{
  "IncidentId": "guid",
  "RescuerId": "guid",
  "OperatorId": "guid",
  "IsAvailable": false,
  "RequestedAt": "2026-05-05T12:00:00Z"
}
```

**FE xử lý:**

- Set `IsAvailable = false` cho rescuer trên map/list.
- Nếu UI có trạng thái "busy", chuyển rescuer sang busy ngay.
- Hoặc có thể refresh lại danh sách rescuer đang online để dashboard cập nhật theo

---

## 2. SnakeCatchingRequestAssigned

**Nguồn phát:** `SignalRSnakeCatchingRequestNotificationService.NotifyRequestAssignedAsync`

**Thay đổi:** thêm `IsAvailable` để FE set rescuer đang bận khi assigned.

**Payload:**

```json
{
  "Id": "guid",
  "Status": "Assigned",
  "AssignedAt": "2026-05-05T12:00:00Z",
  "AssignedRescuerId": "guid",
  "AssignedRescuerName": "string",
  "AssignedRescuerPhone": "string",
  "IsAvailable": false
}
```

**Ghi chú:**

- `IsAvailable` là `false` khi có `AssignedRescuerId`.
- Nếu vì lý do nào đó event không có `AssignedRescuerId`, `IsAvailable` sẽ là `null`.

**FE xử lý:**

- Nếu `AssignedRescuerId` có giá trị, set rescuer đó `IsAvailable = false`.
- Hoặc cũng có thể refresh lại danh sách rescuer đang online để dashboard cập nhật theo

---

## 3. RescuerDeclined (không đổi payload)

**Nguồn phát:** `SignalROperatorRealtimeNotificationService.NotifyRescuerDeclinedAsync`

**Payload:**

```json
{
  "IncidentId": "guid",
  "RescuerId": "guid",
  "Reason": "string|null",
  "DeclinedAt": "2026-05-05T12:00:00Z"
}
```

**FE xử lý:**

- Set `IsAvailable = true` cho rescuer vừa decline.
- Hiển thị toast nếu cần. (đồng bộ Id với các toast khác ex: INC-...)
- Hoặc cũng có thể refresh lại  danh sách rescuer đang online để dashboard cập nhật theo

---

## 4. Lưu ý tích hợp

- `IsOnline` vẫn phản ánh trạng thái kết nối/hoạt động, không dùng để thể hiện busy.
- `IsAvailable` mới là flag để cập nhật marker/list busy/rảnh.
