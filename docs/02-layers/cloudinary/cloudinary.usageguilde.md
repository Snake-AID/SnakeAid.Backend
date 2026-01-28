# Cloudinary Integration - Usage Guide

This guide describes how frontend or mobile clients should upload media once the Cloudinary integration is implemented in SnakeAid.

## Quick Summary
- Uploads go through the SnakeAid backend, not directly to Cloudinary.
- The backend returns a public `secure_url`.
- Domain persistence and AI flows are future phases; this guide focuses on uploads only.

## Authentication
Uploads should require authentication.

Typical flow:
1. Call `POST /api/auth/login`.
2. Read `data.accessToken` from the response.
3. Send `Authorization: Bearer <accessToken>` in upload requests.

## Endpoints

### 1) Upload Image
- Method: `POST`
- Path: `/api/media/upload-image`
- Content-Type: `multipart/form-data`
- Auth: required

Form fields:
- `file`: required, the image file
- `domain`: optional, default `uploads`

Example with curl:
```bash
curl -X POST "https://localhost:7026/api/media/upload-image" \
  -H "Authorization: Bearer <access_token>" \
  -F "file=@C:/images/snake.jpg" \
  -F "domain=uploads"
```

Example response:
```json
{
  "status_code": 200,
  "message": "Image uploaded successfully.",
  "is_success": true,
  "data": {
    "secureUrl": "https://res.cloudinary.com/<cloud>/image/upload/v123/snakeaid/dev/uploads/<userId>/snake_20260127.jpg",
    "publicId": "snakeaid/dev/uploads/<userId>/snake_20260127",
    "resourceType": "image",
    "format": "jpg",
    "bytes": 345678,
    "width": 1280,
    "height": 720,
    "folder": "snakeaid/dev/uploads/<userId>",
    "tags": ["snakeaid", "dev", "uploads"]
  },
  "error": null
}
```

### 2) Upload File
- Method: `POST`
- Path: `/api/media/upload-file`
- Content-Type: `multipart/form-data`
- Auth: required

Form fields:
- `file`: required
- `domain`: optional, default `files`

## Recommended Domain Values
Use consistent domain values to keep Cloudinary organized:
- `uploads`
- `library-media`
- `avatars`
- `certificates`
- `chat`
- `files`
- `report-media` (future plan)

## JavaScript Example
```javascript
async function uploadSnakeImage(file, accessToken) {
  const form = new FormData();
  form.append("file", file);
  form.append("domain", "uploads");

  const res = await fetch("https://localhost:7026/api/media/upload-image", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${accessToken}`
    },
    body: form
  });

  const body = await res.json();
  if (!body.is_success) {
    throw new Error(body.message);
  }

  return body.data.secureUrl;
}
```

## Future Plan - Identification Flow (Deferred)
Identification-specific flows (such as `ReportMedia` persistence and AI handoff) are intentionally deferred in this turn.

## Error Handling Notes
Likely error categories:
- Validation errors for file size or extension.
- Authentication errors when the token is missing or expired.
- Server errors when Cloudinary credentials are not configured.

Client recommendations:
- Always check `is_success` and `status_code`.
- Display `message` to the user when appropriate.
- For 401 responses, redirect to login and refresh the token if supported.

## Future Plan - Upload and Persist
A later endpoint can both upload and create a `ReportMedia` record in one call, reducing client complexity. Prefer that endpoint once it exists.
