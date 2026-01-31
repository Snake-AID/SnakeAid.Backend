# Snake Detection - Giới thiệu

## Tổng quan

`POST /api/detection/detect` là endpoint cho phép nhận diện loài rắn từ ảnh sử dụng YOLO model. Endpoint này đóng vai trò **wrapper** cho SnakeAI FastAPI service (`/detect/url`).

## Vị trí trong User Flow

- **Flow:** P1 - Emergency Rescue
- **Screen:** 3. Chụp ảnh rắn để AI nhận diện
- **Role:** Member

## Lý do thiết kế tách riêng

Chọn phương án **tách riêng** upload và AI detection:

| Endpoint | Mục đích |
|----------|----------|
| `POST /api/media/upload-image` | Upload ảnh lên Cloudinary → trả về `imageUrl` |
| `POST /api/detection/detect` | Nhận `imageUrl` → gọi SnakeAI → trả về kết quả |

**Lợi ích:**
- ✅ Reusable: upload endpoint dùng cho nhiều mục đích khác
- ✅ Dễ debug và monitor từng bước
- ✅ Client có thể retry từng step
- ✅ Parallel uploads (upload nhiều ảnh cùng lúc)

## Use Cases

1. **Emergency SOS (P1-S1):** Member chụp ảnh rắn → AI nhận diện → hiển thị kết quả + first aid
2. **Snake Catching (P2):** Member báo cáo rắn → AI xác định loài → tính phí phù hợp
3. **Library Browse:** User upload ảnh → AI identify → navigates to species page

## Tham khảo

- [SnakeAI Model Endpoint API Reference](../../02-layers/ai/SankeAi.introduction.md)
- [Cloudinary Integration](../../02-layers/cloudinary/cloudinary.introduction.md)
