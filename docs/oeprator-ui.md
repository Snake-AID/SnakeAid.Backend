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
  - Cancel
  - Mark False Alarm
  - Mark No Answer (với option tiếp tục gọi / release)

---

## 💡 Tóm lại: “Incident case dialog” nên có

- Thông tin **current state**
- Danh sách **available rescuer** để dispatch (liên kết tới map)
- **Thông báo real-time** khi rescuer abort/decline → UI phải đẩy case này lên top và show alert
- Áp dụng `RescuerAborted` event để bật **badge + toast + focus (scroll)** — để Operator không bỏ sót
