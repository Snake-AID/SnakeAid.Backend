# Luồng event realtime của SOS Dispatch (đã implement)

Tài liệu này mô tả toàn bộ **luồng sự kiện realtime** hiện đang hoạt động trong hệ thống: từ lúc member tạo SOS, tới lúc operator dispatch, rescuer nhận/không nhận, và operator được báo.

## Cập nhật contract mới (2026-04-14)

- Đã chuẩn hóa reason code cho luồng `RequestCancelled` để client parse ổn định.
- Tài liệu tích hợp chi tiết cho Flutter rescuer app và FE operator:
  - `docs/sos-dispatch-cancellation-reason-contract.md`

---

## 1) Khi member tạo SOS (incident mới)

### 🟢 Event tới Operator (dashboard)

- Khi `POST /api/incidents/sos` được gọi, hệ thống tạo `SnakebiteIncident` với trạng thái `Pending`.
- Sau khi tạo xong, server gọi:
  - `NotifyNewIncidentCreatedAsync(...)`
- **SignalR event** gửi tới group **`Operators`**:
  - Event name: **`NewIncidentCreated`** (preferred)
  - Legacy event name (vẫn gửi để tương thích): **`IncidentLocationUpdated`**
  - Payload có:
    - `IncidentId`
    - `MemberId`
    - `Latitude`, `Longitude`
    - `IsNewIncident = true`
    - `UpdatedAt`

➡️ Kết quả: Dashboard operators thấy vụ mới xuất hiện trên bản đồ.

---

## 2) Khi rescuer đăng nhập / online (đang chờ dispatch)

### 🟢 Event tới Operator

- Khi rescuer gọi `JoinAsRescuer()` (kết nối RescuerHub):
  - Profile rescuer được đánh dấu `IsOnline=true`
  - Server gửi sự kiện tới group **`Operators`**:
    - Event name: **`RescuerOnlineStatus`**
    - Payload gồm: `RescuerId`, `IsOnline`, `IsAvailable`, `UpdatedAt`

➡️ Operator dashboard có thể hiển thị danh sách rescuer online.

---

## 3) Operator claim + confirm incident (đã contact và verify ca này ko false alarn)

### 🟢 Event tới Operator

- Khi operator gọi **`POST /api/incidents/{id}/confirm`** (đã hợp nhất claim + confirm):
  - Nếu chưa có operator nào giữ case thì `HandlingOperatorId` được set (claim)
  - Nếu status đang là `Pending`, sẽ chuyển sang **`Verified`** (confirm)
  - Server gửi **SignalR event** tới **group `Operators`**:
    - Event name: **`IncidentClaimed`**
    - Payload: `{ IncidentId, OperatorId, UpdatedAt }`

> Lưu ý: hiện không gửi event này tới member/mission hub.

---

## 4) Operator gửi dispatch đến rescuer

### 🟢 Thực tế gửi request tới rescuer (RescuerHub)

- Operator gọi **`POST /api/incidents/{id}/dispatch`** với `RescuerId`
- Server xây `RescuerRequest` mới và giữ incident ở trạng thái `Verified` (chờ rescuer đồng ý)
- Server gửi SignalR event tới **rescue client**:
  - Hub: `RescuerHub`
  - Event: **`DispatchRequested`**
  - Payload: `RequestId`, `IncidentId`, `OperatorId`, `RescuerId`, `DispatchedAt`, `Latitude/Longitude`, `Message`
- Đồng thời gửi **SignalR event** tới **group `Monitors`** (admin dashboard) để log.

### 🟢 Operator biết dispatch đã được gửi

- Server cũng gửi SignalR event tới **group `Operators`**:
  - Event: **`DispatchRequested`**
  - Payload: `{ IncidentId, RescuerId, OperatorId, RequestedAt }`
- API response trả về:
  - `DispatchRequestId`
  - `DispatchedRescuerId`

➡️ Như vậy operator có thể biết “dispatch đã gửi thành công” (dù rescuer có nhận hay không).

---

## 5) Rescuer chấp nhận dispatch

### 🟢 Event tới Rescuer

- Rescuer gọi `AcceptDispatchRequest(requestId)` trên `RescuerHub`.
- Server cập nhật:
  - `RescuerRequest.Status = Accepted`
  - `Incident.Status = Assigned`
  - `Incident.AssignedRescuerId = rescuerId`
  - `RescuerProfile.IsAvailable = false`
- Server gửi:
  - **`RequestAccepted`** tới rescuer (caller)

### 🟢 Event tới Operator

- Event: **`RescuerAccepted`** tới group **`Operators`**

---

## 6) Rescuer từ chối dispatch

### 🟢 Event tới Rescuer

- Rescuer gọi `DeclineDispatchRequest(requestId, reason)`
- Server cập nhật:
  - `RescuerRequest.Status = Declined`
  - `Incident.Status = Verified` (cho phép dispatch lại)
  - `RescuerProfile.IsAvailable` giữ là `true`
- Server gửi:
  - **`RequestDeclined`** tới rescuer (caller)

### 🟢 Event tới Operator

- Event: **`RescuerDeclined`** tới group **`Operators`**

---

## 7) Những event “không phát” (theo thiết kế hiện tại)

- **Không gửi tới member/mission hub** khi dispatch được gửi (chỉ operator và rescuer mới nhận)
- **Không gửi `OperatorContacting` đến member/mission**
- **Không gửi `RescuerAccepted`/`RescuerDeclined`** đến member/mission

---

## 8) Các event và nơi nhận (tóm tắt)

| Event name | Gửi từ | Nhóm đối tượng nhận | Ghi chú |
|------------|--------|---------------------|---------|
| `NewIncidentCreated` (preferred) / `IncidentLocationUpdated` (legacy) | Khi tạo SOS | `Operators` | Hiển thị map vụ mới |
| `RescuerOnlineStatus` | Rescuer join/leave | `Operators` | Theo dõi online rescuer |
| `IncidentClaimed` | Operator claim | `Operators` | Báo đã có operator đang xử lý |
| `DispatchRequested` | Operator dispatch | `Rescuer` + `Monitors` + `Operators` | Gửi tới rescuer và dashboard |
| `RequestAccepted` | Rescuer accept | `Rescuer` | Xác nhận nhận request |
| `RescuerAccepted` | Rescuer accept | `Operators` | Operator biết rescuer nhận |
| `RequestDeclined` | Rescuer decline | `Rescuer` | Xác nhận từ chối |
| `RescuerDeclined` | Rescuer decline | `Operators` | Operator biết rescuer từ chối |
| `MissionAbortedByOperator` | Operator abort | `Rescuer` + `Operators` | Ép hủy khi rescuer không phản hồi |

---

## 9) Ghi chú / Cần kiểm tra thêm

- Nếu rescuer **không kết nối**, dispatch request sẽ không gửi (chỉ log warning), và operator **không được báo**.
- Nếu cần thêm: “operator được báo khi không gửi được request”, cần bổ sung event/response.

---

Nếu cần tôi có thể mở rộng thêm phần “test checklist cụ thể cho QA” (bao gồm cả câu lệnh SignalR client).
