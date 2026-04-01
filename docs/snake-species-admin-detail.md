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

### Response model cho map đã tách theo trách nhiệm

- `GeographicRegionResponse` chỉ còn dữ liệu nền để vẽ map: `id`, `name`, `code`, `description`, `displayOrder`, `isActive`, `boundaryCoordinates`
- Đã bỏ khỏi `GeographicRegionResponse`: `isMapped`, `mapping`
- Trạng thái phân bố theo loài rắn lấy riêng từ `RegionSnakeMappingResponse` qua endpoint `GET /api/snake-species/{id}/region-mappings`
- FE cần join theo khóa: `region.id == mapping.geographicRegionId`

---

## API Calls khi load trang

Gọi song song 3 request:

```
GET /api/snake-species/{id}
GET /api/geographic-regions
GET /api/snake-species/{id}/region-mappings
```

- `GET /api/geographic-regions`: tải polygon một lần để khởi tạo map và lưu cache/registry ở FE.
- `GET /api/snake-species/{id}/region-mappings`: tải trạng thái phân bố theo loài rắn hiện tại.
- FE highlight bằng cách so sánh `region.id` với `mapping.geographicRegionId`.

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

### `GET /api/geographic-regions`

Trả về **tất cả regions** kèm polygon boundary. Endpoint này chỉ dùng để render map và cache ở FE.

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
    ]
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
    ]
  }
]
```

**`boundaryCoordinates`**: mảng `[lng, lat]` theo chuẩn GeoJSON, dùng trực tiếp với Leaflet/MapLibre/Google Maps.

### `GET /api/snake-species/{id}/region-mappings`

Trả về danh sách vùng đã mapping cho loài rắn hiện tại, **không chứa lat/lng**.

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

### Cách FE so sánh để visualize

1. Lưu `regionsRegistry` từ `GET /api/geographic-regions` (key: `region.id`).
2. Lưu `mappings` từ `GET /api/snake-species/{id}/region-mappings`.
3. Tạo `mappingByRegionId` (key: `geographicRegionId`).
4. Khi render từng polygon:
   - Có mapping: `mappingByRegionId[region.id]` tồn tại thì tô đậm theo `commonLevelValue`.
   - Không mapping: tô xám mờ.
5. Click polygon:
   - Có mapping: mở modal edit với dữ liệu từ `mappingByRegionId[region.id]`.
   - Không mapping: mở modal thêm mới với `geographicRegionId = region.id`.

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

> `mappingId` lấy từ `id` trong response của `GET /api/snake-species/{id}/region-mappings`

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
  ├── GET /api/snake-species/{id}                    → render form thông tin rắn
  ├── GET /api/geographic-regions                    → init registry polygon
  └── GET /api/snake-species/{id}/region-mappings    → mapping của snake hiện tại

[Map render]
  ├── Có mapping (match theo region.id)  → polygon highlight + tooltip
  └── Không mapping                      → polygon xám

[Click polygon chưa mapping]
    └── Mở modal "Thêm phân bố"
        ├── Dropdown CommonLevel (Rare → Abundant)
        ├── Input Priority (0-100)
        ├── Textarea DistributionNotes
        └── [Lưu] → POST /api/snake-species/{id}/region-mappings
           → re-fetch /api/snake-species/{id}/region-mappings

[Click polygon đã mapping]
    └── Mở modal "Chỉnh sửa phân bố" (pre-fill từ mapping)
        ├── Dropdown CommonLevel
        ├── Input Priority
        ├── Textarea DistributionNotes
    ├── [Lưu] → PATCH /api/snake-species/{id}/region-mappings/{mappingId}
    └── [Xóa] → DELETE /api/snake-species/{id}/region-mappings/{mappingId}
           → re-fetch /api/snake-species/{id}/region-mappings

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
  ├── GET /api/geographic-regions
  │       → build regionsRegistry: Map<regionId, regionGeometry>
  └── GET /api/snake-species/{id}/region-mappings
      → build mappingByRegionId: Map<regionId, mapping>
      → render map:
        - mapped: opacity theo commonLevelValue
        - unmapped: màu xám nhạt

[Hover polygon]
  └── Lookup mappingByRegionId[region.id]
    ├── Có mapping: hiện commonLevel, priority, distributionNotes
    └── Không mapping: hiện "Chưa có dữ liệu"

[Click polygon - chưa mapping]
    └── Mở modal "Thêm phân bố"
        ├── CommonLevel dropdown (mặc định: Common)
        ├── Priority input (mặc định: 0)
        ├── DistributionNotes textarea (optional)
        └── [Lưu]
            → POST /api/snake-species/{id}/region-mappings
              body: { geographicRegionId, commonLevel, priority, distributionNotes }
      → Re-fetch GET /api/snake-species/{id}/region-mappings
      → Re-render map bằng registry có sẵn

[Click polygon - đã mapping]
  └── Mở modal "Chỉnh sửa phân bố" (pre-fill từ mappingByRegionId[region.id])
        ├── CommonLevel dropdown
        ├── Priority input
        ├── DistributionNotes textarea
        ├── [Lưu]
    │   → PATCH /api/snake-species/{id}/region-mappings/{mappingId}
        │     body: chỉ gửi field thay đổi
    │   → Re-fetch GET /api/snake-species/{id}/region-mappings
        └── [Xóa]
      → DELETE /api/snake-species/{id}/region-mappings/{mappingId}
      → Re-fetch GET /api/snake-species/{id}/region-mappings
      → polygon trở về xám

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
- Lưu cache `regionsRegistry` sau lần gọi đầu `GET /api/geographic-regions`, chỉ refresh khi có thay đổi boundary dữ liệu nền
- Sau mỗi thao tác POST/PATCH/DELETE, chỉ re-fetch `GET /api/snake-species/{id}/region-mappings`
- `mappingId` lấy từ `RegionSnakeMappingResponse.id`; `region.id` chỉ dùng để map polygon
