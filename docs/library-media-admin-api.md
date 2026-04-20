# API quản lý Library Media

Tài liệu này mô tả API quản lý thư viện media cho trang admin và các model request/response frontend nên dùng cho:

- quản lý các bản ghi media trong trang admin
- hiển thị modal chọn ảnh/media để trả về `mediaId` khi cập nhật loài rắn hoặc các thực thể khác

---

## Mục tiêu

Xây dựng trải nghiệm thư viện media giống như thư viện ảnh trên điện thoại:

- danh sách media upload phân trang
- lọc theo loại, liên kết, trạng thái active/public
- xem trước thumbnail hoặc link file
- chọn 1 media và trả về `mediaId` cho caller
- dùng `mediaId` đó khi cập nhật ảnh cho snake species hoặc thực thể khác có tham chiếu media

---

## Controller

`SnakeAid.Api.Controllers.LibraryMediaController`

Route gốc: `api/library-media`

Các endpoint:

### 1. Tạo media mới

- Phương thức: `POST`
- Đường dẫn: `/api/library-media`
- Content type: `multipart/form-data`
- Mục đích: upload file lên Cloudinary và tạo bản ghi library media

Model request: `CreateLibraryMediaRequest`

- `IFormFile File` - file bắt buộc
- `MediaType MediaType` - kiểu media bắt buộc (image/video/document/...)
- `int? SnakeSpeciesId` - tuỳ chọn liên kết media với một loài rắn

Model response: `LibraryMediaResponse`

- `Guid Id`
- `string MediaUrl`
- `MediaType MediaType`
- `string? FileName`
- `long? FileSizeBytes`
- `string? ContentType`
- `bool IsActive`
- `bool IsPublic`
- `Guid? UploadedById`
- `DateTime? UploadedAt`
- `int? SnakeSpeciesId`

Ví dụ request (multipart/form-data):

- `File` = file nhị phân
- `MediaType` = `Image`
- `SnakeSpeciesId` = `123`

Ví dụ response body:

```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "mediaUrl": "https://cdn.example.com/images/snake-1.jpg",
  "mediaType": "Image",
  "fileName": "snake-1.jpg",
  "fileSizeBytes": 112345,
  "contentType": "image/jpeg",
  "isActive": true,
  "isPublic": true,
  "uploadedById": "5a3e8ebf-1d2b-4f83-b21f-b5dd6be0c7a4",
  "uploadedAt": "2026-04-20T12:34:56Z",
  "snakeSpeciesId": 123
}
```

### 2. Lấy media theo ID

- Phương thức: `GET`
- Đường dẫn: `/api/library-media/{id:guid}`
- Mục đích: lấy một bản ghi media theo GUID

Model response: `LibraryMediaResponse`

Ví dụ response body:

```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "mediaUrl": "https://cdn.example.com/images/snake-1.jpg",
  "mediaType": "Image",
  "fileName": "snake-1.jpg",
  "fileSizeBytes": 112345,
  "contentType": "image/jpeg",
  "isActive": true,
  "isPublic": true,
  "uploadedById": "5a3e8ebf-1d2b-4f83-b21f-b5dd6be0c7a4",
  "uploadedAt": "2026-04-20T12:34:56Z",
  "snakeSpeciesId": 123
}
```

### 3. Lọc / phân trang media

- Phương thức: `GET`
- Đường dẫn: `/api/library-media`
- Mục đích: lấy danh sách media phân trang, phù hợp cho trang admin hoặc modal chọn media

Model query: `GetLibraryMediaRequest`

- kế thừa `PaginationRequest`
- `int? SnakeSpeciesId`
- `MediaType? MediaType`
- `bool? IsActive`
- `bool? IsPublic`
- `string? FileName`

```
public class PaginationRequest
    {
        /// <summary>
        /// Page number (starting from 1)
        /// </summary>
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Number of items per page
        /// </summary>
        [Range(1, 100)]
        public int PageSize { get; set; } = 10;
    }
```

Model response: `PagedData<LibraryMediaResponse>`

- chứa `Items`, `TotalCount`, `Page`, `PageSize`, ...

Ví dụ request:
`GET /api/library-media?page=1&pageSize=20&mediaType=Image&isActive=true`

Ví dụ response body:

```json
{
  "items": [
    {
      "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "mediaUrl": "https://cdn.example.com/images/snake-1.jpg",
      "mediaType": "Image",
      "fileName": "snake-1.jpg",
      "fileSizeBytes": 112345,
      "contentType": "image/jpeg",
      "isActive": true,
      "isPublic": true,
      "uploadedById": "5a3e8ebf-1d2b-4f83-b21f-b5dd6be0c7a4",
      "uploadedAt": "2026-04-20T12:34:56Z",
      "snakeSpeciesId": 123
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20
}
```

### 4. Cập nhật media

- Phương thức: `PUT`
- Đường dẫn: `/api/library-media/{id:guid}`
- Content type: `multipart/form-data`
- Mục đích: cập nhật metadata hoặc thay file của media hiện có

Model request: `UpdateLibraryMediaRequest`

- `IFormFile? File` - file mới tuỳ chọn
- `MediaType? MediaType` - kiểu mới tuỳ chọn
- `bool? IsActive` - bật/tắt media
- `bool? IsPublic` - công khai/riêng tư
- `int? SnakeSpeciesId` - liên kết tùy chọn với snake species

Model response: `LibraryMediaResponse`

Ví dụ request (multipart/form-data):

- `File` = file mới tuỳ chọn
- `MediaType` = `Video`
- `IsActive` = `false`
- `IsPublic` = `true`
- `SnakeSpeciesId` = `123`

Ví dụ response body:

```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "mediaUrl": "https://cdn.example.com/videos/updated.mp4",
  "mediaType": "Video",
  "fileName": "updated.mp4",
  "fileSizeBytes": 2423456,
  "contentType": "video/mp4",
  "isActive": false,
  "isPublic": true,
  "uploadedById": "5a3e8ebf-1d2b-4f83-b21f-b5dd6be0c7a4",
  "uploadedAt": "2026-04-20T12:34:56Z",
  "snakeSpeciesId": 123
}
```

### 5. Xoá media

- Phương thức: `DELETE`
- Đường dẫn: `/api/library-media/{id:guid}`
- Mục đích: xóa bản ghi media và xoá file trên Cloudinary

Response: chỉ trả thông báo thành công

Ví dụ response body:

```json
{
  "message": "Library media deleted successfully."
}
```

---

## Trang admin frontend

Trang admin nên hỗ trợ:

- xem gallery media phân trang
- lọc theo `MediaType`, `IsActive`, `IsPublic`, và `SnakeSpeciesId` nếu cần
- xem trước thumbnail hoặc icon file
- mở nhanh chi tiết item
- sửa bằng `PUT /api/library-media/{id}`
- xóa bằng `DELETE /api/library-media/{id}`

Gallery nên hoạt động như thư viện ảnh:

- hiển thị dưới dạng card hoặc hàng
- show preview từ `MediaUrl`
- cho phép click/chọn item trong modal picker
- trả về `mediaId` đã chọn cho caller

---

## Modal chọn media

Dùng lại API backend của danh sách media, nhưng hiển thị trong modal tập trung.

Hành vi picker:

- gọi `GET /api/library-media` với tham số phân trang
- show preview mỗi item bằng `MediaUrl`
- hỗ trợ tìm kiếm / lọc theo loại, tên file, trạng thái active/public
- cho phép chọn một item
- trả về `Guid` đã chọn dưới dạng `mediaId`

`mediaId` này sẽ dùng trong payload cập nhật của bất kỳ entity nào cần tham chiếu media.

Ví dụ:

- cập nhật ảnh cho loài rắn
- chọn image cho bước first-aid qua `MediaId`

---

## Thay đổi liên quan đến snake species request

Để hỗ trợ flow media picker, các model request snake species hiện tại chấp nhận payload first-aid step có `MediaId` ở lớp request.

### Integrate request model

`CreateSnakeSpeciesRequest` và `UpdateSnakeSpeciesRequest` giờ dùng:

- `SnakeSpeciesFirstAidOverrideRequest? FirstAidGuidelineOverride`

Thay vì dùng trực tiếp domain `FirstAidOverride`.

### Các model request-only mới

Định nghĩa tại: `SnakeAid.Core.Requests.SnakeSpecies\SnakeSpeciesFirstAidRequestModels.cs`

- `SnakeSpeciesFirstAidOverrideRequest`
  - `OverrideMode Mode`
  - `SnakeSpeciesFirstAidContentRequest Content`

- `SnakeSpeciesFirstAidContentRequest`
  - `List<SnakeSpeciesFirstAidStepRequest> Steps`
  - `List<SnakeSpeciesFirstAidStepRequest> Dos`
  - `List<SnakeSpeciesFirstAidStepRequest> Donts`
  - `List<string> Notes`

- `SnakeSpeciesFirstAidStepRequest`
  - `string Text`
  - `string? MediaUrl`
  - `Guid? MediaId`

### Hành vi

- nếu một bước có `MediaId`, backend sẽ resolve `MediaUrl` từ `LibraryMedia` trước khi lưu
- frontend chỉ cần gửi `MediaId` khi muốn dùng media đã upload
- backend giữ domain `FirstAidStep` nguyên vẹn và chỉ lưu `MediaUrl` đã resolve

---

## Gợi ý frontend

### Use case

1. Trang admin media:
   - liệt kê media
   - lọc, preview, sửa, xóa
   - quản lý tài sản app tập trung

2. Modal chọn media:
   - chọn `mediaId`
   - dùng ID đã chọn trong payload cập nhật loài rắn hoặc thực thể khác
   - modal hoạt động như gallery picker, không phải form upload

### Lưu ý quan trọng

- Ưu tiên dùng `mediaId` thay vì URL thủ công khi cập nhật tham chiếu ảnh.
- Modal nên trả đúng 1 `Guid` cho caller.
- Danh sách admin cần phân trang để mở rộng tốt.
- Preview dùng `MediaUrl` trả về.
- `MediaType` giúp UI xác định hiển thị (preview ảnh vs icon file).

---

## Ví dụ payload

### Tạo media

`POST /api/library-media`

```http
Content-Type: multipart/form-data
```

Form fields:

- `File` = file upload
- `MediaType` = `Image`
- `SnakeSpeciesId` = integer tuỳ chọn

### Lấy trang media

`GET /api/library-media?page=1&pageSize=20&mediaType=Image&isActive=true`

### Cập nhật metadata hoặc file media

`PUT /api/library-media/{id}`

```http
Content-Type: multipart/form-data
```

Form fields:

- `File` = file mới tuỳ chọn
- `MediaType` = kiểu mới tuỳ chọn
- `IsActive` = true/false tuỳ chọn
- `IsPublic` = true/false tuỳ chọn
- `SnakeSpeciesId` = integer tuỳ chọn

### Chọn mediaId để cập nhật snake species

Dùng modal picker để lấy `mediaId`.
Sau đó gửi `mediaId` vào payload cập nhật snake species.

Ví dụ step request:

```json
{
  "text": "Hiển thị vết thương cho bác sĩ.",
  "mediaId": "7c9e6679-7425-40de-944b-e07fc1f90ae7"
}
```
