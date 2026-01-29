
# Mapping Flow P2-S4: Bắt rắn - Các màn hình chung

> **Flow ID:** P2 | **SubFlow:** S4 - Màn hình chung | **Role:** Member/System

Dưới đây là các màn hình dùng chung trong Flow P2:

## 1. Xác nhận cảnh báo cộng đồng
**Screen:** Xác nhận cảnh báo cộng đồng  
**Action:** System hỏi Member có muốn cảnh báo cộng đồng không  
**Backend Process:**
- User chọn Yes → Tạo `CommunityReport`
- Gửi notification đến users trong khu vực
- Cập nhật heatmap data  
**Endpoints:**
- `POST /api/community/reports`
- `GET /api/community/reports?lat={lat}&lng={lng}` (nearby sightings)  
**Note:** Miễn phí, giúp cảnh báo cộng đồng về rắn trong khu vực.

## 2. Không tìm thấy đội cứu hộ
**Screen:** Không tìm thấy đội cứu hộ  
**Action:** System thông báo không có Rescuer khả dụng  
**Backend Process:**
- Sau 5 phút tìm kiếm không có kết quả
- Đề xuất các lựa chọn thay thế  
**Suggestions:**
- Gọi trung tâm kiểm soát động vật địa phương
- Gọi 115 (nếu khẩn cấp)
- Đăng lại request sau  
**Endpoint:** `PUT /api/catching/requests/{id}/cancel`  
**Note:** Log reason = "no_rescuer_available"

## 3. Hủy cứu hộ
**Screen:** Hủy cứu hộ  
**Action:** Member hủy yêu cầu bắt rắn  
**Backend Process:**
- Cập nhật `CatchingRequest.Status` → `Cancelled`
- Nếu đã có Rescuer accept → thông báo và hoàn phí (nếu có)  
**Endpoint:** `PUT /api/catching/requests/{id}/cancel`  
**Note:** Có thể phạt phí nếu hủy sau khi Rescuer đã di chuyển.
