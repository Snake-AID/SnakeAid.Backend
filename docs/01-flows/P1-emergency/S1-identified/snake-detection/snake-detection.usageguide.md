# Snake Detection - Usage Guide

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

```
### Supported File Types
- **Extensions**: `.jpg`, `.jpeg`, `.png`, `.webp`
- **Max Size**: 10MB
- **Purpose**: SnakeIdentification (default)
---

## 🔄 FLOW

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
POST /api/detection/detect/{reportMediaId}
```

#### **Request Body**
*None* (ID is passed in URL path)

#### **Response Example**
```json
{
  "status_code": 200,
  "message": "Snake detection completed successfully.",
  "is_success": true,
  "data": {
    "ai_metadata": {
      "model_version": "7",
      "image_width": 227,
      "image_height": 222,
      "detection_count": 1,
      "warnings": {
        "blur": 0,
        "brightness": 0.6604,
        "too_small": 0.3063
      }
    },
    "results": [
      {
        "ai_detection": {
          "class_id": 3,
          "class_name": "ho_mang_chua",
          "confidence": 0.8951632,
          "bbox": {
            "x1": 9,
            "y1": 40,
            "x2": 189,
            "y2": 198
          }
        },
        "snake": {
          "id": 3,
          "scientificName": "Ophiophagus hannah",
          "slug": "ran-ho-mang-chua",
          "commonName": "Rắn Hổ Mang Chúa",
          "imageUrl": "https://vietnamsnakes.com/storage/snakes/species/55/1737107747_0.jpg",
          "description": "Loài rắn độc dài nhất thế giới, cực kỳ nguy hiểm với lượng nọc độc khổng lồ.",
          "identificationSummary": "Kích thước khổng lồ (4-6m), Vảy đầu lớn, Cổ phình mang hẹp, vân chữ V ngược ở cổ.",
          "primaryVenomType": "Neurotoxic",
          "identification": {
            "physicalTraits": [
              "Kích thước khổng lồ (4-6m)",
              "Cặp vảy chẩm hình cánh bướm, nằm ở ngay phía sau vảy đầu",
              "Phình mang hẹp dài",
              "Màu đen, nâu hoặc vàng chì"
            ],
            "behaviors": [
              "Chủ động tấn công nếu bị kích động",
              "Có khả năng rướn cao thân mình",
              "Là một loài rắn thông minh, sẽ quan sát và phản ứng."
            ],
            "habitat": "Rừng rậm, nương rẫy, gần nguồn nước"
          },
          "symptomsByTime": [
            {
              "timeRange": "0 - 15 phút",
              "signs": ["Đau nhức", "Chóng mặt", "Hoa mắt"],
              "isCritical": true
            },
            {
              "timeRange": "30 - 60 phút",
              "signs": ["Hôn mê", "Suy hô hấp cấp", "Tử vong nhanh"],
              "isCritical": true
            }
          ],
          "firstAidGuidelineOverride": {
            "mode": "Append",
            "steps": [
              "Vận chuyển nạn nhân bằng phương tiện nhanh nhất có thể đến bệnh viện lớn."
            ]
          },
          "riskLevel": 10,
          "isVenomous": true,
          "speciesVenoms": [
            {
              "venomType": {
                "name": "Độc thần kinh",
                "description": "Nọc độc chủ yếu ảnh hưởng đến hệ thần kinh...",
                "firstAidGuideline": {
                  "name": "Sơ cứu Độc thần kinh",
                  "content": {
                    "steps": [
                      { "text": "Di chuyển nhẹ nhàng...", "mediaUrl": "..." },
                      { "text": "Gọi hỗ trợ y tế...", "mediaUrl": "..." }
                    ],
                    "dos": [{ "text": "Kiểm tra mạch...", "mediaUrl": "" }],
                    "donts": [{ "text": "Không tự ý tháo băng...", "mediaUrl": "..." }]
                  },
                  "summary": "Ngăn chặn liệt hô hấp bằng cách băng cố định đúng cách."
                }
              }
            }
          ]
        }
      }
    ],
    "recognition_result_id": "d08d9855-00a7-4e1f-82bb-9935e06044bb"
  },
  "error": null
}
```

#### **Key Response Fields**

| Section | Field | Description |
|:---|:---|:---|
| **Root** | `status_code` | Tiêu chuẩn HTTP status (200, 400, 500...) |
| | `is_success` | Cờ báo hiệu request thành công hay không |
| | `data` | Payload chính chứa kết quả nhận diện |
| **AI Metadata** | `model_version` | Phiên bản AI Model đang chạy |
| | `warnings` | Cảnh báo chất lượng ảnh (`blur`, `brightness`, `too_small`) |
| **Detections** | `results` | Mảng các đối tượng được tìm thấy trong ảnh |
| | `ai_detection` | Thông tin kỹ thuật từ AI (Tên lớp, Confidence, BBox) |
| | `snake` | Dữ liệu đầy đủ về loài rắn được map từ database |
| **Snake Info** | `primaryVenomType` | Loại nọc độc chính (Neurotoxic, Hemotoxic...) |
| | `identification` | Đặc điểm nhận dạng (Ngoại hình, Hành vi, Môi trường) |
| | `symptomsByTime` | Các triệu chứng theo mốc thời gian |
| | `speciesVenoms` | Chi tiết các loại nọc độc và **Hướng dẫn sơ cứu tương ứng** |
| **Guidelines** | `firstAidGuideline` | Chứa các bước (`steps`), việc nên làm (`dos`), không nên làm (`donts`) kèm `mediaUrl` |

---

### **Step 3: Get Historical Results** (Optional)

Retrieve previously saved detection results by RecognitionResultId.

#### **Endpoint**
```
GET /api/detection/{recognitionResultId}
```

---

## 🎨 FRONTEND INTEGRATION EXAMPLES
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
      Uri.parse('$baseUrl/api/detection/detect/$reportMediaId'),
      headers: {
        'Authorization': 'Bearer $_accessToken',
      },
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
   - URL: `{{baseUrl}}/api/detection/detect/{{reportMediaId}}`
   - Headers: `Authorization: Bearer {{accessToken}}`
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