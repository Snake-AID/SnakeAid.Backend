# Admin Dashboard API Endpoints

Tài liệu này mô tả 2 endpoint dùng để lấy danh sách incident và rescue mission cho admin dashboard.

---

## 1. GET `/api/incidents/admin/list`

Lấy danh sách snakebite incident theo từng trang với filter tùy chọn.

### Request

**Headers:**

```
Authorization: Bearer <token>
```

**Query Parameters:**

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `status` | string | ❌ | CSV danh sách status cần filter. Để trống = không filter | `Pending,Verified,Assigned` |
| `since` | ISO 8601 string | ❌ | Lọc incident từ ngày này (theo UTC) | `2026-04-05T00:00:00Z` |
| `until` | ISO 8601 string | ❌ | Lọc incident đến ngày này (theo UTC) | `2026-12-31T23:59:59Z` |
| `page` | integer | ✅ | Trang hiện tại (bắt đầu từ 1) | `1` |
| `pageSize` | integer | ✅ | Số item mỗi trang (mặc định 50) | `50` |

**Hợp lệ Status Values:**

```
Pending, Verified, Assigned, FalseAlarm, Finished, Cancelled, NoRescuerFound, Disputed, Completed
```

**Example Requests:**

```bash
# Lấy trang 1, không filter
GET /api/incidents/admin/list?page=1&pageSize=50

# Filter theo status
GET /api/incidents/admin/list?status=Pending,Verified&page=1&pageSize=50

# Filter theo ngày
GET /api/incidents/admin/list?since=2026-04-01T00:00:00Z&until=2026-04-30T23:59:59Z&page=1

# Kết hợp các filter
GET /api/incidents/admin/list?status=Assigned&since=2026-04-05T00:00:00Z&page=1&pageSize=25
```

**JavaScript/NextJS Example:**

```typescript
// Cách 1: Fetch API
const params = new URLSearchParams({
  status: 'Pending,Verified,Assigned',
  page: '1',
  pageSize: '50'
});

const response = await fetch(`/api/incidents/admin/list?${params}`, {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

// Cách 2: Axios
import axios from 'axios';

const response = await axios.get('/api/incidents/admin/list', {
  headers: { Authorization: `Bearer ${token}` },
  params: {
    status: 'Pending,Verified,Assigned',
    since: new Date('2026-04-05').toISOString(),
    page: 1,
    pageSize: 50
  }
});

// Cách 3: TanStack Query
const { data } = useQuery({
  queryKey: ['incidents', { status: 'Pending', page: 1 }],
  queryFn: async ({ queryKey }) => {
    const [_, filters] = queryKey;
    const response = await fetch(
      `/api/incidents/admin/list?${new URLSearchParams({
        status: filters.status,
        page: String(filters.page),
        pageSize: '50'
      })}`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    return response.json();
  }
});
```

### Response

**Status Code: 200 OK**

```json
{
  "success": true,
  "message": "Admin incident list retrieved.",
  "data": {
    "items": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "status": "Assigned",
        "locationCoordinates": {
          "type": "Point",
          "coordinates": [106.70098, 10.77689]
        },
        "createdAt": "2026-04-05T10:30:00+07:00",
        "address": "District 1, Ho Chi Minh City",
        "assignedRescuerId": "550e8400-e29b-41d4-a716-446655440001",
        "activeMissionStatus": "EnRoute",
        "needsRedispatch": false,
        "handlingOperatorId": "550e8400-e29b-41d4-a716-446655440002"
      }
    ],
    "meta": {
      "total_items": 150,
      "total_pages": 3,
      "current_page": 1,
      "page_size": 50
    }
  }
}
```

**Response Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `items[].id` | uuid | ID của snakebite incident |
| `items[].status` | string | Trạng thái hiện tại (enum: SnakebiteIncidentStatus) |
| `items[].locationCoordinates` | GeoJSON Point | Tọa độ (longitude, latitude) |
| `items[].createdAt` | ISO 8601 string | Thời điểm báo cáo |
| `items[].address` | string | Địa chỉ text |
| `items[].assignedRescuerId` | uuid \| null | ID rescuer được phân công (nếu có) |
| `items[].activeMissionStatus` | string \| null | Trạng thái mission hiện tại (nếu có) |
| `items[].needsRedispatch` | boolean | Có cần phân công lại hay không |
| `items[].handlingOperatorId` | uuid \| null | ID operator đang xử lý |
| `meta.total_items` | integer | Tổng số incident |
| `meta.total_pages` | integer | Tổng số trang |
| `meta.current_page` | integer | Trang hiện tại |
| `meta.page_size` | integer | Số item mỗi trang |

**Status Codes:**

- `200 OK` - Lấy danh sách thành công
- `400 Bad Request` - Status không hợp lệ
- `401 Unauthorized` - Token không hợp lệ hoặc hết hạn
- `403 Forbidden` - Không phải Admin

---

## 2. GET `/api/rescue-missions/admin/list`

Lấy danh sách rescue mission theo từng trang với filter tùy chọn.

### Request

**Headers:**

```
Authorization: Bearer <token>
```

**Query Parameters:**

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `status` | string | ❌ | CSV danh sách status cần filter. Để trống = không filter | `Preparing,EnRoute,RescuerArrived` |
| `since` | ISO 8601 string | ❌ | Lọc mission từ ngày này (theo UTC) | `2026-04-05T00:00:00Z` |
| `until` | ISO 8601 string | ❌ | Lọc mission đến ngày này (theo UTC) | `2026-12-31T23:59:59Z` |
| `page` | integer | ✅ | Trang hiện tại (bắt đầu từ 1) | `1` |
| `pageSize` | integer | ✅ | Số item mỗi trang (mặc định 50) | `50` |

**Hợp lệ Status Values:**

```
Preparing, EnRoute, RescuerArrived, MissionCompleted, Cancelled, MissionAborted
```

**Example Requests:**

```bash
# Lấy trang 1, không filter
GET /api/rescue-missions/admin/list?page=1&pageSize=50

# Filter theo status
GET /api/rescue-missions/admin/list?status=Preparing,EnRoute&page=1&pageSize=50

# Filter theo ngày
GET /api/rescue-missions/admin/list?since=2026-04-01T00:00:00Z&until=2026-04-30T23:59:59Z&page=1

# Kết hợp các filter
GET /api/rescue-missions/admin/list?status=MissionCompleted&since=2026-04-05T00:00:00Z&page=1&pageSize=25
```

**JavaScript/NextJS Example:**

```typescript
// Cách 1: Fetch API
const params = new URLSearchParams({
  status: 'Preparing,EnRoute,RescuerArrived',
  page: '1',
  pageSize: '50'
});

const response = await fetch(`/api/rescue-missions/admin/list?${params}`, {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

// Cách 2: Axios
import axios from 'axios';

const response = await axios.get('/api/rescue-missions/admin/list', {
  headers: { Authorization: `Bearer ${token}` },
  params: {
    status: 'Preparing,EnRoute,RescuerArrived',
    since: new Date('2026-04-05').toISOString(),
    page: 1,
    pageSize: 50
  }
});

// Cách 3: React Query (TanStack Query)
const { data, isLoading } = useQuery({
  queryKey: ['missions', { status: 'Preparing', page: 1 }],
  queryFn: async ({ queryKey }) => {
    const [_, filters] = queryKey;
    const response = await fetch(
      `/api/rescue-missions/admin/list?${new URLSearchParams({
        status: filters.status,
        page: String(filters.page),
        pageSize: '50'
      })}`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    return response.json();
  }
});
```

### Response

**Status Code: 200 OK**

```json
{
  "success": true,
  "message": "Admin rescue mission list retrieved.",
  "data": {
    "items": [
      {
        "id": "660e8400-e29b-41d4-a716-446655440000",
        "incidentId": "550e8400-e29b-41d4-a716-446655440000",
        "rescuerId": "550e8400-e29b-41d4-a716-446655440001",
        "status": "EnRoute",
        "price": 500000,
        "actualCost": null,
        "costFromCenter": 0,
        "createdAt": "2026-04-05T10:35:00+07:00",
        "updatedAt": "2026-04-05T11:00:00+07:00",
        "startedAt": "2026-04-05T10:45:00+07:00",
        "arrivedAt": null,
        "completedAt": null,
        "incidentStatus": "Assigned",
        "incidentAddress": "District 1, Ho Chi Minh City",
        "rescuerName": "Nguyễn Văn A"
      }
    ],
    "meta": {
      "total_items": 85,
      "total_pages": 2,
      "current_page": 1,
      "page_size": 50
    }
  }
}
```

**Response Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `items[].id` | uuid | ID của rescue mission |
| `items[].incidentId` | uuid | ID của snakebite incident liên quan |
| `items[].rescuerId` | uuid | ID của rescuer thực hiện mission |
| `items[].status` | string | Trạng thái mission (enum: RescueMissionStatus) |
| `items[].price` | decimal | Giá tiền công (VND) |
| `items[].actualCost` | decimal \| null | Chi phí thực tế (nếu đã hoàn thành) |
| `items[].costFromCenter` | decimal | Chi phí từ trung tâm |
| `items[].createdAt` | ISO 8601 string | Thời điểm tạo mission |
| `items[].updatedAt` | ISO 8601 string \| null | Lần cập nhật cuối |
| `items[].startedAt` | ISO 8601 string \| null | Thời điểm rescuer bắt đầu |
| `items[].arrivedAt` | ISO 8601 string \| null | Thời điểm rescuer tới nơi |
| `items[].completedAt` | ISO 8601 string \| null | Thời điểm hoàn thành mission |
| `items[].incidentStatus` | string | Trạng thái incident (enum: SnakebiteIncidentStatus) |
| `items[].incidentAddress` | string | Địa chỉ incident |
| `items[].rescuerName` | string | Tên rescuer |
| `meta.total_items` | integer | Tổng số mission |
| `meta.total_pages` | integer | Tổng số trang |
| `meta.current_page` | integer | Trang hiện tại |
| `meta.page_size` | integer | Số item mỗi trang |

**Status Codes:**

- `200 OK` - Lấy danh sách thành công
- `400 Bad Request` - Status không hợp lệ
- `401 Unauthorized` - Token không hợp lệ hoặc hết hạn
- `403 Forbidden` - Không phải Admin

---

## Enum Values

### SnakebiteIncidentStatus

```
0  = Pending
1  = Verified
2  = Assigned
3  = FalseAlarm
4  = Finished
5  = Cancelled
6  = NoRescuerFound
7  = Disputed
8  = Completed
```

### RescueMissionStatus

```
0  = Preparing
1  = EnRoute
2  = RescuerArrived
3  = MissionCompleted
4  = Cancelled
5  = MissionAborted
```

---

## Pagination Strategy

### Lấy trang đầu tiên

```bash
GET /api/incidents/admin/list?page=1&pageSize=50
```

### Lấy trang kế tiếp

```javascript
const nextPage = response.data.meta.current_page + 1;
const hasMore = nextPage <= response.data.meta.total_pages;

if (hasMore) {
  const nextResponse = await fetch(
    `/api/incidents/admin/list?page=${nextPage}&pageSize=50`
  );
}
```

### Ví dụ: Tải tất cả dữ liệu (infinite scroll)

```typescript
const loadMore = async (pageNumber: number) => {
  const response = await fetch(
    `/api/incidents/admin/list?page=${pageNumber}&pageSize=50`,
    { headers: { Authorization: `Bearer ${token}` } }
  );
  
  const result = await response.json();
  setIncidents(prev => [...prev, ...result.data.items]);
  
  return result.data.meta.current_page < result.data.meta.total_pages;
};
```

---

## Note về DateTime

- Tất cả thời gian được trả về dạng **ISO 8601 string** với múi giờ
- VD: `"2026-04-05T10:30:00+07:00"` (Việt Nam GMT+7)
- Khi gửi `since` và `until` parameter, dùng format: `2026-04-05T00:00:00Z` (UTC)

**Chuyển đổi timezone trong NextJS:**

```typescript
// Chuyển từ local time sang ISO string
const vietnamTime = new Date('2026-04-05');
const isoString = vietnamTime.toISOString();
// "2026-04-05T00:00:00.000Z" (UTC)

// Parse response datetime
const createdAt = new Date('2026-04-05T10:30:00+07:00');
console.log(createdAt.toLocaleString('vi-VN'));
// "5/4/2026, 10:30:00"
```

---

## Authorization

Cả 2 endpoint yêu cầu:

- **Role**: `Admin`
- **Header**: `Authorization: Bearer <JWT_TOKEN>`

Nếu không phải Admin, API sẽ trả về `403 Forbidden`.

---

## Error Handling

### Invalid Status

```json
{
  "success": false,
  "message": "Invalid status value",
  "errors": ["InvalidStatus"]
}
```

### Invalid Pagination

```json
{
  "success": false,
  "message": "Page and pageSize must be greater than 0",
  "errors": ["InvalidPagination"]
}
```

---

## Performance Tips

1. **Limit pageSize**: Tránh request quá nhiều data cùng lúc. Dùng `pageSize: 25-50`
2. **Filter trước**: Dùng `status` filter để giảm số kết quả
3. **Date range**: Kết hợp `since` + `until` để lọc theo ngày
4. **Caching**: Cache response trong frontend nếu dữ liệu không thay đổi thường xuyên

---

**Last Updated**: 2026-04-05
**API Version**: v1
