
# Mapping Flow P2-S3: Bắt rắn - Bắt cả ổ rắn

> **Flow ID:** P2 | **SubFlow:** S3 - Bắt cả ổ rắn | **Role:** Member

Dưới đây là các bước khi Member yêu cầu bắt cả ổ rắn:

## 1. Báo cáo ổ rắn / Nhiều con
**Screen:** Báo cáo ổ rắn / Nhiều con  
**Action:** Member báo cáo phát hiện ổ rắn  
**Backend Process:**
- Khởi tạo `CatchingRequest` với type = "nest"
- Đánh dấu priority cao hơn  
**Endpoint:** `POST /api/catching/requests`  
**Note:** Request body: `{ type: "nest", estimatedCount: 10+, location: {...} }`

## 2. Chọn loài rắn (ổ rắn / nhiều con)
**Screen:** Chọn loài rắn (ổ rắn / nhiều con)  
**Action:** Member xác định loài rắn trong ổ  
**Backend Process:**
- Upload ảnh ổ rắn
- AI nhận diện species (thường chỉ 1 loài trong 1 ổ)  
**Endpoints:**
- `POST /api/media/upload-image`
- `POST /api/aivision/detect`
- `GET /api/snakes/{slug}` (species details)

## 3. Kết quả phân tích ổ rắn
**Screen:** Kết quả phân tích ổ rắn / nhiều con  
**Action:** System hiển thị kết quả phân tích  
**Backend Process:**
- Hiển thị loài, ước tính số lượng
- Tính phí đặc biệt cho nest removal
- Recommend professional team  
**Endpoints:**
- `GET /api/aivision/{id}`
- `PUT /api/catching/requests/{id}`  
**Note:** Nest removal thường cần team chuyên nghiệp, phí cao hơn.

---

> **Tiếp theo:** Các bước 4-10 tương tự Flow P2-S1 (Xác nhận → Tìm Rescuer → Theo dõi → Thanh toán)  
> **Lưu ý:** Ổ rắn yêu cầu Rescuer có kinh nghiệm cao, có thể cần nhiều người.
