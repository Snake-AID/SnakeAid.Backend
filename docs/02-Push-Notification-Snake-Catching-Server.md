# PUSH NOTIFICATION — SERVER SPECIFICATION — LUỒNG BẮT RẮN

## Thông tin tài liệu

| Trường | Nội dung |
|---|---|
| **Tên dự án** | SnakeAid — AI-Powered Snakebite First Aid & Rescue |
| **Module** | Snake Catching — Push Notification (Server-side) |
| **Phiên bản** | 1.0 |
| **Ngày tạo** | 02/04/2026 |
| **Mô hình** | v2.0 — Operator-based |
| **Phạm vi** | Chỉ bao gồm **Firebase FCM Push Notification** do server gửi |

---

## MÔ TẢ CHUNG

Tài liệu này là **đặc tả kỹ thuật cho phía server** — mô tả chính xác **khi nào**, **gửi cho ai**, **tiêu đề và nội dung** của từng Push Notification trong luồng bắt rắn.

### Kênh gửi
- **Firebase FCM** — gửi qua FCM token của thiết bị
- Áp dụng khi: app đang ở nền (`background`) hoặc đã đóng (`terminated`)
- Khi app đang mở (`foreground`): client tự xử lý hiển thị In-App / Toast

### Payload chuẩn (FCM Data Message)

```json
{
  "token": "<fcm_device_token>",
  "notification": {
    "title": "<Tiêu đề>",
    "body": "<Nội dung>"
  },
  "data": {
    "notificationType": "<loại thông báo>",
    "requestId": "<id>",
    "missionId": "<id nếu có>"
  },
  "android": {
    "priority": "high"
  },
  "apns": {
    "headers": { "apns-priority": "10" }
  }
}
```

### Nhóm nhận thông báo

| Ký hiệu | Vai trò | FCM Token nguồn |
|---|---|---|
| 👤 **Member** | Người tạo yêu cầu | `users.fcm_token` theo `memberId` |
| 🦺 **Rescuer** | Cứu hộ viên được giao | `users.fcm_token` theo `rescuerId` |
| 🧑‍💼 **Operator** | Điều phối viên | `users.fcm_token` theo role `Operator` (broadcast) |

---

## DANH SÁCH PUSH NOTIFICATION THEO TRIGGER

---

### [PN-01] Tạo yêu cầu bắt rắn thành công

**Trigger:** `POST /api/snakecatching/requests` — tạo đơn thành công  
**Trạng thái đơn:** `Pending`  
**notificationType:** `SNAKE_CATCHING_REQUEST_CREATED`

| Người nhận | Tiêu đề | Nội dung |
|---|---|---|
| 🧑‍💼 Operator (tất cả) | `📋 Đơn bắt rắn mới!` | `Có yêu cầu bắt rắn mới từ {memberName} — {address} — {snakeSpecies nếu có}. Vui lòng xem xét và xác nhận.` |

---

### [PN-02] Thanh toán đặt cọc thành công (Round 1)

**Trigger:** Xác nhận thanh toán khi transactionType == `CatchingDeposit` thành công  
**Trạng thái đơn:** `Pending / Confirmed / Assigned`  
**notificationType:** `SNAKE_CATCHING_DEPOSIT_SUCCESS`

| Người nhận | Tiêu đề | Nội dung |
|---|---|---|
| 👤 Member | `✅ Đặt cọc thành công` | `Bạn đã đặt cọc thành công {depositAmount} VNĐ cho đơn #{requestId}. Đơn của bạn sẽ được ưu tiên xử lý.` |
| 🧑‍💼 Operator | `Member đã đặt cọc` | `{memberName} đã thanh toán đặt cọc cho đơn #{requestId}. Ưu tiên xử lý đơn này.` |

---

### [PN-03] Đơn hết hạn — Không được xử lý kịp

**Trigger:** Hệ thống tự động — timeout `Pending → Expired`  
**notificationType:** `SNAKE_CATCHING_REQUEST_EXPIRED`

| Người nhận | Tiêu đề | Nội dung |
|---|---|---|
| 👤 Member | `⚠️ Yêu cầu hết hạn` | `Yêu cầu #{requestId} của bạn đã hết hạn do không có điều phối viên xử lý kịp thời. Vui lòng tạo yêu cầu mới hoặc gọi hotline hỗ trợ.` |

---

### [PN-04] Operator xác nhận đơn

**Trigger:** `PATCH /api/snakecatching/requests/confirm/{requestId}` -> Operator cập nhật trạng thái đơn → `Confirmed`  
**notificationType:** `SNAKE_CATCHING_REQUEST_CONFIRMED`

| Người nhận | Tiêu đề | Nội dung |
|---|---|---|
| 👤 Member | `✅ Yêu cầu đã được xác nhận` | `Yêu cầu #{requestId} đã được xác nhận! Điều phối viên đang tìm cứu hộ viên phù hợp cho bạn.` |

---

### [PN-05] Operator gán Rescuer — Nhiệm vụ được phân công

**Trigger:** `POST /api/snakecatching/requests/assign/{requestId}` Operator gán Rescuer vào đơn → trạng thái `Assigned`  
**notificationType (Member):** `SNAKE_CATCHING_RESCUER_ASSIGNED`  
**notificationType (Rescuer):** `SNAKE_CATCHING_MISSION_ASSIGNED`

| Người nhận | Tiêu đề | Nội dung |
|---|---|---|
| 👤 Member | `🦺 Cứu hộ viên đã được phân công` | `{rescuerName} sẽ đến hỗ trợ bạn! Thời gian di chuyển ước tính: ~{estimatedMinutes} phút.` |
| 🦺 Rescuer | `📋 Bạn được giao nhiệm vụ bắt rắn` | `Nhiệm vụ mới: {snakeSpecies} tại {address} — Khách hàng: {memberName}. Mở app để xem chi tiết.` |

> **Ghi chú:** Rescuer cũng nhận SignalR event `SnakeCatchingMissionAssigned` song song. Push là fallback nếu app đang nền/đóng.

---

### [PN-06] Rescuer bắt đầu di chuyển (EnRoute)

**Trigger:** `PATCH /api/snakecatching/missions/{missionId}/start` — mission status `Preparing → EnRoute`  
**notificationType:** `SNAKE_CATCHING_RESCUER_EN_ROUTE`

| Người nhận | Tiêu đề | Nội dung |
|---|---|---|
| 👤 Member | `🚗 Cứu hộ viên đang di chuyển` | `{rescuerName} đang trên đường đến vị trí của bạn. Dự kiến đến nơi sau khoảng {estimatedMinutes} phút.` |

> **Ghi chú:** Member cũng nhận SignalR event `RescuerEnRoute` nếu đang mở app — Push là fallback.

---

### [PN-07] Rescuer đến nơi (Arrived)

**Trigger:** `PATCH /api/snakecatching/missions/{missionId}/arrived`  
**notificationType:** `SNAKE_CATCHING_RESCUER_ARRIVED`

| Người nhận | Tiêu đề | Nội dung |
|---|---|---|
| 👤 Member | `📍 Cứu hộ viên đã đến!` | `{rescuerName} đã đến nơi và bắt đầu khảo sát hiện trường. Vui lòng ra đón hoặc chỉ dẫn vị trí.` |

---

### [PN-08] Rescuer hoàn thành nhiệm vụ

**Trigger:** `PATCH /api/snakecatching/missions/{missionId}/complete`  
**Trạng thái đơn sau:** `Finished`  
**notificationType:** `SNAKE_CATCHING_MISSION_COMPLETED`

| Người nhận | Tiêu đề | Nội dung |
|---|---|---|
| 👤 Member | `🎉 Nhiệm vụ hoàn thành — Cần thanh toán` | `{rescuerName} đã hoàn thành việc bắt rắn! Phí dịch vụ thực tế: {actualCost} VNĐ. Vui lòng thanh toán để hoàn tất.` |

> **Ghi chú:** Member cũng nhận SignalR event `MissionCompleted` nếu đang mở app. Push là fallback và đồng thời là lời nhắc hành động (CTA).

---

### [PN-09] Thanh toán dịch vụ thành công (Round 2)

**Trigger:** Xác nhận thanh toán cuối cùng khi transactionType == `CatchingPayment` thành công → trạng thái `Completed`  
**notificationType (Member):** `SNAKE_CATCHING_PAYMENT_SUCCESS`  
**notificationType (Rescuer):** `SNAKE_CATCHING_PAYMENT_CONFIRMED`

| Người nhận | Tiêu đề | Nội dung |
|---|---|---|
| 👤 Member | `✅ Đơn hoàn tất` | `Cảm ơn bạn đã sử dụng SnakeAid! Đơn #{requestId} đã hoàn tất. Đừng quên đánh giá dịch vụ để giúp chúng tôi cải thiện.` |
| 🦺 Rescuer | `💰 Thanh toán xác nhận` | `Khách hàng đã thanh toán cho nhiệm vụ #{missionId}. Nhiệm vụ đã kết thúc chính thức. Cảm ơn bạn!` |

> **Ghi chú:** Notification data sẽ bao gồm `paymentMethod` = `Wallet` hoặc `PayOS` để client biết thanh toán được thực hiện qua ví app hay PayOS. Nếu là đặt cọc, `paymentStage` = `deposit`; nếu là thanh toán cuối, `paymentStage` = `final`.

---

### [PN-10] Thanh toán Round 2 thất bại

**Trigger:** Callback lỗi từ PayOS hoặc ví không đủ số dư  
**notificationType:** `SNAKE_CATCHING_PAYMENT_FAILED`

| Người nhận | Tiêu đề | Nội dung |
|---|---|---|
| 👤 Member | `⚠️ Chưa hoàn tất thanh toán` | `Bạn còn {actualCost} VNĐ cần thanh toán cho đơn #{requestId}. Vui lòng mở app và hoàn tất để kết thúc dịch vụ.` |

---

### [PN-11] Member hủy đơn khi đã có Rescuer (`Assigned`)

**Trigger:** `PATCH /api/snakecatching/requests/cancel/{requestId}` khi mission còn `Preparing`  
**notificationType:** `SNAKE_CATCHING_REQUEST_CANCELLED_BY_MEMBER`

| Người nhận | Tiêu đề | Nội dung |
|---|---|---|
| 🦺 Rescuer | `❌ Nhiệm vụ bị hủy` | `Khách hàng {memberName} đã hủy yêu cầu #{requestId}. Nhiệm vụ của bạn được hủy — không cần di chuyển.` |

> **Ghi chú:** Nếu đơn ở `Pending / Confirmed` (chưa có Rescuer), không cần Push cho Rescuer.

---

### [PN-12] Rescuer hủy nhiệm vụ (Abort)

**Trigger:** `PATCH /api/snakecatching/missions/{missionId}/abort`  
**notificationType (Member):** `SNAKE_CATCHING_MISSION_ABORTED`  
**notificationType (Operator):** `SNAKE_CATCHING_REASSIGN_NEEDED`

| Người nhận | Tiêu đề | Nội dung |
|---|---|---|
| 👤 Member | `⚠️ Cứu hộ viên không thể thực hiện` | `Rất tiếc! {rescuerName} không thể hoàn thành nhiệm vụ. Đội SnakeAid đang tìm cứu hộ viên thay thế cho bạn.` |
| 🧑‍💼 Operator (tất cả) | `🚨 Cần phân công lại ngay` | `Rescuer {rescuerName} đã hủy nhiệm vụ cho đơn #{requestId}. Cần phân công cứu hộ viên thay thế ngay!` |

---

## TỔNG HỢP — BẢNG PUSH NOTIFICATION THEO TRIGGER

| Mã | Trigger | Người nhận | notificationType |
|---|---|---|---|
| PN-01 | Tạo đơn thành công | 🧑‍💼 Operator | `SNAKE_CATCHING_REQUEST_CREATED` |
| PN-02a | Đặt cọc thành công | 👤 Member | `SNAKE_CATCHING_DEPOSIT_SUCCESS` |
| PN-02b | Đặt cọc thành công | 🧑‍💼 Operator | `SNAKE_CATCHING_DEPOSIT_SUCCESS` |
| PN-03 | Đơn hết hạn (timeout) | 👤 Member | `SNAKE_CATCHING_REQUEST_EXPIRED` |
| PN-04 | Operator xác nhận đơn | 👤 Member | `SNAKE_CATCHING_REQUEST_CONFIRMED` |
| PN-05a | Operator gán Rescuer | 👤 Member | `SNAKE_CATCHING_RESCUER_ASSIGNED` |
| PN-05b | Operator gán Rescuer | 🦺 Rescuer | `SNAKE_CATCHING_MISSION_ASSIGNED` |
| PN-06 | Rescuer bắt đầu di chuyển | 👤 Member | `SNAKE_CATCHING_RESCUER_EN_ROUTE` |
| PN-07 | Rescuer đến nơi | 👤 Member | `SNAKE_CATCHING_RESCUER_ARRIVED` |
| PN-08 | Hoàn thành nhiệm vụ | 👤 Member | `SNAKE_CATCHING_MISSION_COMPLETED` |
| PN-09a | Thanh toán Round 2 thành công | 👤 Member | `SNAKE_CATCHING_PAYMENT_SUCCESS` |
| PN-09b | Thanh toán Round 2 thành công | 🦺 Rescuer | `SNAKE_CATCHING_PAYMENT_CONFIRMED` |
| PN-10 | Thanh toán Round 2 thất bại | 👤 Member | `SNAKE_CATCHING_PAYMENT_FAILED` |
| PN-11 | Member hủy khi đã có Rescuer | 🦺 Rescuer | `SNAKE_CATCHING_REQUEST_CANCELLED_BY_MEMBER` |
| PN-12a | Rescuer hủy nhiệm vụ | 👤 Member | `SNAKE_CATCHING_MISSION_ABORTED` |
| PN-12b | Rescuer hủy nhiệm vụ | 🧑‍💼 Operator (tất cả) | `SNAKE_CATCHING_REASSIGN_NEEDED` |

---

## QUY TẮC GỬI VÀ ƯU TIÊN

| Mức | Loại | Android priority | APNs priority |
|---|---|---|---|
| **P0 — Khẩn cấp** | PN-12b (Operator cần phân công lại) | `high` | `10` |
| **P1 — Quan trọng** | PN-05, PN-06, PN-07, PN-08, PN-12a | `high` | `10` |
| **P2 — Thông tin** | PN-01, PN-02, PN-03, PN-04, PN-09, PN-10, PN-11 | `normal` | `5` |

---

## ROUTING DEEP LINK (data payload)

Client dùng `notificationType` trong `data` để điều hướng khi người dùng nhấn vào notification:

| notificationType | Deep link |
|---|---|
| `SNAKE_CATCHING_REQUEST_CREATED` | `/operator/requests/{requestId}` |
| `SNAKE_CATCHING_DEPOSIT_SUCCESS` | `/snake-catching/detail/{requestId}` |
| `SNAKE_CATCHING_REQUEST_EXPIRED` | `/snake-catching/history` |
| `SNAKE_CATCHING_REQUEST_CONFIRMED` | `/snake-catching/detail/{requestId}` |
| `SNAKE_CATCHING_RESCUER_ASSIGNED` | `/snake-catching/tracking/{requestId}` |
| `SNAKE_CATCHING_MISSION_ASSIGNED` | `/rescuer/mission/{missionId}` |
| `SNAKE_CATCHING_RESCUER_EN_ROUTE` | `/snake-catching/tracking/{requestId}` |
| `SNAKE_CATCHING_RESCUER_ARRIVED` | `/snake-catching/tracking/{requestId}` |
| `SNAKE_CATCHING_MISSION_COMPLETED` | `/snake-catching/payment/{requestId}` |
| `SNAKE_CATCHING_PAYMENT_SUCCESS` | `/snake-catching/detail/{requestId}` |
| `SNAKE_CATCHING_PAYMENT_CONFIRMED` | `/rescuer/history/{missionId}` |
| `SNAKE_CATCHING_PAYMENT_FAILED` | `/snake-catching/payment/{requestId}` |
| `SNAKE_CATCHING_REQUEST_CANCELLED_BY_MEMBER` | `/rescuer/history/{missionId}` |
| `SNAKE_CATCHING_MISSION_ABORTED` | `/snake-catching/detail/{requestId}` |
| `SNAKE_CATCHING_REASSIGN_NEEDED` | `/operator/requests/{requestId}` |
