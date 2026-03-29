# Snake Species Admin Detail - API Documentation

## Tổng quan màn hình

Trang detail rắn cho admin gồm 2 phần chính chạy song song khi load:

1. **Thông tin rắn** - tên, mô tả, ảnh, venom, antivenom, first aid,...
2. **Bản đồ phân bố** - map Việt Nam với các polygon khu vực, highlight những vùng con rắn đã được mapping

---

## Thay đổi breaking so với trước

### Slug không còn nhập tay

- `Slug` đã bị **xóa khỏi** `CreateSnakeSpeciesRequest` và `UpdateSnakeSpeciesRequest`
- Slug được **tự động generate** từ `CommonName` (sau khi normalize dấu tiếng Việt, lowercase, replace space bằng `-`)
- Nếu `CommonName` trống khi tạo từ Excel, fallback sang `ScientificName`
- Alternative names cũng tự generate slug, không cần truyền lên

---

## API Calls khi load trang

Gọi song song 2 request:

```
GET /api/snake-species/{id}
GET /api/geographic-regions?snakeSpeciesId={id}
```

---

## 1. Thông tin rắn

### `GET /api/snake-species/{id}`

**Response: `DetailSnakeSpeciesResponse`**

```json
{
  "id": 5,
  "scientificName": "Naja kaouthia",
  "commonName": "Rắn hổ mang một mắt kính",
  "slug": "ran-ho-mang-mot-mat-kinh",
  "imageUrl": "https://...",
  "description": "...",
  "identificationSummary": "...",
  "isVenomous": true,
  "primaryVenomType": "Neurotoxic",
  "riskLevel": 8.5,
  "isActive": true,
  "alternativeNames": ["Hổ mang bành", "Cobra"],
  "venoms": [
    { "venomType": "Neurotoxin", "description": "..." }
  ],
  "antivenoms": [
    { "antivenomName": "Naja Antivenom", "manufacturer": "...", "effectiveness": "..." }
  ],
  "identification": {
    "physicalTraits": ["Cổ bành rộng", "Vảy bóng"],
    "behaviors": ["Dựng đứng khi bị đe dọa"],
    "habitat": "Đồng ruộng, bờ kênh"
  },
  "firstAidGuidelineOverride": { ... }
}
```

### `PUT /api/snake-species/{id}`

**Request: `UpdateSnakeSpeciesRequest`** - tất cả field optional, chỉ gửi field cần thay đổi

```json
{
  "scientificName": "Naja kaouthia",
  "commonName": "Rắn hổ mang một mắt kính",
  "mediaId": "guid",
  "description": "...",
  "identificationSummary": "...",
  "isVenomous": true,
  "primaryVenomType": "Neurotoxic",
  "riskLevel": 8.5,
  "isActive": true,
  "venomIds": [1, 2],
  "antivenomIds": [3],
  "alternativeNames": ["Hổ mang bành", "Cobra"]
}
```

> Lưu ý: Không có field `slug` - backend tự generate khi `commonName` thay đổi.

---

## 2. Bản đồ phân bố

### `GET /api/geographic-regions?snakeSpeciesId={id}`

Trả về **tất cả regions** kèm polygon và trạng thái mapping cho con rắn được chỉ định.

**Response: `List<GeographicRegionResponse>`**

```json
[
  {
    "id": 1,
    "name": "Đông Nam Bộ",
    "code": "DNB",
    "description": "Vùng Đông Nam Bộ gồm TP.HCM, Bình Dương...",
    "displayOrder": 1,
    "isActive": true,
    "boundaryCoordinates": [
      [106.1, 10.2],
      [107.3, 10.5],
      [107.8, 11.1],
      [106.1, 10.2]
    ],
    "isMapped": false,
    "mapping": null
  },
  {
    "id": 2,
    "name": "Tây Nam Bộ",
    "code": "TNB",
    "description": "Vùng đồng bằng sông Cửu Long",
    "displayOrder": 2,
    "isActive": true,
    "boundaryCoordinates": [
      [104.8, 9.1],
      [106.2, 9.3],
      [106.5, 10.1],
      [104.8, 9.1]
    ],
    "isMapped": true,
    "mapping": {
      "id": 15,
      "geographicRegionId": 2,
      "regionName": "Tây Nam Bộ",
      "regionCode": "TNB",
      "commonLevel": "VeryCommon",
      "commonLevelValue": 4,
      "priority": 10,
      "distributionNotes": "Thường gặp ở vùng ven sông, mùa mưa xuất hiện nhiều hơn",
      "isActive": true
    }
  }
]
```

**`boundaryCoordinates`**: mảng `[lng, lat]` theo chuẩn GeoJSON, dùng trực tiếp với Leaflet/MapLibre/Google Maps.

**Logic render map:**

- `isMapped: true` → highlight polygon (màu xanh/cam tùy design)
- `isMapped: false` → polygon xám mờ, clickable để thêm mới
- Click vào polygon `isMapped: true` → mở form edit, pre-fill từ `mapping`
- Click vào polygon `isMapped: false` → mở form thêm mới

---

## 3. Quản lý mapping phân bố

### `CommonLevel` enum

| Value | String | Ý nghĩa |
|-------|--------|---------|
| 1 | `Rare` | Rất hiếm gặp |
| 2 | `Uncommon` | Ít gặp |
| 3 | `Common` | Phổ biến |
| 4 | `VeryCommon` | Rất phổ biến |
| 5 | `Abundant` | Cực kỳ phổ biến (loài đặc trưng) |

---

### Thêm 1 region mapping

**`POST /api/snake-species/{id}/region-mappings`**

```json
{
  "geographicRegionId": 1,
  "commonLevel": "Common",
  "priority": 5,
  "distributionNotes": "Thường gặp ở vùng ven sông"
}
```

> Trả về `RegionSnakeMappingResponse` của mapping vừa tạo.
> Lỗi 400 nếu region đó đã được mapped (dùng PATCH để update).

---

### Update 1 region mapping

**`PATCH /api/snake-species/{id}/region-mappings/{mappingId}`**

> `mappingId` lấy từ `mapping.id` trong response của `GET /api/geographic-regions?snakeSpeciesId={id}`

Tất cả field optional:

```json
{
  "commonLevel": "VeryCommon",
  "priority": 10,
  "distributionNotes": "Cập nhật ghi chú phân bố",
  "isActive": true
}
```

---

### Xóa 1 region mapping

**`DELETE /api/snake-species/{id}/region-mappings/{mappingId}`**

Không có body. Trả về 200 nếu thành công.

---

### Sync toàn bộ (batch replace)

**`PUT /api/snake-species/{id}/region-mappings`**

Thay thế toàn bộ mappings hiện có bằng danh sách mới. Regions không có trong list sẽ bị xóa.

```json
{
  "mappings": [
    {
      "geographicRegionId": 2,
      "commonLevel": "VeryCommon",
      "priority": 10,
      "distributionNotes": "Thường gặp ở vùng ven sông",
      "isActive": true
    },
    {
      "geographicRegionId": 4,
      "commonLevel": "Common",
      "priority": 5,
      "distributionNotes": null,
      "isActive": true
    }
  ]
}
```

> Dùng khi admin chọn/bỏ chọn nhiều vùng cùng lúc rồi save 1 lần.
> Trả về `List<RegionSnakeMappingResponse>` sau khi sync.

---

### Danh sách mapping (sidebar/table)

**`GET /api/snake-species/{id}/region-mappings`**

Chỉ trả về những regions **đã được mapped**, không có polygon.
Dùng cho sidebar list hoặc table tóm tắt, không cần cho map render.

```json
[
  {
    "id": 15,
    "geographicRegionId": 2,
    "regionName": "Tây Nam Bộ",
    "regionCode": "TNB",
    "commonLevel": "VeryCommon",
    "commonLevelValue": 4,
    "priority": 10,
    "distributionNotes": "Thường gặp ở vùng ven sông",
    "isActive": true
  }
]
```

---

## Gợi ý UX flow trên màn hình

```
[Load trang]
    ├── GET /api/snake-species/{id}          → render form thông tin rắn
    └── GET /api/geographic-regions?snakeSpeciesId={id}  → render map

[Map render]
    ├── isMapped: true  → polygon highlight + tooltip tên vùng + commonLevel
    └── isMapped: false → polygon xám

[Click polygon isMapped: false]
    └── Mở modal "Thêm phân bố"
        ├── Dropdown CommonLevel (Rare → Abundant)
        ├── Input Priority (0-100)
        ├── Textarea DistributionNotes
        └── [Lưu] → POST /api/snake-species/{id}/region-mappings
                   → refresh map data

[Click polygon isMapped: true]
    └── Mở modal "Chỉnh sửa phân bố" (pre-fill từ mapping)
        ├── Dropdown CommonLevel
        ├── Input Priority
        ├── Textarea DistributionNotes
        ├── [Lưu] → PATCH /api/snake-species/{id}/region-mappings/{mapping.id}
        └── [Xóa] → DELETE /api/snake-species/{id}/region-mappings/{mapping.id}
                   → refresh map data

[Batch mode - chọn nhiều vùng]
    └── [Save tất cả] → PUT /api/snake-species/{id}/region-mappings
```

---

## Ý tưởng UI: Bản đồ phân bố vùng miền

### Bố cục

```
┌─────────────────────────────────────────────────────────┐
│  Phân Bố Địa Lý                                         │
│  ─────────────────────────────────────────────────────  │
│                                                         │
│  ┌──────────────────────────┐  ┌─────────────────────┐  │
│  │                          │  │ Danh sách phân bố   │  │
│  │     [BẢN ĐỒ VIỆT NAM]   │  │ (optional sidebar)  │  │
│  │                          │  │                     │  │
│  │   Polygon xám nhạt       │  │ • Tây Nam Bộ        │  │
│  │   = chưa mapping         │  │   VeryCommon  P:10  │  │
│  │                          │  │                     │  │
│  │   Polygon màu đậm        │  │ • Duyên hải NTB     │  │
│  │   = đã mapping           │  │   Common      P:5   │  │
│  │   (đậm hơn = phổ biến    │  │                     │  │
│  │    hơn theo CommonLevel) │  │ [+ Thêm vùng]       │  │
│  │                          │  └─────────────────────┘  │
│  └──────────────────────────┘                           │
└─────────────────────────────────────────────────────────┘
```

---

### Màu sắc polygon theo trạng thái

| Trạng thái | Fill | Opacity | Border |
|-----------|------|---------|--------|
| Chưa mapping | `#94a3b8` (slate-400) | 0.2 | `#64748b` 1px |
| Rare (1) | `#22c55e` (green-500) | 0.25 | `#16a34a` 1.5px |
| Uncommon (2) | `#22c55e` | 0.40 | `#16a34a` 1.5px |
| Common (3) | `#22c55e` | 0.55 | `#16a34a` 2px |
| VeryCommon (4) | `#22c55e` | 0.70 | `#16a34a` 2px |
| Abundant (5) | `#22c55e` | 0.90 | `#15803d` 2.5px |

> Opacity tăng dần theo `commonLevelValue` → vùng phổ biến hơn trông đậm hơn trực quan.

---

### Tooltip khi hover polygon

```
┌─────────────────────────┐
│ 🟢 Tây Nam Bộ           │
│ Mức độ: Rất phổ biến    │
│ Ưu tiên: 10             │
│ "Thường gặp ở vùng      │
│  ven sông, mùa mưa..."  │
│                         │
│ [Chỉnh sửa] [Xóa]      │
└─────────────────────────┘
```

Với polygon chưa mapping:

```
┌─────────────────────────┐
│ ⬜ Đông Nam Bộ          │
│ Chưa có dữ liệu         │
│                         │
│ [+ Thêm phân bố]        │
└─────────────────────────┘
```

---

### Modal thêm / chỉnh sửa mapping

```
┌──────────────────────────────────────┐
│ Phân bố tại: Tây Nam Bộ             │
│ ────────────────────────────────── │
│                                      │
│ Mức độ phổ biến *                    │
│ ┌──────────────────────────────────┐ │
│ │ Rất phổ biến (VeryCommon)    ▼  │ │
│ └──────────────────────────────────┘ │
│                                      │
│ Độ ưu tiên hiển thị (0-100)          │
│ ┌──────────────────────────────────┐ │
│ │ 10                               │ │
│ └──────────────────────────────────┘ │
│                                      │
│ Ghi chú phân bố                      │
│ ┌──────────────────────────────────┐ │
│ │ Thường gặp ở vùng ven sông...   │ │
│ │                                  │ │
│ └──────────────────────────────────┘ │
│                                      │
│ [Hủy]              [Xóa] [Lưu]      │
└──────────────────────────────────────┘
```

---

### UX Flow chi tiết

```
[Load trang]
    ├── GET /api/snake-species/{id}
    │       → render form thông tin rắn
    └── GET /api/geographic-regions?snakeSpeciesId={id}
            → render map với tất cả polygons
            → isMapped: true  → polygon màu xanh, opacity theo commonLevelValue
            → isMapped: false → polygon xám nhạt

[Hover polygon]
    └── Hiện tooltip với thông tin mapping hoặc "Chưa có dữ liệu"

[Click polygon - chưa mapping]
    └── Mở modal "Thêm phân bố"
        ├── CommonLevel dropdown (mặc định: Common)
        ├── Priority input (mặc định: 0)
        ├── DistributionNotes textarea (optional)
        └── [Lưu]
            → POST /api/snake-species/{id}/region-mappings
              body: { geographicRegionId, commonLevel, priority, distributionNotes }
            → Re-fetch GET /api/geographic-regions?snakeSpeciesId={id}
            → Map tự cập nhật polygon màu

[Click polygon - đã mapping]
    └── Mở modal "Chỉnh sửa phân bố" (pre-fill từ region.mapping)
        ├── CommonLevel dropdown
        ├── Priority input
        ├── DistributionNotes textarea
        ├── [Lưu]
        │   → PATCH /api/snake-species/{id}/region-mappings/{region.mapping.id}
        │     body: chỉ gửi field thay đổi
        │   → Re-fetch map
        └── [Xóa]
            → DELETE /api/snake-species/{id}/region-mappings/{region.mapping.id}
            → Re-fetch map → polygon trở về xám

[Sidebar danh sách - optional]
    └── Hiển thị GET /api/snake-species/{id}/region-mappings
        → List các vùng đã mapped, mỗi item có nút Edit/Delete
        → Nút Edit mở modal tương tự click polygon
        → Nút [+ Thêm vùng] mở modal với dropdown chọn region
```

---

### Lưu ý kỹ thuật cho FE

- `boundaryCoordinates` là `[[lng, lat], ...]` - **lng trước, lat sau** (chuẩn GeoJSON)
- Leaflet dùng `[lat, lng]` → cần swap khi dùng với Leaflet: `coords.map(c => [c[1], c[0]])`
- MapLibre/Mapbox dùng `[lng, lat]` → dùng thẳng không cần swap
- Sau mỗi thao tác POST/PATCH/DELETE, re-fetch `GET /api/geographic-regions?snakeSpeciesId={id}` để sync lại toàn bộ map state
- `region.mapping.id` là `mappingId` dùng cho PATCH/DELETE, không phải `region.id`
