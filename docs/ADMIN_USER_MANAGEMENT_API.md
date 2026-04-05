# Admin User Management API

## Overview

Tài liệu này mô tả chi tiết request model, response model, validation và ví dụ payload cho các endpoint trong Admin User Management.

Base route: `/api/admin/users`

Authorization: bắt buộc JWT và role `Admin`.

## Response Envelope Chuẩn

Tất cả endpoint trả theo format `ApiResponse<T>`:

```json
{
  "status_code": 200,
  "message": "...",
  "is_success": true,
  "data": {},
  "error": null
}
```

Khi lỗi validate:

```json
{
  "status_code": 422,
  "message": "Validation failed",
  "is_success": false,
  "data": null,
  "error": {
    "errorCode": "VALIDATION_ERROR",
    "timestamp": "2026-04-05T10:00:00Z",
    "validationErrors": {
      "Reason": [
        "Reason is required"
      ]
    }
  }
}
```

## Endpoint 1: Lấy Danh Sách User

- Method: `GET`
- Path: `/api/admin/users/list`
- Controller method: `GetAdminUserList`

### Query Parameters

- `role` (string, optional): `User | Admin | Expert | Rescuer | Operator`
- `isActive` (bool, optional): `true | false`
- `searchTerm` (string, optional): tìm theo `UserName`, `FullName`, `Email` (contains, case-insensitive)
- `page` (int, optional, default `1`)
- `pageSize` (int, optional, default `50`)

### Validation/Behavior

- Không có attribute validate riêng cho query params ở controller.
- `role` không hợp lệ sẽ không throw lỗi, nhưng kết quả thường rỗng do không map được enum role.
- `page` và `pageSize` hiện không có rule chặn ở controller/service.

### Success Response Model

`ApiResponse<PagedData<AdminUserSummaryResponse>>`

`AdminUserSummaryResponse`:

- `id` (Guid)
- `userName` (string)
- `fullName` (string)
- `email` (string | null)
- `phoneNumber` (string | null)
- `role` (AccountRole enum number)
- `createdAt` (datetime)
- `updatedAt` (datetime)
- `isActive` (bool)
- `reputationPoints` (int)
- `reputationStatus` (ReputationStatus enum number)
- `suspendedUntil` (datetime | null)
- `suspensionReason` (string | null)
- `avatarUrl` (string | null)

### Example Response

```json
{
  "status_code": 200,
  "message": "User list retrieved.",
  "is_success": true,
  "data": {
    "items": [
      {
        "id": "5b85b3fd-9057-4f59-8f86-2d8a3d8de91e",
        "userName": "expert01",
        "fullName": "Nguyen Van A",
        "email": "expert01@mail.com",
        "phoneNumber": "0900000001",
        "role": 2,
        "createdAt": "2026-04-01T08:20:00Z",
        "updatedAt": "2026-04-03T09:15:00Z",
        "isActive": true,
        "reputationPoints": 95,
        "reputationStatus": 0,
        "suspendedUntil": null,
        "suspensionReason": null,
        "avatarUrl": "https://cdn.example.com/avatar1.png"
      }
    ],
    "meta": {
      "total_pages": 1,
      "total_items": 1,
      "current_page": 1,
      "page_size": 50
    }
  },
  "error": null
}
```

## Endpoint 2: Lấy Chi Tiết User

- Method: `GET`
- Path: `/api/admin/users/{userId}`
- Controller method: `GetAdminUserDetail`

### Path Parameters

- `userId` (Guid, required)

### Validation/Behavior

- `userId` phải đúng định dạng Guid.
- Nếu không tìm thấy user, service throw NotFoundException (response phụ thuộc global exception handler của API).
- Detail response có thêm dữ liệu profile theo role:
- `memberProfile`
- `expertProfile`
- `rescuerProfile`

Mỗi field profile có thể `null` nếu user không có profile tương ứng.

### Success Response Model

`ApiResponse<AdminUserDetailResponse>`

`AdminUserDetailResponse`:

- `id` (Guid)
- `userName` (string)
- `fullName` (string)
- `email` (string | null)
- `phoneNumber` (string | null)
- `role` (AccountRole enum number)
- `createdAt` (datetime)
- `updatedAt` (datetime)
- `isActive` (bool)
- `reputationPoints` (int)
- `reputationStatus` (ReputationStatus enum number)
- `suspendedUntil` (datetime | null)
- `suspensionReason` (string | null)
- `avatarUrl` (string | null)
- `memberProfile` (AdminMemberProfileResponse | null)
- `expertProfile` (AdminExpertProfileResponse | null)
- `rescuerProfile` (AdminRescuerProfileResponse | null)

`AdminMemberProfileResponse`:

- `rating` (float)
- `ratingCount` (int)
- `hasUnderlyingDisease` (bool)
- `emergencyContacts` (string[])

`AdminExpertProfileResponse`:

- `biography` (string)
- `isOnline` (bool)
- `consultationFee` (decimal)
- `emergencyConsultationFee` (decimal | null)
- `rating` (decimal)
- `ratingCount` (int)

`AdminRescuerProfileResponse`:

- `isOnline` (bool)
- `isAvailable` (bool)
- `type` (RescuerType enum number)
- `rating` (decimal)
- `ratingCount` (int)
- `totalMissions` (int)
- `completedMissions` (int)
- `lastLocationUpdate` (datetime | null)

### Example Response

```json
{
  "status_code": 200,
  "message": "User detail retrieved.",
  "is_success": true,
  "data": {
    "id": "5b85b3fd-9057-4f59-8f86-2d8a3d8de91e",
    "userName": "rescuer01",
    "fullName": "Tran Thi B",
    "email": "rescuer01@mail.com",
    "phoneNumber": "0900000002",
    "role": 3,
    "createdAt": "2026-04-01T08:20:00Z",
    "updatedAt": "2026-04-05T08:20:00Z",
    "isActive": true,
    "reputationPoints": 88,
    "reputationStatus": 1,
    "suspendedUntil": null,
    "suspensionReason": null,
    "avatarUrl": null,
    "memberProfile": null,
    "expertProfile": null,
    "rescuerProfile": {
      "isOnline": true,
      "isAvailable": true,
      "type": 2,
      "rating": 4.8,
      "ratingCount": 120,
      "totalMissions": 400,
      "completedMissions": 380,
      "lastLocationUpdate": "2026-04-05T07:59:00Z"
    }
  },
  "error": null
}
```

## Endpoint 3: Ban User

- Method: `POST`
- Path: `/api/admin/users/{userId}/ban`
- Controller method: `BanUser`
- Validation attribute: `[ValidateModel]`

### Path Parameters

- `userId` (Guid, required)

### Request Model

`BanUserRequest`

- `reason` (string, required, max length 500)

### Request Example

```json
{
  "reason": "Vi phạm chính sách cộng đồng"
}
```

### Validation Rules

- `reason` bắt buộc.
- `reason` tối đa 500 ký tự.
- Khi sai validate, API trả `422 Unprocessable Entity` theo format `ApiResponse<object>`.

### Success Response Model

`ApiResponse<AdminUserDetailResponse>`

Behavior hiện tại khi ban:

- Set `isActive = false`
- Set `suspendedUntil = null`
- Set `suspensionReason = reason`

### Example Success Response

```json
{
  "status_code": 200,
  "message": "User has been banned.",
  "is_success": true,
  "data": {
    "id": "5b85b3fd-9057-4f59-8f86-2d8a3d8de91e",
    "userName": "member01",
    "fullName": "Le Van C",
    "email": "member01@mail.com",
    "phoneNumber": null,
    "role": 0,
    "createdAt": "2026-04-01T08:20:00Z",
    "updatedAt": "2026-04-05T10:00:00Z",
    "isActive": false,
    "reputationPoints": 70,
    "reputationStatus": 1,
    "suspendedUntil": null,
    "suspensionReason": "Vi phạm chính sách cộng đồng",
    "avatarUrl": null,
    "memberProfile": null,
    "expertProfile": null,
    "rescuerProfile": null
  },
  "error": null
}
```

## Endpoint 4: Unban User

- Method: `POST`
- Path: `/api/admin/users/{userId}/unban`
- Controller method: `UnbanUser`

### Path Parameters

- `userId` (Guid, required)

### Request Body

- Không yêu cầu body.

### Success Response Model

`ApiResponse<AdminUserDetailResponse>`

Behavior hiện tại khi unban:

- Set `isActive = true`
- Set `suspendedUntil = null`
- Set `suspensionReason = null`

### Example Success Response

```json
{
  "status_code": 200,
  "message": "User has been unbanned.",
  "is_success": true,
  "data": {
    "id": "5b85b3fd-9057-4f59-8f86-2d8a3d8de91e",
    "userName": "member01",
    "fullName": "Le Van C",
    "email": "member01@mail.com",
    "phoneNumber": null,
    "role": 0,
    "createdAt": "2026-04-01T08:20:00Z",
    "updatedAt": "2026-04-05T10:05:00Z",
    "isActive": true,
    "reputationPoints": 70,
    "reputationStatus": 1,
    "suspendedUntil": null,
    "suspensionReason": null,
    "avatarUrl": null,
    "memberProfile": null,
    "expertProfile": null,
    "rescuerProfile": null
  },
  "error": null
}
```

## Enum Mapping

Dùng numeric enum trong JSON response:

- AccountRole
- `0`: User
- `1`: Admin
- `2`: Expert
- `3`: Rescuer
- `4`: Operator

- ReputationStatus
- `0`: Excellent
- `1`: Good
- `2`: Average
- `3`: Poor
- `4`: Suspended

- RescuerType
- `0`: Emergency
- `1`: Catching
- `2`: Both

## Ghi Chú Triển Khai Frontend Admin

- Nên hiển thị profile block theo role và fallback `null` an toàn.
- Với endpoint list, cần chủ động validate input ở frontend cho `page` và `pageSize`.
- Với endpoint ban, luôn hiển thị lỗi validate từ `error.validationErrors` nếu có.
- Với endpoint detail, không assume cả 3 profile đều có dữ liệu.
