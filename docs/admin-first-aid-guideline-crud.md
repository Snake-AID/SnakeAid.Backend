# Admin First Aid Guideline CRUD API

## Overview

API quản lý `FirstAidGuideline` cho admin bao gồm các thao tác CRUD: tạo, lấy, cập nhật và xóa.

Base route: `/api/first-aid-guidelines`

> Lưu ý: các endpoint `POST`, `PUT`, `DELETE` là admin-only theo tài liệu Swagger của controller.

---

## Endpoints

### 1. Create First Aid Guideline

- Method: `POST`
- URL: `/api/first-aid-guidelines`
- Auth: Admin
- Request body: `CreateFirstAidGuidelineRequest`
- Response: `ApiResponse<FirstAidGuidelineResponse>`

#### Request body

```json
{
  "name": "String",
  "content": {
    "steps": [
      {
        "text": "String",
        "mediaUrl": "String|null",
        "mediaId": "Guid|null"
      }
    ],
    "dos": [],
    "donts": [],
    "notes": ["String"]
  },
  "type": "GENERAL|VENOM_SPECIFIC",
  "summary": "String|null"
}
```

#### Media behavior

- `mediaId` là ID của `LibraryMedia`.
- Nếu `mediaId` được cung cấp, backend sẽ lấy `LibraryMedia.MediaUrl` tương ứng và dùng URL đó cho `content.steps`, `dos`, `donts`.
- Nếu không có `mediaId`, hệ thống vẫn chấp nhận `mediaUrl` do client gửi.
- `mediaId` hoạt động cho cả media từ thư viện (`LibraryMedia`) và media upload cùng cách xử lý.

#### Example using LibraryMedia

```json
{
  "name": "Hướng dẫn sơ cứu rắn độc",
  "content": {
    "steps": [
      {
        "text": "Rửa vết thương bằng nước sạch",
        "mediaUrl": null,
        "mediaId": "f47ac10b-58cc-4372-a567-0e02b2c3d479"
      }
    ],
    "dos": [],
    "donts": [],
    "notes": ["Dùng hình ảnh lưu trong thư viện media nếu có"]
  },
  "type": "VENOM_SPECIFIC",
  "summary": "Hướng dẫn sơ cứu cho rắn độc"
}
```

#### Validation

- `name`: required, max 255
- `content`: required
- `type`: required
- `summary`: optional, max 500

#### Example response

```json
{
  "status_code": 200,
  "message": "First aid guideline created successfully!",
  "is_success": true,
  "data": {
    "id": 123,
    "name": "String",
    "content": {
      "steps": [
        { "text": "String", "mediaUrl": "String" }
      ],
      "dos": [],
      "donts": [],
      "notes": ["String"]
    },
    "type": "GENERAL",
    "summary": "String",
    "createdAt": "2026-04-27T00:00:00Z",
    "updatedAt": "2026-04-27T00:00:00Z"
  }
}
```

### 5. Update First Aid Guideline

- Method: `PUT`
- URL: `/api/first-aid-guidelines/{id}`
- Auth: Admin
- Request body: `UpdateFirstAidGuidelineRequest`
- Response: `ApiResponse<FirstAidGuidelineResponse>`

#### Request body

```json
{
  "name": "String|null",
  "content": {
    "steps": [
      {
        "text": "String",
        "mediaUrl": "String|null",
        "mediaId": "Guid|null"
      }
    ],
    "dos": [],
    "donts": [],
    "notes": ["String"]
  },
  "type": "GENERAL|VENOM_SPECIFIC|null",
  "summary": "String|null"
}
```

#### Media behavior

- `mediaId` là ID của `LibraryMedia`.
- Nếu `mediaId` được cung cấp, backend sẽ lấy URL từ `LibraryMedia.MediaUrl` và dùng URL này cho trường `mediaUrl` trong nội dung trả về.
- Nếu `mediaId` không được cung cấp, backend sẽ giữ nguyên `mediaUrl` do client gửi.
- Cả media upload lẫn media thư viện đều được xử lý giống nhau khi sử dụng `mediaId`.

#### Example using LibraryMedia

```json
{
  "name": "Cập nhật hướng dẫn sơ cứu",
  "content": {
    "steps": [
      {
        "text": "Áp băng vết thương nhẹ nhàng",
        "mediaUrl": null,
        "mediaId": "f47ac10b-58cc-4372-a567-0e02b2c3d479"
      }
    ],
    "dos": [],
    "donts": [],
    "notes": ["Media được lấy từ LibraryMedia nếu mediaId hợp lệ"]
  },
  "type": "GENERAL",
  "summary": "Cập nhật hướng dẫn"
}
```

#### Example response

```json
{
  "status_code": 200,
  "message": "First aid guideline updated successfully!",
  "is_success": true,
  "data": {
    "id": 123,
    "name": "String",
    "content": {
      "steps": [],
      "dos": [],
      "donts": [],
      "notes": []
    },
    "type": "GENERAL",
    "summary": "String",
    "createdAt": "2026-04-27T00:00:00Z",
    "updatedAt": "2026-04-27T00:00:00Z"
  }
}
```

---

### 6. Delete First Aid Guideline

- Method: `DELETE`
- URL: `/api/first-aid-guidelines/{id}`
- Auth: Admin
- Response: `ApiResponse<bool>`

#### Example response

```json
{
  "status_code": 200,
  "message": "First aid guideline deleted successfully!",
  "is_success": true,
  "data": null
}
```

---

## Models

### CreateFirstAidGuidelineRequest

- `string Name` — required
- `FirstAidContentRequest Content` — required
- `GuidelineType Type` — required
- `string? Summary` — optional

### UpdateFirstAidGuidelineRequest

- `string? Name`
- `FirstAidContentRequest? Content`
- `GuidelineType? Type`
- `string? Summary`

### GetFirstAidGuidelineRequest

- `string? Name`
- `GuidelineType? Type`
- `int PageNumber` — default 1
- `int PageSize` — default 10, max 100

### FirstAidContentRequest

- `List<FirstAidStepRequest> Steps`
- `List<FirstAidStepRequest> Dos`
- `List<FirstAidStepRequest> Donts`
- `List<string> Notes`

### FirstAidStepRequest

- `string Text`
- `string? MediaUrl`
- `Guid? MediaId` — nếu điền, backend sẽ dùng `LibraryMedia.MediaUrl` tương ứng

### FirstAidGuidelineResponse

- `int Id`
- `string Name`
- `FirstAidContent Content`
- `GuidelineType Type`
- `string? Summary`
- `DateTime CreatedAt`
- `DateTime UpdatedAt`

### FirstAidContent

- `List<FirstAidStep> Steps`
- `List<FirstAidStep> Dos`
- `List<FirstAidStep> Donts`
- `List<string> Notes`

### FirstAidStep

- `string Text`
- `string MediaUrl`

### GuidelineType

- `GENERAL` = 0
- `VENOM_SPECIFIC` = 1

---

## Notes

- Endpoint `POST /api/first-aid-guidelines` tạo mới guideline.
- Endpoint `PUT /api/first-aid-guidelines/{id}` cập nhật fields được gửi.
- Endpoint `DELETE /api/first-aid-guidelines/{id}` xóa guideline.
- `Content` lưu dưới dạng JSON bao gồm các bước, điều nên làm, điều không nên làm và ghi chú.
