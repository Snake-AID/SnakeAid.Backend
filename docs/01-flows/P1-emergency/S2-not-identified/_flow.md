
# Mapping Flow P1-S2: Cứu hộ - Không nhận diện được rắn

> **Flow ID:** P1 | **SubFlow:** S2 - Không nhận diện được rắn | **Role:** Member

Dưới đây là các bước khi Member không nhận diện được loài rắn:

## 1. Chọn rắn theo vị trí
**Screen:** Chọn rắn theo vị trí  
**Action:** Member chọn loài rắn dựa trên vùng địa lý  
**Backend Process:**
- Lấy danh sách loài rắn phổ biến theo location
- Lọc theo `SnakeSpecies.GeographicDistribution`  
**Endpoint:** `GET /api/snakes?location={lat,lng}`  
**Note:** Filter by distribution polygon.

## 2. Xác nhận loài rắn
**Screen:** Xác nhận loài rắn  
**Action:** Member chọn loài rắn từ danh sách  
**Backend Process:**
- Lấy chi tiết loài rắn đã chọn
- Hiển thị hình ảnh và đặc điểm  
**Endpoint:** `GET /api/snakes/{slug}`

## 3. Nhận dạng qua câu hỏi
**Screen:** Nhận dạng qua câu hỏi  
**Action:** Member trả lời câu hỏi để thu hẹp loài rắn  
**Backend Process:**
- Sử dụng decision tree / questionnaire
- Lọc dần danh sách loài khả thi  
**Endpoint:** `GET /api/snakes/identify` (cần mở rộng API)  
**Note:** Input: answers to questions, Output: filtered species list.

## 4. Chọn rắn theo vị trí (2)
**Screen:** Chọn rắn theo vị trí  
**Action:** Member chọn lại loài rắn sau khi xem gợi ý  
**Backend Process:**
- Tương tự bước 1-2  
**Endpoint:** `GET /api/snakes/{slug}`

## 5. Hướng dẫn sơ cứu chung
**Screen:** Hướng dẫn sơ cứu chung  
**Action:** Hiển thị hướng dẫn sơ cứu mặc định (không có loài cụ thể)  
**Backend Process:**
- Lấy general first aid guidelines
- Hiển thị các bước sơ cứu phổ quát  
**Endpoint:** `GET /api/first-aid/general`
