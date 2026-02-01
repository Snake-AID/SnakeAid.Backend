# Snake Detection - Introduction

## 🎯 Overview

`POST /api/detection/detect/{reportMediaId}` là endpoint chính cho chức năng nhận diện loài rắn, sử dụng mô hình AI kết hợp với cơ sở dữ liệu loài (SnakeLibs) để cung cấp thông tin chi tiết và hướng dẫn sơ cứu.

Hệ thống hoạt động theo quy trình **Two-Step Flow**:
1.  **Media Upload:** Upload ảnh rắn để tạo `ReportMedia`.
2.  **Detection:** Gọi AI để phân tích ảnh đó và map kết quả với `SnakeSpecies`.

## 🔄 User Flow (Phase 2)

-   **Flow:** P1 - Emergency Rescue & P2 - Education
-   **Screen:** Camera/Upload Screen -> Detection Result Screen
-   **Actors:** Member, Guest, System Admin

## 🏗 Architecture

### 1. Separation of Concerns
Chúng tôi tách biệt việc upload và detection để tối ưu hóa hiệu năng và quản lý dữ liệu:

| Endpoint | Mục đích |
|----------|----------|
| `POST /api/media/report` | Upload ảnh lên Cloudinary và lưu metadata vào DB (`ReportMedia`) |
| `POST /api/detection/detect/{reportMediaId}` | Trigger AI analysis trên ảnh đã upload |

### 2. SnakeLibs Integration
Không chỉ trả về kết quả raw từ YOLO (VD: "cobra"), hệ thống còn map kết quả này với entity `SnakeSpecies` trong database để trả về:
-   **Common Name & Scientific Name** (Tiếng Việt/Anh)
-   **Venom Status & Risk Level**
-   **First Aid Guidelines** (Hướng dẫn sơ cứu cụ thể cho từng loại nọc độc)

## 💡 Key Features (V3)
-   **Strict JSON Structure:** Response tuân thủ cấu trúc JSON strict với `ai_metadata` và `results`.
-   **Fallback Logic:** Tự động tìm hướng dẫn sơ cứu từ Venom Type nếu không có override cụ thể cho loài.
-   **Persistence:** Mọi kết quả nhận diện đều được lưu lại (`SnakeAIRecognitionResult`) để audit và training sau này.

## 📚 References
-   [Implementation Plan](./snake-detection.plan.md)
-   [Source Code Status](./snake-detection.sourcecode.md)
-   [Usage Guide](./snake-detection.usageguide.md)
