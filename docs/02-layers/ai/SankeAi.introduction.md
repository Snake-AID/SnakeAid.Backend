# SnakeAI Model Endpoint - API Reference

This document provides detailed technical documentation for the SnakeAI Model Endpoint (FastAPI). It is intended for backend developers implementing clients for this service.

## Base URL

By default, the service runs on port 8000.
`http://localhost:8000`

## Error Handling

All errors return a standard JSON structure.

**Schema:**
```json
{
  "error": {
    "code": "STRING",
    "message": "STRING",
    "details": "ANY"
  }
}
```

**Common Error Codes:**
| Status Code | Error Code | Description |
| :--- | :--- | :--- |
| 400 | `VALIDATION_ERROR` | Invalid input parameters. |
| 400 | `INVALID_CONTENT_TYPE` | Uploaded file is not an image. |
| 400 | `INVALID_IMAGE` | Image data could not be decoded. |
| 400 | `INVALID_BASE64` | Invalid Base64 string. |
| 400 | `URL_FETCH_ERROR` | Failed to fetch image from URL. |
| 413 | `PAYLOAD_TOO_LARGE` | Upload exceeds `MAX_UPLOAD_MB`. |
| 413 | `DOWNLOAD_TOO_LARGE` | URL download exceeds limit. |
| 429 | `RATE_LIMITED` | Request rate limit exceeded (per IP). |
| 429 | `QUEUE_FULL` | Server concurrency limit reached. |
| 503 | `MODEL_NOT_LOADED` | Model failed to load at startup. |
| 504 | `URL_FETCH_TIMEOUT` | Upstream image download timed out. |

---

## Data Structures

### Detection Object
Represents a single detected object (snake).

```json
{
  "class_id": 0,
  "class_name": "snake_species_name",
  "confidence": 0.85,
  "bbox": {
    "x1": 100,
    "y1": 200,
    "x2": 300,
    "y2": 400
  }
}
```

### Response Object (Success)
Returned by all detect endpoints.

```json
{
  "model_version": "snake-yolo12-v1.0",
  "image_width": 1280,
  "image_height": 720,
  "warnings": {
    "blur": 0.05,
    "brightness": 0.45,
    "too_small": 0.0
  },
  "detections": [ ... ],
  "saved_image_path": "/data/saved_images/uuid.jpg" // Optional, present if save_image=true
}
```

---

## Endpoints

### 1. Detect from File
Upload an image file directly via `multipart/form-data`.

- **Method:** `POST`
- **Path:** `/detect/file`
- **Content-Type:** `multipart/form-data`

**Parameters (Query & Form):**
| Parameter | Type | In | Default | Description |
| :--- | :--- | :--- | :--- | :--- |
| `image` | File | Form | Required | Image file (JPEG, PNG, etc). |
| `imgsz` | int | Query | 640 | Inference size (longest side). |
| `conf` | float | Query | 0.25 | Confidence threshold (0.0 - 1.0). |
| `iou` | float | Query | 0.5 | NMS IoU threshold (0.0 - 1.0). |
| `topk` | int | Query | 100 | Max detections to return. |
| `save_image` | bool | Query | false | Save processed image to disk. |

**Example Request:**
```bash
curl -X POST "http://localhost:8000/detect/file?imgsz=640&conf=0.25" \
  -F "image=@/path/to/cobra.jpg"
```

---

### 2. Detect from Base64
Send a raw Base64 string in a JSON body.

- **Method:** `POST`
- **Path:** `/detect/base64`
- **Content-Type:** `application/json`

**Request Body Schema:**
```json
{
  "image_base64": "base64_string_here",
  "imgsz": 640,
  "conf": 0.25,
  "iou": 0.5,
  "topk": 100,
  "save_image": false
}
```

**Example Request:**
```bash
curl -X POST "http://localhost:8000/detect/base64" \
  -H "Content-Type: application/json" \
  -d '{
    "image_base64": "/9j/4AAQSkZJRg...",
    "conf": 0.5
  }'
```

---

### 3. Detect from URL
Server downloads image from a public URL.

- **Method:** `POST`
- **Path:** `/detect/url`
- **Content-Type:** `application/json`

**Request Body Schema:**
```json
{
  "image_url": "https://example.com/snake.jpg",
  "imgsz": 640,
  "conf": 0.25,
  "iou": 0.5,
  "topk": 100,
  "save_image": false
}
```

**Example Request:**
```bash
curl -X POST "http://localhost:8000/detect/url" \
  -H "Content-Type: application/json" \
  -d '{
    "image_url": "https://upload.wikimedia.org/wikipedia/commons/d/d4/King_Cobra.jpg",
    "save_image": true
  }'
```

---

### 4. Health Check
Check service status and model availability.

- **Method:** `GET`
- **Path:** `/health`

**Response:**
```json
{
  "status": "ok",
  "model_loaded": true,
  "model_version": "snake-yolo12-v1.0",
  "uptime_s": 12345
}
```
