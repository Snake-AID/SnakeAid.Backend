
# Mapping Flow P2-S1: Bắt rắn - Bắt 1 con rắn

> **Flow ID:** P2 | **SubFlow:** S1 - Bắt 1 con rắn | **Role:** Member

Dưới đây là các bước khi Member yêu cầu bắt 1 con rắn (non-emergency):

## 1. Chọn số lượng rắn
**Screen:** Chọn số lượng rắn  
**Action:** Member chọn "1 con rắn"  
**Backend Process:**
- Khởi tạo `CatchingRequest` với quantity = 1  
**Endpoint:** `POST /api/catching/requests`  
**Note:** Request body chứa quantity và initial location.

## 2. Báo cáo 1 con rắn
**Screen:** Báo cáo 1 con rắn  
**Action:** Member nhập thông tin về con rắn  
**Backend Process:**
- Cập nhật chi tiết: vị trí cụ thể, kích thước ước tính, mô tả  
**Endpoint:** `PUT /api/catching/requests/{id}`

## 3. Chọn loài rắn
**Screen:** Chọn loài rắn  
**Action:** Member chọn hoặc chụp ảnh để xác định loài  
**Backend Process:**
- Upload ảnh nếu có
- Chọn từ danh sách hoặc AI nhận diện  
**Endpoints:**
- `POST /api/media/upload-image`
- `POST /api/aivision/detect`
- `GET /api/snakes?location={lat,lng}`

## 4. Kết quả rắn AI
**Screen:** Kết quả rắn AI  
**Action:** System hiển thị kết quả nhận diện  
**Backend Process:**
- Hiển thị loài, độc tính, mức độ nguy hiểm
- Lưu vào `CatchingRequest.DeclaredSpeciesId`  
**Endpoint:** `GET /api/aivision/{id}`

## 5. Xác nhận yêu cầu cứu hộ
**Screen:** Xác nhận yêu cầu cứu hộ  
**Action:** Member xác nhận và submit yêu cầu bắt rắn  
**Backend Process:**
- Finalize request
- Tính phí dự kiến dựa trên loài và kích thước  
**Endpoint:** `PUT /api/catching/requests/{id}/confirm`  
**Note:** Cần mở rộng API - thêm confirm endpoint.

## 6. Đang tìm đội cứu hộ
**Screen:** Đang tìm đội cứu hộ  
**Action:** System tìm kiếm Rescuer gần nhất  
**Backend Process:**
- Query `Rescuer` trong bán kính 5km → 10km → 15km
- Filter by availability và experience
- Send notifications đến top candidates  
**Endpoint:** `GET /api/catching/requests/nearby` (Rescuer side)  
**Note:** Member chờ, system tự động matching.

## 7. Theo dõi cứu hộ trực tiếp
**Screen:** Theo dõi cứu hộ trực tiếp  
**Action:** Member theo dõi vị trí Rescuer trên map  
**Backend Process:**
- Stream `LocationEvent` của Rescuer
- Cập nhật ETA  
**Endpoints:**
- `GET /api/catching/missions/{id}/tracking`
- `GET /api/missions/{id}/tracking` (shared endpoint)

## 8. Đội cứu hộ đã đến
**Screen:** Đội cứu hộ đã đến  
**Action:** Rescuer xác nhận đã đến  
**Backend Process:**
- Cập nhật `CatchingMission.Status` → `Arrived`  
**Endpoint:** `PUT /api/catching/missions/{id}/status`

## 9. Thanh toán và đánh giá
**Screen:** Thanh toán tiền còn lại và đánh giá  
**Action:** Member thanh toán và đánh giá dịch vụ  
**Backend Process:**
- Tính phí cuối cùng (có thể điều chỉnh nếu loài khác)
- Xử lý thanh toán qua wallet
- Nhận rating & review  
**Endpoints:**
- `POST /api/wallet/deposit`
- `POST /api/catching/missions/{id}/review` (cần mở rộng)

## 10. Thanh toán thành công
**Screen:** Thanh toán thành công  
**Action:** System xác nhận thanh toán hoàn tất  
**Backend Process:**
- Cập nhật `CatchingMission.Status` → `Completed`
- Phân chia thanh toán (85% Rescuer, 10% Platform, 5% Insurance)
- Lưu transaction history  
**Endpoints:**
- `PUT /api/catching/missions/{id}/complete`
- `GET /api/wallet/transactions`
