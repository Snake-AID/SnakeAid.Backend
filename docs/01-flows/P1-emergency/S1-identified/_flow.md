
# Mapping Flow P1-S1: Cứu hộ - Nhận diện được rắn (Emergency Rescue)

> **Flow ID:** P1 | **SubFlow:** S1 - Nhận diện được rắn | **Role:** Member

Dưới đây là các bước trong Flow P1-S1, mỗi bước tương ứng với một endpoint và quy trình backend:

## 1. Trang chủ (Home - SOS)
**Screen:** Trang chủ  
**Action:** Member bấm SOS  
**Backend Process:**
- Tạo `SnakebiteIncident` với Location
- Tạo `RescuerRequest` để tìm Rescuer gần nhất  
**Endpoint:** `POST /api/incidents/sos`  
**Note:** Tạo incident với GPS location, bắt đầu flow cấp cứu.

## 2. Cảnh báo khẩn cấp
**Screen:** Cảnh báo khẩn cấp  
**Action:** Member thấy Rescuer trên map  
**Backend Process:**
- Stream vị trí Rescuer real-time
- Cập nhật `RescueMission.LocationEvent`  
**Endpoint:** `GET /api/missions/{id}/tracking`  
**Note:** SSE hoặc WebSocket stream vị trí rescuer.

## 3. Chụp ảnh rắn để AI nhận diện
**Screen:** Chụp ảnh rắn  
**Action:** Member chụp ảnh rắn  
**Backend Process:**
- Upload ảnh lên Cloudinary
- Gọi YOLO Model qua SnakeAI service
- Lưu kết quả vào `SnakeAIRecognitionResult`  
**Endpoints:**
- `POST /api/media/upload-image` (upload ảnh)
- `POST /api/detection/detect` (gọi AI)  
**Note:** Wraps SnakeAI `/detect` endpoint.

## 4. Kết quả nhận diện loài rắn
**Screen:** Kết quả nhận diện  
**Action:** System trả kết quả sau khi chụp  
**Backend Process:**
- Map YOLO class → `SnakeSpecies`
- Lấy `FirstAidGuidelineOverride` nếu có  
**Endpoints:**
- `GET /api/detection/{id}` (lấy kết quả)
- `GET /api/snakes/{slug}` (chi tiết loài)  
**Note:** Response nên trả kèm sơ bộ thông tin loài.

## 5. Hướng dẫn sơ cứu theo loài
**Screen:** Hướng dẫn sơ cứu  
**Action:** User bấm xem cách sơ cứu  
**Backend Process:**
- Lấy `FirstAidGuideline` theo `Species` → `VenomType`  
**Endpoint:** `GET /api/first-aid/species/{slug}`

## 6. Nhập triệu chứng và chụp vết cắn
**Screen:** Nhập triệu chứng  
**Action:** User nhập triệu chứng & chụp vết cắn  
**Backend Process:**
- Cập nhật `SymptomsReport` (jsonB) vào Incident
- Tính `SeverityLevel` (AI hoặc rule-based)  
**Endpoint:** `PUT /api/incidents/{id}/symptoms`

## 7. Hướng dẫn chi tiết theo mức độ
**Screen:** Hướng dẫn chi tiết  
**Action:** System hiện đánh giá mức độ nghiêm trọng  
**Backend Process:**
- Trả về `SeverityLevel` (Nhẹ/Trung bình/Nặng/Nguy kịch)
- Lấy hướng dẫn chi tiết tương ứng  
**Endpoint:** `GET /api/incidents/{id}`

## 8. Theo dõi SOS khẩn cấp
**Screen:** Theo dõi SOS khẩn cấp  
**Action:** Member theo dõi trạng thái SOS  
**Backend Process:**
- Lấy mission đang active
- Stream trạng thái real-time  
**Endpoints:**
- `GET /api/missions/active` (lấy mission hiện tại)
- `GET /api/missions/{id}/tracking` (stream vị trí)

## 9. Cứu hộ đã đến
**Screen:** Cứu hộ đã đến  
**Action:** Rescuer xác nhận đã đến  
**Backend Process:**
- Cập nhật `RescueMission.Status` → `Arrived`  
**Endpoint:** `PUT /api/missions/{id}/status`  
**Note:** Status enum: EnRoute, Arrived, Hospital, Completed.

## 10. Thanh toán và đánh giá
**Screen:** Thanh toán và đánh giá sau cấp cứu  
**Action:** Member thanh toán và đánh giá dịch vụ  
**Backend Process:**
- Khởi tạo giao dịch thanh toán
- Gửi rating & review  
**Endpoints:**
- `POST /api/wallet/deposit` (thanh toán)
- `POST /api/missions/{id}/review` (đánh giá - cần mở rộng API)

## 11. Thanh toán thành công
**Screen:** Thanh toán thành công  
**Action:** System xác nhận thanh toán  
**Backend Process:**
- Cập nhật `RescueMission.Status` → `Completed`
- Phân chia thanh toán (85% Rescuer, 10% Platform, 5% Insurance)  
**Endpoints:**
- `PUT /api/missions/{id}/status`
- `GET /api/wallet/transactions`

## 12. Tìm bệnh viện có huyết thanh
**Screen:** Bản đồ tìm kiếm bệnh viện có huyết thanh  
**Action:** Member tìm bệnh viện gần nhất  
**Backend Process:**
- Query danh sách cơ sở y tế có antivenom
- Lọc theo vị trí GPS và loài rắn  
**Endpoints:**
- `GET /api/antivenoms/nearest` (tìm antivenom gần nhất)
- `GET /api/facilities` (danh sách bệnh viện)