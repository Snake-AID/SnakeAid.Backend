## Tổng kết Refactor: Ride-hailing → Rescue Center Dispatch

---

### Phần 1 — Database Models

**Models bị xóa hoàn toàn:**

```
RescueRequestSession    → Bỏ (không còn ping vòng)
SessionTimeoutBackgroundService → Bỏ (không còn auto-timeout)
```

**Models được sửa:**

`RescuerRequest`:

```
- Id
- IncidentId
- RescuerId
- OperatorId              [mới]
- Status: Pending | Accepted | Declined | Cancelled
- DispatchedAt
- ResponseAt
- DeclineReason (nullable)
```

`SnakebiteIncident` — thêm fields + mở rộng Status:

```
Thêm fields:
- HandlingOperatorId (FK nullable)
- OperatorNotes
- DispatchedAt (nullable)
- ConfirmedAt (nullable)

Bỏ các field liên quan đến session và tính toán phí dư thừa (Estimated cost,...)

Status mới:
Pending              → Chờ Operator nhận
OperatorContacting   → Operator đang gọi xác nhận [mới]
Confirmed            → Xác nhận thật, chờ điều phối [mới]
Dispatched           → Đã điều phối rescuer [mới]
Assigned             → Rescuer đã acknowledge, đang chuẩn bị
EnRoute              → (move từ Mission lên Incident level) [cân nhắc]
FalseAlarm           → Báo động giả [mới]
Cancelled            → Giữ nguyên
NoRescuerFound       → Giữ nguyên
Finished             → Giữ nguyên
```

**Models mới:**

`OperatorProfile`:

```
- Id
- AccountId (FK)
- IsOnDuty (bool)
- CurrentCaseCount
- MaxConcurrentCases (default 3)
- IsAcceptingNew (bool)   → toggle thủ công khi cần nghỉ
```

`WorkShift` (template ca trực):

```
- Id
- Name                    → "Ca sáng", "Ca chiều", "Ca đêm"
- StartTime (TimeSpan)    → 06:00
- EndTime (TimeSpan)      → 14:00
- RequiredRescuers (int)
```

`ShiftAssignment` (instance thực tế theo ngày):

```
- Id
- RescuerId
- ShiftId
- Date
- Status: Scheduled | Active | Completed | Cancelled | NoShow
- CheckInAt (nullable)
- CheckOutAt (nullable)
- Notes
```

hãy xem cần thiết có entity này không, khi trong incident đã có 1 status FalseAlarm
`IncidentCallLog`:

```
- Id
- IncidentId
- OperatorId
- CalledAt
- Duration (nullable, seconds)
- Outcome: Confirmed | FalseAlarm | NoAnswer | Cancelled
- Notes
```

---

### Phần 2 — Luồng nghiệp vụ chính

**Luồng Happy Path:**

```
1. Member tạo SOS
   → Incident tạo, Status = Pending
   → SignalR broadcast "NewIncidentCreated" → group "Operators"

2. Operator claim incident
   → UPDATE Incident SET HandlingOperatorId = @opId 
     WHERE HandlingOperatorId IS NULL  (optimistic lock)
   → Status = OperatorContacting
   → SignalR "IncidentClaimed" → các Operator còn lại (ẩn khỏi queue)
   → SignalR "OperatorContacting" → Member (biết có người đang xử lý)

3. Operator gọi xác nhận (Mức 1: gọi điện thật, log outcome)
   → Operator thấy SĐT Member trên dashboard
   → Gọi xong → chọn outcome trong app
   → Nếu Confirmed: Status = Confirmed, tạo IncidentCallLog
   → Nếu FalseAlarm: Status = FalseAlarm, đóng case
   → Nếu NoAnswer: Operator quyết định tiếp tục hay đóng

4. Operator điều phối Rescuer
   → Xem map: rescuer đang trực ca (ShiftAssignment.Status = Active)
     + IsAvailable = true + IsOnline = true
   → Chọn rescuer → POST /api/incidents/{id}/dispatch
   → Tạo DispatchRequest (Status = Pending)
   → Incident Status = Dispatched
   → SignalR "MissionAssigned" → Rescuer cụ thể
   → SignalR "RescuerDispatched" → Member

5. Rescuer acknowledge
   → Rescuer bấm "Đã nhận lệnh" trên app
   → DispatchRequest Status = Accepted
   → Incident Status = Assigned
   → RescuerProfile.IsAvailable = false
   → SignalR "RescuerAccepted" → Operator dashboard + Member

6. Mission diễn ra (giữ nguyên flow hiện tại)
   → EnRoute → RescuerArrived → MissionCompleted
```

---

### Phần 3 — Edge Cases

**EC-1: 2 Operator cùng claim 1 incident**

```
Solution: Optimistic locking
- Dùng Concurrency Token (RowVersion) trên Incident
- Hoặc: UPDATE ... WHERE HandlingOperatorId IS NULL
- Người thua nhận HTTP 409 Conflict
- Frontend tự refresh list, case đó disappear khỏi queue
```

**EC-2: Incident Pending quá lâu không ai nhận**

```
Cần giữ lại 1 BackgroundService đơn giản:
- Check mỗi 30s: Incident Pending > 2 phút
- Push notification escalate đến tất cả Operator đang trực
- > 5 phút: notify Supervisor role (nếu có)
- Log cảnh báo để review sau
```

**EC-3: Rescuer decline lệnh điều phối**

```
DispatchRequest Status = Declined (kèm DeclineReason)
→ SignalR "RescuerDeclined" → Operator dashboard
→ Incident Status quay về Confirmed (chờ điều phối lại)
→ RescuerProfile.IsAvailable vẫn = true (chưa nhận mission)
→ Operator chọn rescuer khác
```

**EC-4: Member hủy SOS sau khi đã Dispatched**

```
→ Incident Status = Cancelled
→ DispatchRequest Status = Cancelled
→ SignalR notify Rescuer đang trên đường
→ RescuerProfile.IsAvailable = true
→ Operator nhận notification "Member đã hủy"
```

**EC-5: Operator ngắt kết nối đột ngột giữa chừng**

```
OnDisconnected của OperatorHub:
→ Nếu có case đang ở OperatorContacting > 3 phút
  → Tự động release HandlingOperatorId = null
  → Incident trở về Pending
  → Broadcast lại cho các Operator còn trực
→ SetOffDuty nếu disconnect quá 5 phút không reconnect
```

**EC-6: Không có Rescuer nào available để dispatch**

```
Operator thấy map trống / filter không có kết quả
→ UI hiển thị rõ "Không có rescuer đang trực"
→ Operator có thể:
  a. Gọi điện trực tiếp cho rescuer ngoài app (manual)
  b. Đánh dấu "NoRescuerFound" + ghi note
  c. Liên hệ trung tâm khác (nếu có liên kết)
```

**EC-7: Rescuer check-in ca trực nhưng không online trên app**

```
ShiftAssignment.Status = Active nhưng RescuerProfile.IsOnline = false
→ Filter dispatch chỉ lấy IsOnline = true AND ShiftAssignment Active
→ Dashboard hiển thị warning "X rescuer trực ca nhưng chưa online"
→ Operator có thể gọi điện nhắc rescuer mở app
```

**EC-8: FalseAlarm nhưng Member gọi lại sau**

```
Cho phép Member tạo incident mới bình thường
Không block account (có thể thêm flag "repeated false alarm" để review)
```

---

### Phần 4 — API Endpoints mới/thay đổi

```
# Operator - Incident Management
POST   /api/incidents/{id}/claim          → Claim để xử lý
POST   /api/incidents/{id}/contact        → Bắt đầu gọi xác nhận
POST   /api/incidents/{id}/confirm        → Xác nhận thật
POST   /api/incidents/{id}/false-alarm    → Báo động giả
POST   /api/incidents/{id}/dispatch       → Điều phối rescuer {rescuerId}
POST   /api/incidents/{id}/redispatch     → Đổi rescuer
GET    /api/incidents?status=Pending&...  → Queue dashboard

# Rescuer - Dispatch
POST   /api/dispatch/{requestId}/accept   → Acknowledge nhận lệnh
POST   /api/dispatch/{requestId}/decline  → Báo không đi được

# Rescuer availability
GET    /api/rescuers/on-duty              → Rescuer đang trực + IsAvailable + location

# Shift Management
GET    /api/shifts                        → List ca trực
POST   /api/shifts                        → Tạo ca
PUT    /api/shifts/{id}                   → Sửa ca
POST   /api/shifts/{id}/assign            → Assign rescuer vào ca
PATCH  /api/shifts/assignments/{id}/checkin
PATCH  /api/shifts/assignments/{id}/checkout

# Operator Management  
GET    /api/operators/on-duty             → Operator đang trực
PATCH  /api/operators/me/duty-status      → Toggle on/off duty
```

**Endpoints bị xóa:**

```
POST /api/incidents/{id}/raise-range      → Không còn radius expansion
(SessionController nếu có)               → Xóa toàn bộ
```

---

### Phần 5 — SignalR

**Hubs:**

```
RescuerHub     → Giữ, bỏ AcceptRequest method, thêm AcknowledgeDispatch
MissionHub     → Giữ nguyên (tracking vị trí)
OperatorHub    → Mới (web dashboard realtime)
```

**Events mới:**

```
Server → Operator:
  NewIncidentCreated      → Incident mới vào queue
  IncidentClaimed         → Có người claim rồi (ẩn khỏi queue)
  IncidentReleased        → Operator disconnect, case về queue
  RescuerDeclined         → Rescuer từ chối, cần dispatch lại
  RescuerOnlineStatus     → Rescuer vừa online/offline (map update)
  ShiftCheckin            → Rescuer vừa check-in ca (thêm vào available pool)

Server → Member:
  OperatorContacting      → Có Operator đang xử lý case của bạn
  RescuerDispatched       → Đã cử người đến
  RescuerAccepted         → Rescuer xác nhận đang đến
  (giữ) RescuerEnRoute, RescuerArrived, MissionCompleted

Server → Rescuer:
  MissionAssigned         → Thay thế NewRescueRequest
  DispatchCancelled       → Operator hủy lệnh điều phối
```

---

### Phần 6 — Công nghệ

**Giữ nguyên:**

```
- ASP.NET Core + SignalR   → Vẫn đủ tốt
- PostgreSQL + PostGIS      → Vẫn dùng cho map rescuer
- EF Core                  → Giữ
- Background Service        → Giữ nhưng đơn giản hơn nhiều
```

**Thêm mới:**

```
- React/Next.js (web dashboard Operator)
  → Mapbox GL JS hoặc Google Maps JS SDK (xem rescuer realtime)
  → @microsoft/signalr (npm) để nhận events
  → React Query hoặc SWR cho data fetching

- Concurrency Token (EF Core built-in)
  → [Timestamp] attribute hoặc IsRowVersion()
  → Xử lý DbUpdateConcurrencyException
```

**Cân nhắc thêm nếu scale:**

```
- Redis Pub/Sub            → Nếu sau này multi-instance server
                             (hiện tại SignalR in-memory đủ dùng)
- Hangfire / Quartz.NET    → Thay BackgroundService nếu cần
                             persistent job (ca trực reminder, etc.)
```

---

### Phần 7 — Thứ tự implement đề xuất

```
Sprint 1 (Core flow):
  1. Tạo models mới (OperatorProfile, DispatchRequest, WorkShift, ShiftAssignment)
  2. Migrate bỏ RescueRequestSession
  3. OperatorHub + claim/confirm/dispatch APIs
  4. Rescuer acknowledge flow

Sprint 2 (Shift management):
  5. WorkShift CRUD
  6. ShiftAssignment checkin/checkout
  7. Filter rescuer theo ca trực

Sprint 3 (Dashboard web):
  8. Operator web dashboard
  9. Map realtime rescuer
  10. Queue management UI

Sprint 4 (Edge cases + polish):
  11. Background escalation service
  12. Concurrency handling
  13. CallLog + FalseAlarm flow
```

Mình sẽ chuyển ý này thành luồng kỹ thuật cụ thể và implement phần xương sống luôn: realtime feed cho Operator và API lấy danh sách rescuer đang trực có lọc theo ca + khoảng cách tới incident. Trước tiên mình rà các service/location hiện có để nối vào đúng chỗ.

Searched for regex `interface IRescuerLocationService|class RescuerLocationService|IRescuerOnlineStatusService|SetOnlineAsync|UpdateLocationAsync` (`**/*.cs`), 13 results

Searched for files matching `**/SnakeAid.Api/Controllers/*Rescuer*.cs`, no matches

Read [](file:///d%3A/snake-aid/SnakeAid.Backend/SnakeAid.Service/Interfaces/IRescuerLocationService.cs)

Searched for regex `Distance|ST_Distance|geometry|LastLocation|NtsGeometryServices|Coordinate` (`**/SnakeAid.Service/**/*.cs`), 20 results

Read [](file:///d%3A/snake-aid/SnakeAid.Backend/SnakeAid.Service/Implements/RescuerLocationService.cs)

Read [](file:///d%3A/snake-aid/SnakeAid.Backend/SnakeAid.Service/Implements/RescuerOnlineStatusService.cs)

Cách làm chuẩn để giữ 2 hub hiện tại và vẫn cho Operator thấy bản đồ realtime là tách rõ 2 lớp: snapshot qua API và streaming qua hub.

1. Giữ vai trò từng hub
1. RescuerHub.cs:

- Rescuer kết nối, cập nhật trạng thái online và vị trí khi rảnh.
- Đây là nguồn dữ liệu vị trí để Operator xem bản đồ chọn người.

2. MissionHub.cs:

- Chỉ cho luồng mission đã assign, realtime giữa Member và Rescuer.
- Không dùng MissionHub làm nguồn bản đồ tổng cho Operator.

1. Luồng cho dashboard Operator
1. Khi mở dashboard:

- Gọi API snapshot danh sách rescuer đủ điều kiện.
- Điều kiện: đang trực ca, online, available, có location.

2. Sau đó subscribe realtime:

- Operator join một group chung, ví dụ Operators.
- Mỗi lần Rescuer online/offline hoặc đổi vị trí, server push event cho Operators.

3. Khi Operator chọn người:

- Gọi endpoint dispatch hiện có để assign incident.
- Server recheck điều kiện tại thời điểm assign để tránh stale data.

1. Điều kiện lọc rescuer để hiện trên map
1. ShiftAssignment:

- Date là hôm nay.
- Status là Active (hoặc Scheduled nếu nghiệp vụ cho phép pre-activate).

2. RescuerProfile:

- IsOnline = true.
- IsAvailable = true.
- LastLocation khác null.

3. Near incident:

- Nếu có incidentId, tính khoảng cách từ incident đến rescuer và sort theo gần nhất.
- Trả về distanceKm cho UI.

1. API snapshot nên có
1. GET api/rescuers/on-duty

- Query: date, incidentId, maxDistanceKm, onlyAvailable.
- Trả về: rescuerId, fullName, lat, lng, isOnline, isAvailable, shift info, distanceKm.

2. Option bổ sung:

- GET api/incidents/{id}/candidate-rescuers để đóng gói filter theo incident.

1. Event realtime nên có cho Operator
1. RescuerOnlineStatus:

- userId, isOnline, isAvailable, timestamp.

2. RescuerLocationUpdated:

- userId, lat, lng, updatedAt.

3. ShiftCheckin và ShiftCheckout:

- assignmentId, rescuerId, shiftId, date, status.

1. Mapping vào code hiện tại
1. Ở RescuerHub.cs:

- Trong JoinAsRescuer, OnDisconnectedAsync, UpdateLocation: push thêm event sang group Operators.

2. Tạo Operator channel riêng:

- Có thể thêm OperatorHub mới hoặc thêm JoinAsOperator trong RescuerHub.
- Nếu muốn sạch kiến trúc, nên có OperatorHub riêng.

3. Ở service dispatch:

- Trước khi dispatch, kiểm tra lại shift active + online + available.

Kết luận:

- Operator map realtime nên lấy dữ liệu từ RescuerHub (nguồn vị trí tổng) + API snapshot ban đầu.
- MissionHub giữ đúng vai trò mission tracking giữa Member và Rescuer sau khi assign.
- Cách này scale tốt, UI mượt, và không làm loạn semantics của từng hub.

Nếu bạn muốn, mình triển khai luôn bản tối thiểu trong bước tiếp theo:

1. Thêm endpoint GET api/rescuers/on-duty.
2. Bổ sung push event Operators trong RescuerHub.
3. Thêm JoinAsOperator group để dashboard nhận feed realtime ngay.
