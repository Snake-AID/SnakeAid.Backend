
# Mapping Flow P2-S2: Bắt rắn - Bắt nhiều rắn (2-5 con)

> **Flow ID:** P2 | **SubFlow:** S2 - Bắt nhiều rắn | **Role:** Member

Dưới đây là các bước khi Member yêu cầu bắt 2-5 con rắn:

## 1. Báo cáo 2-5 con rắn
**Screen:** Báo cáo 2-5 con rắn  
**Action:** Member nhập số lượng và thông tin  
**Backend Process:**
- Khởi tạo `CatchingRequest` với quantity = 2-5
- Lấy GPS location  
**Endpoint:** `POST /api/catching/requests`  
**Note:** Request body: `{ quantity: 3, location: {...} }`

## 2. Chọn loài rắn (2-5 con)
**Screen:** Chọn loài rắn (2-5 con)  
**Action:** Member xác định loài cho từng con (hoặc mixed)  
**Backend Process:**
- Upload nhiều ảnh (mỗi con 1 ảnh)
- AI nhận diện từng ảnh
- Cho phép chọn "mixed species"  
**Endpoints:**
- `POST /api/media/upload-image` (multiple calls)
- `POST /api/aivision/detect` (per image)
- `GET /api/snakes?location={lat,lng}`

## 3. Kết quả nhận diện nhiều rắn AI
**Screen:** Kết quả nhận diện nhiều rắn AI  
**Action:** System hiển thị kết quả nhận diện cho từng con  
**Backend Process:**
- Aggregate results for all snakes
- Tính tổng độ nguy hiểm và phí dự kiến  
**Endpoints:**
- `GET /api/aivision/{id}` (per detection)
- `PUT /api/catching/requests/{id}` (update with species list)  
**Note:** Phí tăng theo số lượng và độ nguy hiểm.

---

> **Tiếp theo:** Các bước 4-10 tương tự Flow P2-S1 (Xác nhận → Tìm Rescuer → Theo dõi → Thanh toán)
