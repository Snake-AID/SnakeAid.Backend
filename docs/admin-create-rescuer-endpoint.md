# Admin Create Rescuer Endpoint

## Mục đích

Endpoint này cho phép admin tạo tài khoản `Rescuer` trực tiếp cho hệ thống.

## Base route

`/api/admin/users`

## Endpoint

- Method: `POST`
- Path: `/api/admin/users/create-rescuer`
- Authorization: JWT Bearer token
- Role: `Admin` (có sẵn vì controller dùng `[Authorize(Roles = "Admin")]`)

## Request body

Content-Type: `application/json`

```json
{
  "email": "rescuer@example.com",
  "password": "StrongP@ssw0rd",
  "fullName": "Nguyễn Văn A",
  "phoneNumber": "+840123456789",
  "type": "Emergency"
}
```

### Request model: `AdminCreateRescuerRequest`

- `email` (string, required)
  - phải tồn tại
  - định dạng email hợp lệ
- `password` (string, required)
  - phải tồn tại
  - tối thiểu 8 ký tự
- `fullName` (string, required)
  - phải tồn tại
  - tối đa 200 ký tự
- `phoneNumber` (string, optional)
  - nếu cung cấp thì phải hợp lệ theo attribute `[Phone]`
- `type` (enum `RescuerType`, required)
  - giá trị hợp lệ: `Emergency`, `Catching`, `Both`

> Lưu ý: `JsonStringEnumConverter` được bật toàn cục, nên frontend nên gửi enum là chuỗi.

## Validation

- Request body bị thiếu hoặc không đúng schema sẽ bị trả lỗi validate bởi `[ValidateModel]`.
- Các lỗi validation thường trả về `422 Unprocessable Entity`.
- Nếu email đã tồn tại, API trả lỗi `400 Bad Request` với thông báo `Email is already in use.`

## Response wrapper

Tất cả response thành công trả theo `ApiResponse<T>`:

```json
{
  "status_code": 200,
  "message": "Rescuer account created successfully.",
  "is_success": true,
  "data": { ... },
  "error": null
}
```

## Success response model: `AdminUserDetailResponse`

`data` trả về thông tin chi tiết user và profile tương ứng.

### Fields chung

- `id` (Guid)
- `userName` (string)
- `fullName` (string)
- `email` (string|null)
- `phoneNumber` (string|null)
- `role` (string enum) — với tài khoản tạo ra sẽ là `Rescuer`
- `createdAt` (datetime)
- `updatedAt` (datetime)
- `isActive` (bool)
- `reputationPoints` (int)
- `reputationStatus` (string enum)
- `suspendedUntil` (datetime|null)
- `suspensionReason` (string|null)
- `avatarUrl` (string|null)
- `memberProfile` (object|null)
- `expertProfile` (object|null)
- `rescuerProfile` (object|null)

### `rescuerProfile` object

- `isOnline` (bool)
- `isAvailable` (bool)
- `type` (string enum): `Emergency`, `Catching`, `Both`
- `rating` (decimal)
- `ratingCount` (decimal)
- `totalMissions` (int)
- `completedMissions` (int)
- `lastLocationUpdate` (datetime|null)

## Example success response

```json
{
  "status_code": 200,
  "message": "Rescuer account created successfully.",
  "is_success": true,
  "data": {
    "id": "e7f4a6cd-0f8f-4f47-a3a5-9b8f1d6a102c",
    "userName": "rescuer@example.com",
    "fullName": "Nguyễn Văn A",
    "email": "rescuer@example.com",
    "phoneNumber": "+840123456789",
    "role": "Rescuer",
    "createdAt": "2026-04-23T08:30:00Z",
    "updatedAt": "2026-04-23T08:30:00Z",
    "isActive": true,
    "reputationPoints": 100,
    "reputationStatus": "Good",
    "suspendedUntil": null,
    "suspensionReason": null,
    "avatarUrl": null,
    "memberProfile": null,
    "expertProfile": null,
    "rescuerProfile": {
      "isOnline": false,
      "isAvailable": false,
      "type": "Emergency",
      "rating": 0,
      "ratingCount": 0,
      "totalMissions": 0,
      "completedMissions": 0,
      "lastLocationUpdate": null
    }
  },
  "error": null
}
```

## Example validation error response

```json
{
  "status_code": 422,
  "message": "Validation failed",
  "is_success": false,
  "data": null,
  "error": {
    "errorCode": "VALIDATION_ERROR",
    "timestamp": "2026-04-23T08:31:00Z",
    "validationErrors": {
      "email": [
        "Invalid email format"
      ],
      "password": [
        "Password must be at least 8 characters"
      ]
    }
  }
}
```

## Notes for frontend

- Dùng endpoint này khi admin cần tạo rescuer trực tiếp.
- Không dùng route `api/auth/register` cho chức năng admin tạo rescuer.
- `type` nên gửi đúng một trong các giá trị chuỗi: `Emergency`, `Catching`, `Both`.
- `phoneNumber` có thể để trống nếu admin không nhập số.
- Nếu cần hiển thị role, FE nên dùng string trả về `Rescuer` thay vì số.
