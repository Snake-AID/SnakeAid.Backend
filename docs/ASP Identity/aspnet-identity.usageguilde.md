# ASP.NET Identity Auth API Usage Guide

This document describes how frontend clients (Flutter and React) should authenticate against the SnakeAid API.

## Base URLs
- HTTP (dev): `http://localhost:5009`
- HTTPS (dev): `https://localhost:7026`

> [!NOTE]
> Use the appropriate base URL per environment. Always use HTTPS in production.

## Response Envelope
All auth endpoints return `ApiResponse<T>`.

Success example:
```json
{
  "status_code": 200,
  "message": "Login successful",
  "is_success": true,
  "data": {
    "AccessToken": "<jwt>",
    "RefreshToken": "<refresh>",
    "AccessTokenExpiresAt": "2026-01-23T18:31:22.000Z",
    "RefreshTokenExpiresAt": "2026-02-22T18:31:22.000Z"
  },
  "Error": null
}
```

Error example:
```json
{
  "status_code": 401,
  "message": "Invalid email or password.",
  "is_success": false,
  "data": null,
  "Error": {
    "ErrorCode": "UNAUTHORIZED",
    "Timestamp": "2026-01-23T18:31:22.000Z",
    "ValidationErrors": null
  }
}
```

Validation error (HTTP 422):
```json
{
  "status_code": 422,
  "message": "Validation failed",
  "is_success": false,
  "data": null,
  "Error": {
    "ErrorCode": "VALIDATION_ERROR",
    "Timestamp": "2026-01-23T18:31:22.000Z",
    "ValidationErrors": {
      "Email": ["The Email field is not a valid e-mail address."]
    }
  }
}
```

## Authorization Header
For protected APIs, send:
```
Authorization: Bearer <access_token>
```

## Token Response
`AuthResponse` returned by register/login/refresh/google:
- `AccessToken` (JWT)
- `RefreshToken` (random string)
- `AccessTokenExpiresAt` (UTC)
- `RefreshTokenExpiresAt` (UTC)

## Endpoints

### 1) Register

<!-- api-section:start -->
<!-- api-docs:start -->

**Endpoint**: `POST /api/auth/register`

**Description**: Create a new user account and receive authentication tokens.

**Request Body**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `email` | string | ✅ | Valid email address (used as login identifier) |
| `password` | string | ✅ | Min 6 characters, follows password policy |
| `fullName` | string | ❌ | User's full name |
| `phoneNumber` | string | ❌ | Phone number |

**Response**: Returns `AuthResponse` with access and refresh tokens.

**Notes**:
- Email is used as the login identifier
- Password policy is driven by `IdentityOptions` in `appsettings.json`
- New users are automatically assigned the `User` role

<!-- api-docs:end -->
<!-- api-example:start -->

#### Request

```http
POST /api/auth/register HTTP/1.1
Host: localhost:5009
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "P@ssw0rd!",
  "fullName": "John Doe",
  "phoneNumber": "0123456789"
}
```

#### Response

```json
{
  "status_code": 200,
  "message": "Registration successful",
  "is_success": true,
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "base64...",
    "accessTokenExpiresAt": "2026-01-24T18:00:00Z",
    "refreshTokenExpiresAt": "2026-02-23T17:00:00Z"
  }
}
```

<!-- api-example:end -->
<!-- api-section:end -->

### 2) Login

<!-- api-section:start -->
<!-- api-docs:start -->

**Endpoint**: `POST /api/auth/login`

**Description**: Authenticate an existing user and receive authentication tokens.

**Request Body**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `email` | string | ✅ | User's registered email address |
| `password` | string | ✅ | User's password |

**Response**: Returns `AuthResponse` with access and refresh tokens.

**Error Cases**:
- `401 Unauthorized`: Invalid email or password
- `403 Forbidden`: Account is locked or inactive

<!-- api-docs:end -->
<!-- api-example:start -->

#### Request

```http
POST /api/auth/login HTTP/1.1
Host: localhost:5009
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "P@ssw0rd!"
}
```

#### Response

```json
{
  "status_code": 200,
  "message": "Login successful",
  "is_success": true,
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "base64...",
    "accessTokenExpiresAt": "2026-01-24T18:00:00Z",
    "refreshTokenExpiresAt": "2026-02-23T17:00:00Z"
  }
}
```

<!-- api-example:end -->
<!-- api-section:end -->

### 3) Refresh Tokens

<!-- api-section:start -->
<!-- api-docs:start -->

**Endpoint**: `POST /api/auth/refresh`

**Description**: Exchange a refresh token for a new access and refresh token pair.

**Request Body**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `userId` | string (GUID) | ✅ | User ID from JWT `sub` or `nameidentifier` claim |
| `refreshToken` | string | ✅ | Current valid refresh token |

**Response**: Returns `AuthResponse` with new access and refresh tokens.

> [!WARNING]
> **Token Rotation**: On success, new access + refresh tokens are returned. The **old refresh token becomes invalid** immediately. Make sure to update stored tokens.

**Error Cases**:
- `401 Unauthorized`: Invalid or expired refresh token

<!-- api-docs:end -->
<!-- api-example:start -->

#### Request

```http
POST /api/auth/refresh HTTP/1.1
Host: localhost:5009
Content-Type: application/json

{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "refreshToken": "CfDJ8N..."
}
```

#### Response

```json
{
  "status_code": 200,
  "message": "Token refreshed successfully",
  "is_success": true,
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "new_base64...",
    "accessTokenExpiresAt": "2026-01-24T19:00:00Z",
    "refreshTokenExpiresAt": "2026-02-23T18:00:00Z"
  }
}
```

<!-- api-example:end -->
<!-- api-section:end -->

### 4) Google Sign-In

<!-- api-section:start -->
<!-- api-docs:start -->

**Endpoint**: `POST /api/auth/google`

**Description**: Authenticate or register a user using Google OAuth ID token.

**Request Body**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `idToken` | string | ✅ | Google ID token obtained from client-side Google Sign-In |

**Response**: Returns `AuthResponse` with access and refresh tokens.

**Notes**:
- The API validates the token against `Authentication:Google:ClientId` from `appsettings.json`
- If the user does not exist, a new account is created and assigned the `User` role
- Google email must be verified, or the API returns `401 Unauthorized`

**Error Cases**:
- `400 Bad Request`: Google configuration missing or invalid
- `401 Unauthorized`: Invalid token or unverified email

<!-- api-docs:end -->
<!-- api-example:start -->

#### Request

```http
POST /api/auth/google HTTP/1.1
Host: localhost:5009
Content-Type: application/json

{
  "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjdlM..."
}
```

#### Response Example

```json
{
  "status_code": 200,
  "message": "Google sign-in successful",
  "is_success": true,
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "base64...",
    "accessTokenExpiresAt": "2026-01-24T18:00:00Z",
    "refreshTokenExpiresAt": "2026-02-23T17:00:00Z"
  }
}
```

<!-- api-example:end -->
<!-- api-section:end -->

## JWT Claims Issued
The access token includes these claims:
- `sub` (user id)
- `email`
- `unique_name`
- `jti`
- `nameidentifier`
- `name`
- `full_name`
- `phone_number`
- `is_active`
- `role` (one or more)

## Typical Client Flow
1) Register or login to obtain access + refresh tokens.
2) Store tokens securely.
3) Call protected APIs with `Authorization: Bearer <access_token>`.
4) When access token expires, call `/api/auth/refresh` to rotate tokens.
5) Retry the failed request with the new access token.

> [!TIP]
> **Token Storage Best Practices**
> - **Mobile (Flutter)**: Use `flutter_secure_storage` to store tokens
> - **Web (React)**: Use `httpOnly` cookies or secure session storage
> - **Never** store tokens in localStorage on web (XSS vulnerability)

## Common Error Codes
- `UNAUTHORIZED` (401)
- `FORBIDDEN` (403)
- `VALIDATION_ERROR` (422)
- `EMAIL_IN_USE` (400)
- `GOOGLE_CONFIG` (400)
