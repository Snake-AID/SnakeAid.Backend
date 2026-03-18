# Tích hợp FE cho Snake Catching Request SignalR

Tài liệu này hướng dẫn Front-End implement realtime cho 4 luồng:
- Create request
- Accept request
- Assign request
- Cancel request

Dựa trên backend hiện tại, các event được phát từ `RescuerHub` route:
- `/rescuer-hub`

---

## 1) Event contract

Backend hiện phát các event sau:

1. `SnakeCatchingRequestCreated`
2. `SnakeCatchingRequestAccepted`
3. `SnakeCatchingRequestAssigned`
4. `SnakeCatchingRequestCancelled`

### Nơi nhận event

- `Operators` group nhận cả 4 event.
- User tạo request (`UserId`) nhận cả 4 event liên quan request của họ.
- Rescuer được assign (`AssignedRescuerId`) nhận:
  - `SnakeCatchingRequestAssigned`
  - `SnakeCatchingRequestCancelled`

### Payload (actual backend `newResponse`)

Lưu ý: backend đang gửi payload rút gọn theo từng event, **không phải full DTO**.

- `SnakeCatchingRequestCreated`
  - `{ id, userId, address, locationCoordinates, additionalDetails, status, estimatedPrice, distanceKm, createdAt, user }`
- `SnakeCatchingRequestAccepted`
  - `{ id, status, confirmedAt, prePaidAt, isPrePaid }`
- `SnakeCatchingRequestAssigned`
  - `{ id, status, assignedAt, assignedRescuerId, assignedRescuer }`
- `SnakeCatchingRequestCancelled`
  - `{ id, userId, status, cancellationReason }`

Các field FE nên ưu tiên để merge state:
- `id`
- `status`
- `userId` (chỉ có ở `Created`, `Cancelled`)
- `confirmedAt`, `prePaidAt`, `isPrePaid` (chỉ có ở `Accepted`)
- `assignedRescuerId`, `assignedAt` (chỉ có ở `Assigned`)
- `cancellationReason` (chỉ có ở `Cancelled`)

---

## 2) Kết nối SignalR từ FE

Cài package:

```bash
npm i @microsoft/signalr
```

Tạo connection:

```ts
import * as signalR from "@microsoft/signalr";

export function createRescuerHubConnection(accessToken: string) {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}/rescuer-hub`, {
      accessTokenFactory: () => accessToken,
      withCredentials: true,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000])
    .build();
}
```

> Khuyến nghị: luôn truyền JWT để backend resolve được `UserId` cho luồng `Clients.User(...)`.

---

## 3) Join group theo role

### Operator dashboard

Sau khi `connection.start()` thành công, gọi:

```ts
await connection.invoke("JoinAsOperator");
```

Nếu không join group này, operator sẽ không nhận event broadcast theo group `Operators`.

### Member/Rescuer app

- Member và rescuer nhận luồng `Clients.User(...)` theo `UserId`.
- Không bắt buộc gọi `JoinAsOperator`.
- Chỉ cần kết nối hub bằng token đúng user.

---

## 4) Subscribe event trên FE

```ts
type SnakeCatchingRequestCreatedEvent = {
  id: string;
  userId: string;
  address: string;
  locationCoordinates: unknown;
  additionalDetails: string;
  status: string;
  estimatedPrice?: number | null;
  distanceKm?: number | null;
  createdAt?: string | null;
  user?: unknown;
};

type SnakeCatchingRequestAcceptedEvent = {
  id: string;
  status: string;
  confirmedAt?: string | null;
  prePaidAt?: string | null;
  isPrePaid: boolean;
};

type SnakeCatchingRequestAssignedEvent = {
  id: string;
  status: string;
  assignedAt?: string | null;
  assignedRescuerId?: string | null;
  assignedRescuer?: unknown;
};

type SnakeCatchingRequestCancelledEvent = {
  id: string;
  userId: string;
  status: string;
  cancellationReason?: string | null;
};

export function bindSnakeCatchingRequestEvents(
  connection: signalR.HubConnection,
  handlers: {
    onCreated?: (data: SnakeCatchingRequestCreatedEvent) => void;
    onAccepted?: (data: SnakeCatchingRequestAcceptedEvent) => void;
    onAssigned?: (data: SnakeCatchingRequestAssignedEvent) => void;
    onCancelled?: (data: SnakeCatchingRequestCancelledEvent) => void;
  }
) {
  connection.on("SnakeCatchingRequestCreated", (data) => handlers.onCreated?.(data));
  connection.on("SnakeCatchingRequestAccepted", (data) => handlers.onAccepted?.(data));
  connection.on("SnakeCatchingRequestAssigned", (data) => handlers.onAssigned?.(data));
  connection.on("SnakeCatchingRequestCancelled", (data) => handlers.onCancelled?.(data));
}

export function unbindSnakeCatchingRequestEvents(connection: signalR.HubConnection) {
  connection.off("SnakeCatchingRequestCreated");
  connection.off("SnakeCatchingRequestAccepted");
  connection.off("SnakeCatchingRequestAssigned");
  connection.off("SnakeCatchingRequestCancelled");
}
```

---

## 5) Mapping UI theo event

### A. Operator màn danh sách request

- `SnakeCatchingRequestCreated`
  - Insert request mới lên đầu list hoặc refresh list nhẹ.
- `SnakeCatchingRequestAccepted`
  - Update row: `status = Confirmed`, set `confirmedAt`, `prePaidAt`, `isPrePaid` nếu có.
- `SnakeCatchingRequestAssigned`
  - Update row: `status = Assigned`, set `assignedRescuerId`, `assignedAt`.
- `SnakeCatchingRequestCancelled`
  - Update row: `status = Cancelled`, show `cancellationReason`.

### B. Member màn “request của tôi”

- Nhận đủ 4 event cùng request của user.
- Cập nhật trạng thái timeline realtime không cần polling.

### C. Rescuer app

- Nhận `SnakeCatchingRequestAssigned` khi được phân công.
- Nhận `SnakeCatchingRequestCancelled` nếu job bị hủy.

---

## 6) Tránh duplicate dữ liệu

Khuyến nghị store chuẩn hóa theo `requestId`:

- Khi nhận event:
  - Nếu chưa có `requestId` => thêm mới.
  - Nếu đã có `requestId` => merge update theo payload mới nhất.
- Ưu tiên `updated status` từ event hơn state local cũ.

Pseudo reducer:

```ts
state.requestsById[payload.id] = {
  ...state.requestsById[payload.id],
  ...payload,
};
```

---

## 7) Reconnect strategy

Khi mất kết nối và reconnect thành công:

1. Nếu role operator: gọi lại `JoinAsOperator`.
2. Gọi API `GET /api/snakecatching/requests` để đồng bộ lại trạng thái cuối.

Ví dụ:

```ts
connection.onreconnected(async () => {
  if (currentUser.role === "Operator" || currentUser.role === "Admin") {
    await connection.invoke("JoinAsOperator");
  }
  await refetchSnakeCatchingRequests();
});
```

---

## 8) Checklist test FE

1. Member tạo request mới:
   - Operator nhận `SnakeCatchingRequestCreated`.
   - Member nhận `SnakeCatchingRequestCreated`.
2. Operator accept request:
   - Operator nhận `SnakeCatchingRequestAccepted`.
   - Member nhận `SnakeCatchingRequestAccepted`.
3. Operator assign rescuer:
   - Operator nhận `SnakeCatchingRequestAssigned`.
   - Member nhận `SnakeCatchingRequestAssigned`.
   - Rescuer được assign nhận `SnakeCatchingRequestAssigned`.
4. Member cancel request:
   - Operator nhận `SnakeCatchingRequestCancelled`.
   - Member nhận `SnakeCatchingRequestCancelled`.
   - Nếu đã assign, rescuer nhận `SnakeCatchingRequestCancelled`.

---

## 9) Lưu ý quan trọng

- Event đang phát qua `RescuerHub` (không phải `MissionHub`).
- Operator bắt buộc join group `Operators` bằng `JoinAsOperator`.
- Nếu FE không gửi JWT khi connect hub, các event theo `Clients.User(...)` có thể không về đúng client.
- Nếu bạn dùng nhiều tab trình duyệt, nên dùng lock hoặc dedupe để tránh xử lý event lặp.
