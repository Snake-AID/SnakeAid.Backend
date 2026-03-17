## ✅ UI đề xuất cho Operator Dashboard (Map + Rescuer list + SOS cases)

Để operator “nhìn thấy” được toàn bộ trạng thái và dễ thao tác, mình gợi ý một layout **3 vùng chính**:

---

## 1) **Bản đồ lớn (Map)** — trung tâm

- **Dot rescuer**: màu/biểu tượng khác nhau
  - **Xanh lá**: online & available (có thể nhấn để dispatch)
  - **Vàng**: online nhưng đang bận (assigned / enroute)
  - **Đỏ**: offline (còn trong ca nhưng mất kết nối)
- **Dot incident**: mỗi SOS là một dot riêng
  - **Xanh dương**: “Pending / Verified” (chờ dispatch)
  - **Cam**: đã dispatch (đang chờ rescuer accept)
  - **Tím**: assigned (rescuer đã accept)
  - **Đỏ**: false-alarm / cancelled / finished (có thể ẩn)

**Cơ chế:**  

- Khi có SOS mới (event “NewIncidentCreated”) → dot mới nhấp nháy + toast popup.
- Khi có cancel / false alarm → dot bớt nổi, có animation fade out.

---

## 2) **Sidebar (danh sách case cần xử lý)**  

**(Có thể là 1 tab / panel bên trái)**

### 2.1 “Queue” — Incident đang chờ dispatch (Verified)

Mỗi dòng / card hiển thị:

- ID / time tạo
- Vị trí (khoảng cách tới từng rescuer)
- Trạng thái (Pending / Contacting / Verified / Dispatched / Assigned)
- Nút **“Dispatch”** (mở modal chọn rescuer hoặc click trên map)
- Nút **“Cancel Dispatch”** (hiển thị khi trạng thái là Dispatched, cho phép Operator thu hồi request nếu Rescuer không phản hồi)
- Event history (lần gọi info, reporter, notes)

> Khi event `RescuerAborted` / `RescuerDeclined` xảy ra:
>
> - Thêm một badge “Needs redispatch” / highlight card, giữ ở top.
> - Mở pop‑up (toast) “Rescuer X aborted mission trên case Y → dispatch lại”.

### 2.2 “In Progress” — Case đang có rescuer (Assigned / EnRoute / Arrived)

- Hiển thị rescuer hiện tại, ETA (nếu có), status mission.
- Nút “Cancel mission” / “Abort mission” (nếu support).

### 2.3 “History / Done” — case đã xong/cancel/false alarm

---

## 3) **Sidebar (Rescuer list + shift status)**  

**(Có thể tab khác hoặc phía dưới map)**

### Rescuer list (filterable)

- Mỗi rescuer hiển thị:
  - Tên, rating, ca (shift) hiện tại
  - Trạng thái: **Online + Available / Online but Busy / Offline**
  - Vị trí (last location)
  - Nút “Dispatch to this rescuer” (khi đang có case cần dispatch)
- Các filter:
  - **Only in shift** (ShiftAssignment.Active)
  - **Only online**
  - **Nearby** (dựa trên incident đang chọn)

---

## 🔔 Khi SOS tới (new incident)

- **Popup / toast**: “New SOS received (Incident #xxx) – Click để mở card”
- **Card tự mở** (nếu UI đang trong “queue view”): hiển thị chi tiết, vị trí, và nút “Dispatch”
- **Map focus**: zoom tới incident dot + highlight

---

## 🔁 Khi Rescuer accept mission

Event: `RescuerAccepted`

- UI cần:
  - **Chuyển case từ “Queue” → “In Progress”**
  - Dùng badge “Rescuer X nhận lệnh” + hiển thị thời gian
  - Map: kết nối line (incident ↔ rescuer), hiển thị dot rescuer màu khác
  - Có thể show panel “Send message to rescuer / call”

---

## ❌ Khi Rescuer abort mission

Event: `RescuerAborted`

- UI cần:
  - **Highlight lại case** (thêm badge “Needs redispatch” / “Rescuer aborted”)
  - **Toast/pop-up** ngay khi event đến:
    > “Rescuer [X] đã abort mission cho case [Y]. Vui lòng chọn rescuer khác.”
  - **Map**: đổi dot incident về trạng thái “sẵn sàng dispatch” (ví dụ màu xanh) và line kết nối cũ biến mất
  - **Giữ case ở top queue** (nên chọn một phím tắt “Re-dispatch now”)

---

## ✅ Đề xuất “UI box trạng thái case”

Một modal / side panel “Case Detail” khi bấm vào incident:

- **Header**: ID + trạng thái + timestamp
- **Section Current Rescuer** (nếu có)
- **Timeline**:
  - Created → Claimed → Contacting → Verified → Dispatched → Assigned → EnRoute → Arrived → Completed
  - Highlight step hiện tại
- **Actions**:
  - Dispatch (chọn rescuer)
  - Cancel Dispatch (thu hồi request đang chờ Rescuer phản hồi)
  - Cancel
  - Mark False Alarm
  - Mark No Answer (với option tiếp tục gọi / release)

---

## 💡 Tóm lại: “Incident case dialog” nên có

- Thông tin **current state**
- Danh sách **available rescuer** để dispatch (liên kết tới map)
- **Thông báo real-time** khi rescuer abort/decline → UI phải đẩy case này lên top và show alert
- Áp dụng `RescuerAborted` event để bật **badge + toast + focus (scroll)** — để Operator không bỏ sót

## 📌 Endpoint: `GET /api/monitoring/on-duty`

### ✅ Mục đích

Cung cấp **danh sách rescuer “đang ca trực”** (on-duty) để UI operator:

- Hiển thị **danh sách candidate để dispatch** cho một incident
- Hiển thị trên bản đồ (location + distance)
- Lọc theo **online/available**, **khoảng cách**, **đã từ chối/abort cho incident này**

Đây là nguồn dữ liệu “độ tin cậy cao” để UI lựa rescuer trước khi gửi dispatch request.

---

## 🧭 Request

### URL

```
GET /api/monitoring/on-duty
```

### Query params

| Param | Loại | Ý nghĩa | Ví dụ |
|------|------|---------|-------|
| `incidentId` | `Guid` | (tùy chọn) Nếu cung cấp, endpoint sẽ loại bỏ rescuer đã **decline** hoặc **abort mission** cho incident này | `?incidentId=...` |
| `date` | `DateOnly` | (tùy chọn) Hiển thị ca trực cho ngày cụ thể (mặc định: hôm nay) | `?date=2026-03-17` |
| `onlyAvailable` | `bool` | Nếu true chỉ lấy rescuer **online + available** | `?onlyAvailable=true` |
| `maxDistanceKm` | `double` | (tùy chọn) Giới hạn rescuer chỉ trong bán kính (km) so với location incident | `?maxDistanceKm=10` |

---

## ✅ Response model (đã có)

Endpoint trả về:

```json
{
  "incidentId": "guid|null",
  "date": "2026-03-17",
  "snapshotAt": "2026-03-17T09:00:00Z",
  "rescuers": [ ... ]
}
```

---

## 🧩 Trường trong `OnDutyRescuerItemResponse`

| Field | Loại | Ý nghĩa (frontend) |
|------|------|-------------------|
| `rescuerId` | `Guid` | ID dùng để gọi dispatch request |
| `fullName` | `string` | Tên hiển thị |
| `phoneNumber` | `string?` | Có thể dùng khi cần gọi ngoài app |
| `isOnline` | `bool` | Cho biết rescuer đang online (client hoặc hub vẫn kết nối) |
| `isAvailable` | `bool` | Cho biết rescuer hiện “có thể nhận request” (không bận) |
| `isOnDutyNow` | `bool` | Rescuer đang nằm trong ca trực **và** đúng thời điểm ca đang diễn ra |
| `assignmentStatus` | `string` | Trạng thái `ShiftAssignment` (ví dụ: `Scheduled`, `Active`) |
| `shiftAssignmentId` | `Guid` | ID record ca trực (dùng để audit/tracing) |
| `shiftId` | `Guid` | ID ca (Shift) |
| `shiftName` | `string` | Tên ca, hiển thị trong UI |
| `shiftStartTime` | `TimeSpan` | Giờ bắt đầu ca (để UI show/so sánh) |
| `shiftEndTime` | `TimeSpan` | Giờ kết thúc ca |
| `shiftDate` | `DateOnly` | Ngày ca (dùng để so sánh/nhiều ngày) |
| `latitude` | `double?` | Vị trí hiện tại rescuer (nếu cập nhật) |
| `longitude` | `double?` | Vị trí hiện tại rescuer |
| `lastLocationUpdate` | `DateTime?` | Thời điểm rescuer cập nhật vị trí lần cuối |
| `distanceKm` | `double?` | Khoảng cách từ loc incident đến rescuer (tính bằng DB PostGIS) |

---

## 🧠 Các lưu ý quan trọng khi dùng endpoint này

- **Không trả các rescuer ngoài ca trực** — chỉ những người có `ShiftAssignment` status `Scheduled` hoặc `Active`.
- **Nếu gửi `incidentId` thì sẽ loại bỏ** rescuer:
  - đã *decline* request cho incident ấy
  - đã *abort mission* cho incident ấy
- `distanceKm` được tính bằng **PostGIS** (EF.Functions.Distance) → chính xác & hiệu quả.

---

## ✅ Cách frontend nên dùng

1. Khi cần dispatch: gọi
   - `GET /api/monitoring/on-duty?incidentId={id}&onlyAvailable=true&maxDistanceKm=10`
2. Hiển thị danh sách, ưu tiên:
   - `isOnDutyNow == true`
   - `distanceKm` nhỏ hơn
   - `isAvailable == true`
3. Trước khi hủy dispatch request, lấy danh sách request hiện tại để xác định `requestId`:
   - `GET /api/incidents/{incidentId}/dispatch-requests`

   ### ✅ Response schema (GET /api/incidents/{incidentId}/dispatch-requests)

   ```json
   {
     "success": true,
     "message": "Dispatch requests retrieved.",
     "data": [
       {
         "requestId": "guid",
         "rescuerId": "guid",
         "rescuerName": "string",
         "rescuerPhone": "string",
         "status": "Pending|Accepted|Declined|Cancelled",
         "createdAt": "2026-03-17T10:00:00Z",
         "responseAt": "2026-03-17T10:05:00Z|null",
         "declineReason": "string|null"
       }
     ]
   }
   ```

   > ⚠️ `status` là `RescueRequestStatus` (có thể là `Pending`, `Accepted`, `Declined`, `Cancelled`)

4. Khi chọn rescuer, gọi:
   - `POST /api/incidents/{incidentId}/dispatch`
5. Khi cần hủy dispatch request (nếu rescuer không phản hồi):
   - `POST /api/incidents/dispatch-requests/{requestId}/cancel`

---

## 📌 Endpoint: `POST /api/incidents/dispatch-requests/{requestId}/cancel`

### ✅ Mục đích

Cho phép Operator chủ động thu hồi (cancel) một dispatch request đã gửi cho Rescuer nhưng chưa được phản hồi (đang ở trạng thái `Pending`).

### 🧭 Request

```
POST /api/incidents/dispatch-requests/{requestId}/cancel
```

- **`requestId`**: ID của `RescuerRequest` (được trả về khi gọi API dispatch).

### ✅ Response Model

```json
{
  "success": true,
  "message": "Dispatch request cancelled.",
  "data": {
    "requestId": "guid",
    "rejectedAt": "2026-03-17T10:00:00Z",
    "message": "Dispatch request was cancelled by operator."
  }
}
```

### 🧠 Logic xử lý

- Kiểm tra request có tồn tại và đang ở trạng thái `Pending` hay không.
- Kiểm tra Operator thực hiện request có phải là người đang xử lý Incident này không.
- Cập nhật trạng thái request thành `Cancelled` với lý do "Cancelled by Operator".
- Đưa Incident trở lại trạng thái `Verified` để có thể dispatch cho Rescuer khác.
- Gửi real-time notification cho Rescuer để đóng popup yêu cầu nhận nhiệm vụ.
- Gửi real-time notification cho Operator dashboard để cập nhật UI.

---

Nếu muốn, mình có thể bổ sung phần **OpenAPI/Swagger snippet** (request + response schema) để frontend tự generate types/contract.
