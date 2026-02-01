# Snake Detection with SnakeLibs - Usage Guide

> **Loại file:** `usageguide.md` - Hướng dẫn sử dụng API/chức năng sau khi implement  
> **Timeline:** Phase 2 Implementation ✅ COMPLETED  
> **Target Audience:** Frontend Developer, Mobile Developer, QA, API Integration

---

## 🎯 OVERVIEW

Phase 2 Snake Detection cung cấp **Two-Step Flow** để nhận diện rắn với species mapping:

1. **Step 1**: Upload media và tạo ReportMedia entity
2. **Step 2**: Detect snake từ ReportMedia với species enrichment từ SnakeLibs

**Key Benefits:**
- ✅ Kết quả được lưu lịch sử trong database
- ✅ Species mapping với tên tiếng Việt, thông tin độc tính
- ✅ Proper audit trail cho mọi detection requests
- ✅ Historical results query support

---

## 📋 PREREQUISITES

### Authentication
All endpoints require JWT Bearer token:

```javascript
const config = {
  headers: {
    'Authorization': `Bearer ${accessToken}`,
    'Content-Type': 'multipart/form-data' // For upload endpoints
  }
}
```

### Supported File Types
- **Extensions**: `.jpg`, `.jpeg`, `.png`, `.webp`
- **Max Size**: 10MB
- **Purpose**: SnakeIdentification (default)

---

## 🔄 PHASE 2 FLOW

### **Step 1: Upload Report Media**

Upload ảnh và tạo ReportMedia entity cho AI processing.

#### **Endpoint**
```
POST /api/media/report
```

#### **Request Parameters**

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `type` | `MediaReferenceType` | ✅ | - | `CommunityReport`, `SnakebiteIncident`, etc. |
| `purpose` | `MediaPurpose` | ❌ | `SnakeIdentification` | Purpose of the media |

**Form Data:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `File` | `IFormFile` | ✅ | Image file (jpg, jpeg, png, webp) |
| `ReferenceId` | `Guid` | ✅ | ID of parent entity (IncidentId, ReportId, etc.) |

#### **JavaScript Example**
```javascript
const uploadMedia = async (file, referenceId) => {
  const formData = new FormData();
  formData.append('File', file);
  formData.append('ReferenceId', referenceId);
  
  const response = await fetch('/api/media/report?type=CommunityReport&purpose=SnakeIdentification', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${accessToken}`
    },
    body: formData
  });
  
  const result = await response.json();
  return result.data; // ReportMediaResponse
};

// Usage
const fileInput = document.getElementById('snakeImage');
const file = fileInput.files[0];
const parentReportId = "550e8400-e29b-41d4-a716-446655440000";

const mediaResult = await uploadMedia(file, parentReportId);
console.log('Uploaded media ID:', mediaResult.id);
```

#### **Response Example**
```json
{
  "success": true,
  "message": "Report media uploaded successfully.",
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "mediaUrl": "https://res.cloudinary.com/snakeaid/image/upload/v1699123456/report-media/userid/filename.jpg",
    "fileName": "snake_photo.jpg",
    "contentType": "image/jpeg",
    "fileSize": 2048576,
    "referenceType": "CommunityReport",
    "purpose": "SnakeIdentification",
    "requiresAIProcessing": true
  },
  "statusCode": 200
}
```

---

### **Step 2: Detect Snake Species**

Nhận diện rắn từ ReportMedia đã upload, bao gồm species mapping.

#### **Endpoint**
```
POST /api/detection/detect
```

#### **Request Body**
```json
{
  "reportMediaId": "123e4567-e89b-12d3-a456-426614174000"
}
```

#### **JavaScript Example**
```javascript
const detectSnake = async (reportMediaId) => {
  const response = await fetch('/api/detection/detect', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${accessToken}`
    },
    body: JSON.stringify({
      reportMediaId: reportMediaId
    })
  });
  
  const result = await response.json();
  return result.data; // SnakeDetectionResponse
};

// Usage (continuing from Step 1)
const detectionResult = await detectSnake(mediaResult.id);
console.log('Detection results:', detectionResult);
```

#### **Response Example**
```json
{
  "success": true,
  "message": "Snake detection completed successfully.",
  "data": {
    "ai_metadata": {
      "model_version": "snake-yolo12-v1.0",
      "image_width": 1280,
      "image_height": 720,
      "detection_count": 1,
      "warnings": {
        "blur": 0.05,
        "brightness": 0.45,
        "too_small": 0.0
      }
    },
    "results": [
      {
        "ai_detection": {
          "class_id": 0,
          "class_name": "king_cobra",
          "confidence": 0.94,
          "bbox": {
            "x1": 100, "y1": 200, "x2": 300, "y2": 400
          }
        },
        "snake": {
          "id": 101,
          "scientificName": "Ophiophagus hannah",
          "commonName": "Rắn hổ mang chúa",
          "slug": "ran-ho-mang-chua",
          "imageUrl": "https://...",
          "isVenomous": true,
          "riskLevel": 9.5,
          "identification": {
             "physicalTraits": ["Cổ bành", "Mắt đen"]
          },
          "firstAidGuidelineOverride": {
             "mode": 0,
             "steps": ["Trấn an nạn nhân", "Bất động chi bị cắn"]
          }
        }
      }
    ],
    "recognition_result_id": "987fcdeb-51a2-4567-8901-234567890abc"
  },
  "statusCode": 200
}
```

---

### **Step 3: Get Historical Results** (Optional)

Retrieve previously saved detection results by RecognitionResultId.

#### **Endpoint**
```
GET /api/detection/{recognitionResultId}
```

#### **JavaScript Example**
```javascript
const getHistoricalResult = async (recognitionResultId) => {
  const response = await fetch(`/api/detection/${recognitionResultId}`, {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${accessToken}`
    }
  });
  
  const result = await response.json();
  return result.data;
};

// Usage
const savedResult = await getHistoricalResult("987fcdeb-51a2-4567-8901-234567890abc");
console.log('Historical detection:', savedResult);
```

---

## 🎨 FRONTEND INTEGRATION EXAMPLES

### **React Hook Example**
```javascript
import { useState, useCallback } from 'react';

export const useSnakeDetection = () => {
  const [isUploading, setIsUploading] = useState(false);
  const [isDetecting, setIsDetecting] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);

  const detectSnakeFromFile = useCallback(async (file, referenceId) => {
    try {
      setError(null);
      setIsUploading(true);
      
      // Step 1: Upload media
      const mediaResult = await uploadMedia(file, referenceId);
      
      setIsUploading(false);
      setIsDetecting(true);
      
      // Step 2: Detect snake
      const detectionResult = await detectSnake(mediaResult.id);
      
      setResult({
        media: mediaResult,
        detection: detectionResult
      });
    } catch (err) {
      setError(err.message);
    } finally {
      setIsUploading(false);
      setIsDetecting(false);
    }
  }, []);

  return {
    detectSnakeFromFile,
    isUploading,
    isDetecting,
    result,
    error
  };
};

// Usage in component
const SnakeDetectionComponent = () => {
  const { detectSnakeFromFile, isUploading, isDetecting, result, error } = useSnakeDetection();
  const [selectedFile, setSelectedFile] = useState(null);
  const reportId = "550e8400-e29b-41d4-a716-446655440000"; // Parent report ID

  const handleDetect = () => {
    if (selectedFile) {
      detectSnakeFromFile(selectedFile, reportId);
    }
  };

  if (isUploading) return <div>Uploading image...</div>;
  if (isDetecting) return <div>Detecting snake species...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
      <input 
        type="file" 
        accept=".jpg,.jpeg,.png,.webp"
        onChange={(e) => setSelectedFile(e.target.files[0])} 
      />
      <button onClick={handleDetect} disabled={!selectedFile}>
        Detect Snake
      </button>
      
      {result && (
        <div className="detection-result">
          <h3>Detection Result</h3>
          {result.detection.results.map((item, index) => (
             <div key={index}>
                <p><strong>Species:</strong> {item.snake?.commonName || item.ai_detection.class_name}</p>
                <p><strong>Scientific:</strong> {item.snake?.scientificName}</p>
                <p><strong>Venomous:</strong> {item.snake?.isVenomous ? 'Yes' : 'No'}</p>
                <p><strong>Risk Level:</strong> {item.snake?.riskLevel}/10</p>
                <p><strong>Confidence:</strong> {(item.ai_detection.confidence * 100).toFixed(1)}%</p>
             </div>
          ))}
        </div>
      )}
    </div>
  );
};
```

### **Flutter/Dart Example**
```dart
import 'dart:io';
import 'package:http/http.dart' as http;
import 'dart:convert';

class SnakeDetectionService {
  final String baseUrl = 'https://api.snakeaid.com';
  final String _accessToken;

  SnakeDetectionService(this._accessToken);

  Future<Map<String, dynamic>> uploadMedia(File file, String referenceId) async {
    var request = http.MultipartRequest('POST', Uri.parse('$baseUrl/api/media/report?type=CommunityReport'));
    request.headers['Authorization'] = 'Bearer $_accessToken';
    
    request.files.add(await http.MultipartFile.fromPath('File', file.path));
    request.fields['ReferenceId'] = referenceId;
    
    var response = await request.send();
    var responseBody = await response.stream.bytesToString();
    
    if (response.statusCode == 200) {
      return json.decode(responseBody);
    } else {
      throw Exception('Upload failed: ${response.statusCode}');
    }
  }

  Future<Map<String, dynamic>> detectSnake(String reportMediaId) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/detection/detect'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $_accessToken',
      },
      body: json.encode({
        'reportMediaId': reportMediaId,
      }),
    );

    if (response.statusCode == 200) {
      return json.decode(response.body);
    } else {
      throw Exception('Detection failed: ${response.statusCode}');
    }
  }

  Future<Map<String, dynamic>> detectSnakeFromFile(File file, String referenceId) async {
    // Step 1: Upload
    final uploadResult = await uploadMedia(file, referenceId);
    final mediaId = uploadResult['data']['id'];
    
    // Step 2: Detect
    final detectionResult = await detectSnake(mediaId);
    
    return {
      'media': uploadResult['data'],
      'detection': detectionResult['data'],
    };
  }
}

// Usage in Flutter widget
class SnakeDetectionWidget extends StatefulWidget {
  @override
  _SnakeDetectionWidgetState createState() => _SnakeDetectionWidgetState();
}

class _SnakeDetectionWidgetState extends State<SnakeDetectionWidget> {
  final SnakeDetectionService _service = SnakeDetectionService(accessToken);
  bool _isLoading = false;
  Map<String, dynamic>? _result;

  Future<void> _detectFromImage(File imageFile) async {
    setState(() {
      _isLoading = true;
    });

    try {
      final result = await _service.detectSnakeFromFile(imageFile, reportId);
      setState(() {
        _result = result;
      });
    } catch (e) {
      // Handle error
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Detection failed: $e')),
      );
    } finally {
      setState(() {
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        if (_isLoading)
          CircularProgressIndicator()
        else if (_result != null)
          DetectionResultWidget(result: _result!),
        // File picker and detect button...
      ],
    );
  }
}
```

---

## 🚨 ERROR HANDLING

### **Common Error Responses**

#### **400 Bad Request**
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": {
    "File": ["The File field is required."],
    "ReferenceId": ["The ReferenceId field is required."]
  },
  "statusCode": 400,
  "errorCode": "VALIDATION_FAILED"
}
```

#### **404 Not Found**
```json
{
  "success": false,
  "message": "ReportMedia not found.",
  "statusCode": 404,
  "errorCode": "NOT_FOUND"
}
```

#### **503 Service Unavailable**
```json
{
  "success": false,
  "message": "Snake detection service is currently unavailable. Please try again later.",
  "statusCode": 503,
  "errorCode": "SERVICE_UNAVAILABLE"
}
```

#### **500 Internal Server Error**
```json
{
  "success": false,
  "message": "Snake detection failed. Please try again later.",
  "statusCode": 500,
  "errorCode": "DETECTION_FAILED"
}
```

### **Error Handling Best Practices**

```javascript
const handleApiCall = async (apiFunction) => {
  try {
    const result = await apiFunction();
    return { success: true, data: result };
  } catch (error) {
    console.error('API Error:', error);
    
    if (error.response) {
      // Server responded with error status
      const { statusCode, errorCode, message } = error.response.data;
      
      switch (statusCode) {
        case 400:
          return { success: false, error: 'Invalid request. Please check your input.' };
        case 404:
          return { success: false, error: 'Resource not found.' };
        case 503:
          return { success: false, error: 'Service temporarily unavailable. Please try again later.' };
        default:
          return { success: false, error: message || 'An unexpected error occurred.' };
      }
    } else {
      // Network error
      return { success: false, error: 'Network error. Please check your connection.' };
    }
  }
};
```

---

## 🧪 TESTING WITH POSTMAN

### **Collection Setup**

**Environment Variables:**
```json
{
  "baseUrl": "https://api.snakeaid.com",
  "accessToken": "your-jwt-token-here",
  "reportId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### **Test Sequence**

1. **Upload Test Image**
   - Method: `POST`
   - URL: `{{baseUrl}}/api/media/report?type=CommunityReport`
   - Headers: `Authorization: Bearer {{accessToken}}`
   - Body: `form-data` with `File` (snake image) and `ReferenceId: {{reportId}}`
   - Test: Save `response.data.id` to `reportMediaId`

2. **Detect Snake**
   - Method: `POST`
   - URL: `{{baseUrl}}/api/detection/detect`
   - Headers: `Authorization: Bearer {{accessToken}}`, `Content-Type: application/json`
   - Body: `{"reportMediaId": "{{reportMediaId}}"}`
   - Test: Save `response.data.recognitionResultId` to `recognitionResultId`

3. **Get Historical Result**
   - Method: `GET`
   - URL: `{{baseUrl}}/api/detection/{{recognitionResultId}}`
   - Headers: `Authorization: Bearer {{accessToken}}`

---

## 🔍 SPECIES INFORMATION FIELDS

The API returns enriched species information when a mapping exists:

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `speciesId` | `int?` | Database ID of the species | `15` |
| `speciesName` | `string?` | Vietnamese common name | `"Rắn hổ mang chúa"` |
| `scientificName` | `string?` | Scientific binomial name | `"Ophiophagus hannah"` |
| `isVenomous` | `bool?` | Whether species is venomous | `true` |
| `riskLevel` | `float?` | Danger level (0-10 scale) | `9.5` |

**Mapping Logic:**
- AI Model detects YOLO class (e.g., "cobra")
- System looks up `AISnakeClassMapping` for active model
- If mapping exists → species info populated
- If no mapping → species fields are `null`

---

## 📊 PERFORMANCE NOTES

### **Typical Response Times**
- **Media Upload**: 2-5 seconds (depends on image size and Cloudinary)
- **Snake Detection**: 3-8 seconds (depends on AI service load)
- **Historical Results**: 100-500ms (database query only)

### **File Size Recommendations**
- **Optimal**: 1-3MB images (balance between quality and speed)
- **Maximum**: 10MB (enforced by validation)
- **Resolution**: 800x600 to 1920x1080 works best for AI model

### **Caching Strategy**
- Recognition results are permanently cached in database
- Use `recognition_result_id` for quick retrieval
- No need to re-run detection for previously processed images

---

## 🎯 NEXT STEPS

Phase 2 provides foundation for:

1. **Emergency Response Flow**: Integration với emergency protocols
2. **Species Library Expansion**: More species mappings và detailed info
3. **Batch Processing**: Multiple image analysis
4. **Real-time Notifications**: WebSocket integration cho instant results
5. **Mobile SDK**: Native mobile libraries cho offline detection

**API Versioning**: Current implementation supports future expansion without breaking changes.