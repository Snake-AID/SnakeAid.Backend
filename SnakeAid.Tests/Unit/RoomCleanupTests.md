---
doc_role: test-documentation
module: consultation.room-cleanup-tests
kind: test
doc_type: reference
status: active
last_updated: 2026-03-30
owners: [backend-team]
source_file: SnakeAid.Tests/Unit/RoomCleanupTests.cs
---

# RoomCleanupTests — Tài liệu test

## Mục đích

Unit tests kiểm chứng hành vi room cleanup khi consultation hết giờ: thứ tự thực thi (signal → delete room → update status), edge cases khi từng bước thất bại, và tính cô lập giữa các consultation.

## Danh sách Unit Tests

### 8.1 — Thứ tự: Signal + DeleteRoom TRƯỚC status update

Verify `AutoCompleteElapsedScheduledConsultationsAsync` gửi `ConsultationCallEnded` signal và gọi `DeleteRoomAsync` TRƯỚC KHI `CommitAsync` (persist status change). Dùng operation log để track thứ tự.

### 8.2 — Emergency: status Completed + EndTime

Verify `AutoCompleteElapsedEmergencyConsultationsAsync` set `Status = Completed` và `EndTime != null` cho emergency consultation hết 30 phút.

### 8.3 — Settlement cho mỗi consultation

Verify `SettleConsultationEscrowAsync` được gọi đúng một lần cho mỗi consultation hoàn tất. Test cả scheduled (2 bookings) và emergency (2 consultations).

### 8.4 — ConsultationCallEnded signal payload

Verify payload chứa đúng `ConsultationId` (Guid) và `Reason = "timeout"`. Dùng reflection để kiểm tra anonymous object.

### 8.5 — DeleteRoomAsync room name format

Verify `DeleteRoomAsync` được gọi với tên phòng format `consultation-{consultationId}`. Test cả scheduled và emergency path.

### 8.6 — SignalR thất bại → vẫn tiếp tục

Khi `SendAsync` throw exception, lifecycle vẫn:
- Gọi `DeleteRoomAsync`
- Cập nhật status `Completed`
- Gọi `SettleConsultationEscrowAsync`

### 8.7 — DeleteRoomAsync thất bại → vẫn cập nhật status

Khi `DeleteRoomAsync` throw exception, consultation vẫn được set `Completed` + `EndTime`. Test cả scheduled và emergency.

### 8.8 — Settlement thất bại → consultation vẫn Completed

Khi `SettleConsultationEscrowAsync` throw exception, consultation entity đã được mutate sang `Completed` trước đó (status update xảy ra trước settlement).

### 8.9 — Một consultation lỗi → các consultation khác vẫn xử lý

Khi `CommitAsync` throw cho consultation đầu tiên, consultation thứ hai vẫn được xử lý thành công. Test cả scheduled và emergency.

## Test Infrastructure

Sử dụng hand-rolled spy/stub (không dùng Moq):

| Spy | Mục đích |
|-----|---------|
| `SpyUnitOfWork` | Trả pre-configured data, record commits, hỗ trợ callback `onCommit` để simulate lỗi |
| `SpyHubContext` | Record `SendAsync` calls (group name, method, payload), hỗ trợ `throwOnSend` |
| `SpyLiveKitService` | Record `DeleteRoomAsync` calls, hỗ trợ `throwOnDelete` |
| `SpyPaymentService` | Record `SettleConsultationEscrowAsync` calls, hỗ trợ `throwOnSettle` |

## Helpers

- `MakeElapsedBooking(consultationId)`: tạo booking với slot hết giờ 5 phút trước, status `Confirmed`, consultation `Ongoing`
- `MakeElapsedEmergencyConsultation(consultationId)`: tạo emergency consultation bắt đầu 35 phút trước (quá ngưỡng 30 phút)
