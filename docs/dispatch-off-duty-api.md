# Dispatch API - On-duty & Off-duty (FE Implementation Guide)

Tài liệu này tóm tắt thay đổi nhỏ ở API dispatch để FE implement đúng UX khi operator dispatch rescuer (bao gồm trường hợp off-duty backup).

## Endpoint

- POST `/api/incidents/{incidentId}/dispatch`

## Request body

- `rescuerId` (Guid) — required
- `allowOffDuty` (bool) — optional, default `false`. Khi `true` cho phép dispatch rescuer không nằm trong ca trực hiện tại.
- `operatorNote` (string?) — optional, **bắt buộc** khi `allowOffDuty = true`. Dùng để ghi chú xác nhận operator đã gọi/confirm thủ công với rescuer.

Ví dụ (on-duty):

```json
{
  "rescuerId": "550e8400-e29b-41d4-a716-446655440000"
}
```

Ví dụ (off-duty):

```json
{
  "rescuerId": "660f9511-f30c-52e5-b827-557766551111",
  "allowOffDuty": true,
  "operatorNote": "Called 14:35 — confirmed available"
}
```

File request model: SnakeAid.Core/Requests/SnakebiteIncident/DispatchIncidentRequest.cs

## Response (success)

- Vẫn trả `CreateIncidentResponse` như trước. FE có thể đọc `DispatchRequestId` và `DispatchedRescuerId` trong response metadata để xác nhận dispatch thành công.

Ví dụ rút gọn:

```json
{
  "statusCode": 200,
  "message": "Incident dispatched.",
  "data": {
    "id": "...",
    "dispatchRequestId": "...",
    "dispatchedRescuerId": "..."
  }
}
```

## Error cases (FE must handle)

- 400 Bad Request
  - "Rescuer is not currently on shift." — xảy ra khi chọn rescuer off-duty mà `allowOffDuty` không set true.
  - "Operator must confirm manual contact when dispatching an off-duty rescuer." — xảy ra khi `allowOffDuty = true` nhưng `operatorNote` rỗng.
  - "Rescuer is currently offline or unavailable." — không gửi request khi rescuer offline/unavailable.
  - Already-declined or aborted rules — server trả 400 nếu rescuer đã từ chối/aborted cho incident này.

- 404 Not Found — incident hoặc rescuer không tồn tại.
- 409 Conflict — incident đang được operator khác xử lý hoặc concurrency conflict.

## Business rules (server-side, FE must reflect these in UX)

- Default behavior unchanged: nếu FE không gửi `allowOffDuty`, server vẫn CHẶN off-duty dispatch.
- Server luôn kiểm tra `rescuer.IsOnline` và `rescuer.IsAvailable` — FE should only allow dispatch button when rescuer shows online & available in the snapshot.
- Khi dispatch off-duty (`allowOffDuty=true`):
  - FE phải yêu cầu operator thực hiện cuộc gọi/nhắn trước (manual contact).
  - FE phải thu `operatorNote` (ngắn) và gửi cùng request.
  - Server logs the `operatorNote` (currently logged; persistence optional).

## Recommended FE flow

1. Operator mở modal **Chọn cứu hộ viên** → FE gọi `/api/monitoring/on-duty` (primary pool) và hiển thị list.
2. Nếu on-duty list trống hoặc operator mở `Backup` tab → FE gọi `/api/monitoring/off-duty` (backup pool) và hiển thị list.
3. Khi operator chọn rescuer:
   - Nếu rescuer có `isOnDutyNow` → gửi payload on-duty (chỉ `rescuerId`).
   - Nếu rescuer từ backup pool (off-duty): hiển thị modal xác nhận:
     - Show `phoneNumber`, `lastLocationUpdate`, `distanceKm`.
     - Button `Gọi` để operator thực hiện cuộc gọi (manual).
     - Checkbox `Tôi đã gọi và xác nhận` + small `operatorNote` textbox.
     - Khi operator xác nhận, gửi POST với `allowOffDuty=true` và `operatorNote`.
4. Xử lý response:
   - Nếu 200 → đóng modal, hiển thị success + dispatch metadata.
   - Nếu 400 liên quan `allowOffDuty`/`operatorNote` → hiển thị message hướng dẫn operator thực hiện bước gọi/ghi chú.

## UX copy / messages (quick suggestions)

- Confirm modal title: "Xác nhận liên hệ cứu hộ viên ngoài ca"
- Helper text: "Bạn cần gọi số này và xác nhận họ có thể đến giúp. Ghi chú ngắn sẽ được lưu cho audit."
- Field placeholder: "Ghi chú: đã gọi lúc 14:35, đồng ý đến trong 15 phút"

## Backward compatibility & notes

- Clients that don't send `allowOffDuty` keep existing behavior.
- `operatorNote` hiện được ghi log; nếu cần audit/persist trong DB, sẽ cần migration để lưu vào `RescuerRequest` (tôi có thể implement nếu bạn muốn).

## Next steps (optional tasks for backend)

- Persist `operatorNote` and an `IsOffDutyDispatch` flag on `RescuerRequest` + DB migration.
- Add dedicated endpoint `POST /api/incidents/{incidentId}/dispatch/off-duty` if you prefer endpoint separation instead of flag.

---

File created: docs/dispatch-off-duty-api.md
