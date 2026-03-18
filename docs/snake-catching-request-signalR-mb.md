Searched for regex `SendAsync\(".*SnakeCatching` (`**/*.cs`), 10 results

## 📄 SignalR Events (Rescuer) — Server → Flutter client

Dưới đây là **danh sách các event SignalR** mà server *gửi tới* (hoặc có thể gửi tới) **rescuer** trong hệ thống hiện tại, kèm **payload** và ai nên lắng nghe (subscriber).

---

## 1) `SnakeCatchingRequestCreated`

### 📍 Ai nhận?

- **Tất cả Operator** (group `"Operators"`)
- **User (requestor)** (được gửi đến `Clients.User(userId)`)

> 🔎 *Rescuer* thường không cần lắng nghe event này, nhưng nếu app rescuer cũng muốn nhận thông tin request mới (để hiển thị map / list), thì có thể đăng ký.

### 📦 Payload (server gửi)

```json
{
  "Id": "<Guid>",
  "Address": "<string|null>",
  "Lat": <double|null>,
  "Lng": <double|null>
}
```

✅ `Lat/Lng` được normalize bằng `NormalizeDouble()` (nếu là `Infinity`, `-Infinity`, `NaN` → sẽ biến thành `null`)

---

## 2) `SnakeCatchingRequestAccepted`

### 📍 Ai nhận?

- **Tất cả Operator**
- **User (requestor)**

> *Rescuer* thường không cần lắng nghe, nhưng có thể dùng để biết request đã được xác nhận (confirmed).

### 📦 Payload

```json
{
  "Id": "<Guid>",
  "Status": "<RequestStatus>",
  "ConfirmedAt": "<DateTime|null>",
  "PrePaidAt": "<DateTime|null>",
  "IsPrePaid": <bool>
}
```

---

## 3) `SnakeCatchingRequestAssigned`

### 📍 Ai nhận?

- **Tất cả Operator**
- **User (requestor)**
- **Rescuer được gán** (theo `assignedRescuerId`)

> Đây là event quan trọng nhất cho role **rescuer** — khi rescuer được gán request sẽ nhận event này.

### 📦 Payload

```json
{
  "Id": "<Guid>",
  "Status": "<RequestStatus>",
  "AssignedAt": "<DateTime|null>",
  "AssignedRescuerId": "<Guid|null>",
  "AssignedRescuerName": "<string|null>",
  "AssignedRescuerPhone": "<string|null>"
}
```

---

## 4) `SnakeCatchingRequestCancelled`

### 📍 Ai nhận?

- **Tất cả Operator**
- **User (requestor)**
- **Rescuer đã được gán** (nếu có)

> Dùng để thông báo request đã bị huỷ.

### 📦 Payload

```json
{
  "Id": "<Guid>",
  "UserId": "<Guid>",
  "Status": "<RequestStatus>",
  "CancellationReason": "<string|null>"
}
```

---

## 🧠 Lưu ý kỹ thuật quan trọng (để Flutter side không bị crash)

- **Server đã normalize** giá trị `double` (Lat/Lng/Distance) với `NormalizeDouble()` để tránh `Infinity/NaN` (vì JSON serializer C# sẽ crash nếu gặp).
- **Nếu client cần full dữ liệu chi tiết**, chỉ cần lấy `Id` từ event và gọi API `GET /.../{id}` để lấy full payload (giúp payload SignalR nhẹ và ổn định).

---

## ✅ Gợi ý triển khai Flutter (ngắn gọn)

- Kết nối SignalR bình thường, đăng ký:
  - `"SnakeCatchingRequestAssigned"` → hiển thị notification / navigation
  - `"SnakeCatchingRequestCancelled"` → update UI nếu đang xử lý request
- Khi nhận `SnakeCatchingRequestAssigned`, dùng `Id` để gọi API chi tiết nếu cần hiển thị full thông tin.

---

Nếu bạn muốn mình tạo 1 document chính thức kiểu “SignalR events contract” (có schema/TS type mẫu + chú ý loại Field), mình có thể làm ngay.
