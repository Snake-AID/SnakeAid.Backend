# SOS Rescue - Operator Abort Mission Contract

Cập nhật: 2026-05-06

Tài liệu này mô tả chi tiết luồng **Điều phối viên chủ động hủy nhiệm vụ cứu hộ** (Operator Abort Mission) khi Cứu hộ viên (Rescuer) không phản hồi hoặc gặp sự cố trong quá trình di chuyển.

## 1) Mục tiêu nghiệp vụ

Cho phép Điều phối viên (Operator) can thiệp vào các nhiệm vụ đang thực hiện (`Preparing` hoặc `EnRoute`) để:
- Thu hồi nhiệm vụ từ Rescuer không phản hồi/di chuyển quá lâu.
- Đưa Incident quay lại trạng thái `Verified` để thực hiện điều phối lại (Re-dispatch).
- Đảm bảo Member nhận được sự hỗ trợ kịp thời từ người khác.

---

## 2) API Endpoint cho Frontend (Operator Dashboard)

Dùng để gọi từ giao diện điều phối của Operator.

- **URL**: `PATCH /api/rescue-missions/{missionId}/operator-abort`
- **Authentication**: Yêu cầu quyền `Operator` hoặc `Admin`.
- **Request Body**:
```json
{
  "cancellationReason": "Rescuer không bắt máy, GPS không di chuyển quá 15 phút"
}
```
- **Xử lý phía Backend**:
    - Chuyển `Mission.Status` -> `MissionAborted`.
    - Chuyển `Incident.Status` -> `Verified`.
    - Xóa `AssignedRescuerId` của Incident.
    - Đặt Rescuer hiện tại thành `IsAvailable = true`.
    - Gửi thông báo tới Member, Rescuer và các Operator khác.

---

## 3) SignalR Event cho Flutter (Rescuer App)

Hub liên quan: `RescuerHub`

### 🟢 Event: `MissionAbortedByOperator`

Khi Operator thực hiện hủy nhiệm vụ, Rescuer đang giữ nhiệm vụ đó sẽ nhận được event này để cập nhật UI ngay lập tức.

- **Payload**:
```json
{
  "incidentId": "guid-của-vụ-việc",
  "reason": "Lý do hủy từ operator",
  "message": "Nhiệm vụ của bạn đã bị điều phối viên hủy. Lý do: [reason]"
}
```

### 📱 Hướng dẫn xử lý trên Flutter Rescuer App:
1. **Lắng nghe event**: Đăng ký listen `MissionAbortedByOperator` trong `RescuerHub`.
2. **Cập nhật UI**: 
   - Hiển thị Dialog thông báo lý do bị hủy.
   - Dừng luồng dẫn đường (nếu đang EnRoute).
   - Đưa Rescuer quay về màn hình Trang chủ (Home) hoặc danh sách yêu cầu.
3. **Local State**: Xóa thông tin mission đang lưu cục bộ.

---

## 4) SignalR Event cho Frontend (Operator Dashboard)

Hub liên quan: `OperatorHub`

### 🟢 Event: `RescuerAborted`

Event này dùng chung cho cả trường hợp Rescuer tự Abort và Operator ép Abort.

- **Payload**:
```json
{
  "incidentId": "guid",
  "rescuerId": "guid",
  "operatorId": "guid-của-người-abort (nếu có)",
  "reason": "Lý do"
}
```

### 💻 Hướng dẫn xử lý trên FE Dashboard:
1. **Cập nhật bản đồ**: Chuyển icon vụ việc về màu/trạng thái `Verified` (Chờ điều phối).
2. **Cập nhật danh sách**: Đưa vụ việc vào lại tab "Chờ điều phối".
3. **Thông báo**: Hiển thị Toast thông báo nhiệm vụ đã được giải phóng.

---

## 5) Push Notification (FCM)

Ngoài SignalR, backend cũng gửi Push Notification để đảm bảo các bên nhận được tin nhắn kể cả khi app đang chạy ngầm.

### 🔔 Cho Cứu hộ viên (Rescuer)
- **Type**: `SNAKE_RESCUE_MISSION_ABORTED_BY_OPERATOR`
- **Title**: "Nhiệm vụ đã bị hủy bởi điều phối viên"
- **Body**: "Nhiệm vụ của bạn đã bị điều phối viên hủy. Lý do: [reason]"

### 🔔 Cho Thành viên (Member)
- **Type**: `SNAKE_RESCUE_MISSION_ABORTED`
- **Title**: "Thay đổi cứu hộ viên"
- **Body**: "Điều phối viên đã thay đổi cứu hộ viên cho yêu cầu của bạn để hỗ trợ nhanh hơn."

---

## 6) Checklist Kiểm thử

1. **Phân quyền**: User thường không thể gọi endpoint này.
2. **Trạng thái**: Chỉ có thể Abort khi Mission là `Preparing` hoặc `EnRoute`. Nếu đã `Arrived` sẽ báo lỗi 400.
3. **Sở hữu**: Chỉ Operator đang trực tiếp xử lý ca đó (HandlingOperator) mới có quyền Abort ca đó (trừ Admin).
4. **Data Integrity**: Kiểm tra Rescuer cũ đã được rảnh tay (`IsAvailable = true`) và Incident đã mất `AssignedRescuerId`.
