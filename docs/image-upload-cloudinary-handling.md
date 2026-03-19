# Image Upload Handling with Cloudinary

## Overview

SnakeAid Backend sử dụng Cloudinary làm dịch vụ lưu trữ và quản lý hình ảnh và file tải lên. Hệ thống hỗ trợ upload hình ảnh và file với các ràng buộc về định dạng, kích thước và tính năng tối ưu hóa tự động.

## Kiến trúc

### Components chính

#### 1. CloudinaryService (`SnakeAid.Service/Implements/CloudinaryService.cs`)
- **Chức năng**: Xử lý upload file lên Cloudinary
- **Interface**: `ICloudinaryService`
- **Dependencies**: CloudinaryDotNet SDK, AppSettings

#### 2. MediaController (`SnakeAid.Api/Controllers/MediaController.cs`)
- **Endpoints**:
  - `POST /api/media/upload-image`: Upload hình ảnh
  - `POST /api/media/upload-file`: Upload file
- **Validation**: Sử dụng custom validator cho file

#### 3. MediaService (`SnakeAid.Service/Implements/MediaService.cs`)
- Tích hợp với CloudinaryService để upload media cho báo cáo

#### 4. CloudinarySettings (`SnakeAid.Core/Settings/AppSettings.cs`)
```csharp
public class CloudinarySettings
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string BaseFolder { get; set; } = "snakeaid";
}
```

## Cấu hình

### appsettings.json
```json
{
  "Cloudinary": {
    "CloudName": "snakeaid",
    "ApiKey": "your_api_key",
    "ApiSecret": "your_api_secret",
    "BaseFolder": "snakeaid"
  }
}
```

### Dependency Injection
```csharp
// SnakeAid.Api/DI/DependencyInjection.cs
services.Configure<CloudinarySettings>(cloudinarySection);
services.AddScoped<ICloudinaryService, CloudinaryService>();
```

## Validation Rules

### Upload Hình ảnh
- **Định dạng cho phép**: .jpg, .jpeg, .png, .webp
- **Kích thước tối đa**: 10MB
- **Tối ưu hóa**: Auto quality, format, resize (max 1600px width)

### Upload File
- **Định dạng cho phép**: .jpg, .jpeg, .png, .webp, .pdf, .txt, .doc, .docx
- **Kích thước tối đa**: 100MB

## Folder Structure trên Cloudinary

```
{BaseFolder}/{Environment}/{Domain}/{UserId}/
```

**Ví dụ**:
```
snakeaid/development/report-media/550e8400-e29b-41d4-a716-446655440000/
```

## Tagging và Metadata

### Tags tự động
- `snakeaid`: Tag chung cho project
- `{Environment}`: development/staging/production
- `{Domain}`: report-media, files, etc.

### Public ID Format
```
{normalized_filename}-{timestamp}
```

**Ví dụ**: `snake-photo-20240318171430`

## Response Format

```csharp
public class CloudinaryUploadResult
{
    public string SecureUrl { get; set; }     // HTTPS URL của file
    public string PublicId { get; set; }      // ID công khai trên Cloudinary
    public string ResourceType { get; set; }  // image/video/raw/auto
    public string? Format { get; set; }       // Định dạng file
    public long? Bytes { get; set; }          // Kích thước file
    public int? Width { get; set; }           // Chiều rộng (chỉ ảnh)
    public int? Height { get; set; }          // Chiều cao (chỉ ảnh)
    public string Folder { get; set; }        // Thư mục lưu trữ
    public IReadOnlyCollection<string> Tags { get; set; } // Tags
}
```

## API Endpoints

### Upload Hình ảnh
```http
POST /api/media/upload-image
Content-Type: multipart/form-data

Form Data:
- file: [file]
- domain: string (optional, default: "uploads")
```

### Upload File
```http
POST /api/media/upload-file
Content-Type: multipart/form-data

Form Data:
- file: [file]
- domain: string (optional, default: "files")
```

## Domain Parameter

`domain` là tham số dùng để phân loại và tổ chức files trên Cloudinary theo mục đích sử dụng:

- **"uploads"**: Upload hình ảnh chung (default cho image)
- **"files"**: Upload file tổng quát (default cho file)
- **"report-media"**: Media cho báo cáo (sử dụng trong MediaService)

Domain này sẽ tạo thành subfolder trong cấu trúc Cloudinary:
```
{BaseFolder}/{Environment}/{Domain}/{UserId}/
```

**Ví dụ với domain "report-media":**
```
snakeaid/development/report-media/550e8400-e29b-41d4-a716-446655440000/
```

## Usage trong Code

### Direct Upload qua CloudinaryService
```csharp
var uploadResult = await _cloudinaryService.UploadImageAsync(
    file: request.File,
    user: User,
    domain: "report-media",
    cancellationToken: ct
);
```

### Tích hợp trong MediaService
```csharp
// Upload lên Cloudinary trước
var uploadResult = await _cloudinaryService.UploadImageAsync(request.File, user, "report-media", ct);

// Lưu metadata vào database
var reportMedia = new ReportMedia
{
    Id = Guid.NewGuid(),
    PublicUrl = uploadResult.SecureUrl,
    CloudinaryPublicId = uploadResult.PublicId,
    // ... other fields
};
```

## Error Handling

- **Validation Errors**: Kiểm tra định dạng và kích thước file
- **Cloudinary Errors**: Xử lý lỗi upload từ Cloudinary API
- **Authentication**: Kiểm tra User ID từ JWT token

## Security Considerations

1. **File Type Validation**: Chỉ cho phép các định dạng được liệt kê
2. **Size Limits**: Giới hạn kích thước file để tránh abuse
3. **User Isolation**: Files được tổ chức theo User ID
4. **Environment Separation**: Files được tách theo environment
5. **HTTPS URLs**: Luôn sử dụng SecureUrl cho HTTPS access

## Monitoring và Logging

- Logging upload success/failure
- Tracking file metadata (size, dimensions)
- Error logging với detailed information

## Performance Optimizations

1. **Auto Format**: Cloudinary tự động chọn format tối ưu (WebP, AVIF)
2. **Auto Quality**: Tự động điều chỉnh chất lượng
3. **Resize**: Giới hạn kích thước ảnh để tối ưu bandwidth
4. **CDN**: Files được phân phối qua Cloudinary CDN

## Dependencies

- **CloudinaryDotNet**: 1.27.4
- **Microsoft.AspNetCore.Http**: For IFormFile
- **Microsoft.Extensions.Options**: For configuration

## Testing

- Unit tests cho CloudinaryService
- Integration tests với mock Cloudinary API
- Validation tests cho file formats và sizes