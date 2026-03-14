Read [](file:///d%3A/snake-aid/SnakeAid.Backend/docs/operator-hub.md)

Searched for regex `class\s+\w+Hub|Hub\s*:` (`**/*.{cs,md}`), 6 results

Read [](file:///d%3A/snake-aid/SnakeAid.Backend/refactor-sos.md#150-150), lines 150 to 260

Read [](file:///d%3A/snake-aid/SnakeAid.Backend/SnakeAid.Api/Hubs/RescuerHub.cs#1-1), lines 1 to 260

Read [](file:///d%3A/snake-aid/SnakeAid.Backend/SnakeAid.Api/Hubs/RescuerHub.cs#260-260), lines 260 to 477

Read [](file:///d%3A/snake-aid/SnakeAid.Backend/SnakeAid.Api/Hubs/MissionHub.cs)

Searched for regex `OperatorGroup|"Operators"|Notify.*Operator|Notify.*RescuerAborted|RescuerAborted|OperatorRealtime|OperatorHub` (`**/*.cs`), 20 results

Read [](file:///d%3A/snake-aid/SnakeAid.Backend/SnakeAid.Api/Services/SignalROperatorRealtimeNotificationService.cs#1-1), lines 1 to 220

Read [](file:///d%3A/snake-aid/SnakeAid.Backend/SnakeAid.Api/Services/SignalROperatorRealtimeNotificationService.cs#220-220), lines 220 to 232

Searched for regex `Clients\.Group\(OperatorGroup\)` (`**/*.cs`), 18 results

Read [](file:///d%3A/snake-aid/SnakeAid.Backend/SnakeAid.Api/Program.cs#1-1), lines 1 to 200

Read [](file:///d%3A/snake-aid/SnakeAid.Backend/SnakeAid.Api/Program.cs#200-200), lines 200 to 380

## ✅ Mục tiêu: “Migrate Operator sang OperatorHub”

Hiện tại **operator đang “ngồi” trong `RescuerHub`** (group `"Operators"`), nên mọi event/notification dành cho operator đều được gửi qua `IHubContext<RescuerHub>` (hoặc `Clients.Group("Operators")` trong `RescuerHub`).  
Nếu muốn “migrate” operator sang **OperatorHub riêng**, thì bạn cần đảm bảo:

1) **Operator client** kết nối vào `OperatorHub` (URL + hub name khác)  
2) **Server** gửi notification dành cho operator qua `IHubContext<OperatorHub>` (không phải `RescuerHub`)  
3) **RescuerHub / MissionHub / NotificationService** phải được cập nhật để dùng OperatorHub khi cần gửi cho operator  

---

## 🔧 Plan tổng quan (các bước chính)

### 1) Tạo `OperatorHub` mới

- Tạo file `SnakeAid.Api/Hubs/OperatorHub.cs`
- Xây dựng các method tương tự `RescuerHub.JoinAsOperator` / `LeaveAsOperator`
- Quản lý group operator (ví dụ: group `"Operators"` hoặc `"Operator:{id}"`)
- Triển khai `OnDisconnectedAsync` để:
  - mark operator off-duty (set off duty, clear handling incident)
  - broadcast cho các operator còn online (vì UI dashboard cần cập nhật)
  - (nếu muốn) trigger “release incident nếu disconnect > X phút”

### 2) Đăng ký và map hub mới trong Program.cs

- `app.MapHub<OperatorHub>("/operator-hub")` (hoặc tên endpoint khác)
- Các client operator chuyển qua endpoint này

### 3) Cập nhật notification service / hub context

**Hiện tại:** `SignalROperatorRealtimeNotificationService` dùng `IHubContext<RescuerHub>`  
**Cần chuyển sang:** `IHubContext<OperatorHub>`  

- Tức là: `SignalROperatorRealtimeNotificationService` (hoặc tên nào đó) sẽ gửi event đến group operator trong `OperatorHub`.
- Các method event (dưới) vẫn giữ, nhưng gửi trên hub mới.

---

## 🧠 Những event + method cần chú ý (vì phải “chuyển kênh” tới OperatorHub)

### A) Event từ service / workflow gọi `IOperatorRealtimeNotificationService`

_(đang được gọi từ `SnakebiteIncidentService`, `RescueMissionService`, etc.)_

✅ Các method hiện đang tồn tại trong `SignalROperatorRealtimeNotificationService`:

- `NotifyNewIncidentCreatedAsync` → mới tạo incident, broadcast cho operator
- `NotifyIncidentClaimedAsync`
- `NotifyOperatorContactingAsync`
- `NotifyDispatchRequestedAsync`
- `NotifyIncidentFalseAlarmAsync`
- `NotifyIncidentNoAnswerAsync`
- `NotifyRescuerDispatchedAsync`
- `NotifyIncidentCancelledAsync`
- `NotifyRescuerDeclinedAsync`
- `NotifyRescuerAbortedAsync` (mới bổ sung)

📌 Đây là **danh sách event mà OperatorHub cần phải nhận**, tức là khi migrate thì `SignalROperatorRealtimeNotificationService` phải gửi xuống `OperatorHub` thay vì `RescuerHub`.

---

### B) Event mà RescuerHub hiện đang broadcast trực tiếp tới operator

Nếu bạn “moved operator out”, các chỗ này phải chuyển qua OperatorHub (bằng IHubContext<OperatorHub>):

- `RescuerHub.JoinAsRescuer` ➜ `Clients.Group(OperatorGroup).SendAsync("RescuerOnlineStatus", ...)`
- `RescuerHub.UpdateLocation` ➜ `Clients.Group(OperatorGroup).SendAsync("RescuerIdleLocationUpdated", ...)`
- `RescuerHub.AcceptDispatchRequest` ➜ `Clients.Group(OperatorGroup).SendAsync("RescuerAccepted", ...)`
- `RescuerHub.DeclineDispatchRequest` ➜ `Clients.Group(OperatorGroup).SendAsync("RescuerDeclined", ...)`

=> **Cần thay bằng IHubContext<OperatorHub>** và gửi sự kiện qua đó.

---

### C) Event hiện do `MissionHub` gửi cho operator

- `MissionHub.UpdateLocation` đang gọi `_rescuerHubContext.Clients.Group(OperatorGroup).SendAsync("IncidentLocationUpdated", ...)`

✅ Cần chuyển sang dùng `IHubContext<OperatorHub>`.

---

## 🧩 Những file/method cần sửa/chú ý (điểm cụ thể)

### 1) File cần tạo/sửa

- `SnakeAid.Api/Hubs/OperatorHub.cs` (mới)
- Program.cs (map hub mới + register DI nếu cần)
- SignalROperatorRealtimeNotificationService.cs (chuyển sang `IHubContext<OperatorHub>`)
- RescuerHub.cs (bỏ logic operator & gửi các sự kiện operator qua hub mới)
- MissionHub.cs (use operator hub context cho `IncidentLocationUpdated`)

### 2) Service gọi tới operator

Các service hiện đang gọi `_operatorRealtimeNotificationService`:

- `SnakebiteIncidentService` (claim / contact / dispatch / false alarm / no answer / cancel)
- `RescueMissionService` (abort -> `NotifyRescuerAbortedAsync`)

✅ Đây là “điểm an toàn”: service không cần đổi nếu chỉ đổi nội bộ của `SignalROperatorRealtimeNotificationService` sang OperatorHub.

---

## 🧭 Phần “disconnect” / release incident (theo doc refactor-sos.md)

Khi Operator nằm ở `OperatorHub`:

- Implement `OnDisconnectedAsync` để:
  - Kiểm tra nếu operator đang “giữ” incident (HandlingOperatorId != null) → giải phóng
  - Nếu ngắt kết nối > X phút (có thể dùng in-memory tracking + background task) thì set OffDuty
- Giúp avoid “Operator rời nhưng incident vẫn bị giữ” (EC-5 trong docs)

---

## ✅ Kết luận ngắn gọn

**Nếu triển khai migration**, cốt lõi bạn phải làm 2 việc:

1. **Operator kết nối vào hub riêng** (`OperatorHub`)
2. **Tất cả event/notification đến operator phải gửi qua hub đó**, không còn gửi qua `RescuerHub` nữa.
