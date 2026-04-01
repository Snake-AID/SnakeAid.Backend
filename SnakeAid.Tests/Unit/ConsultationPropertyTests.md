---
doc_role: test-documentation
module: consultation.property-tests
kind: test
doc_type: reference
status: active
last_updated: 2026-03-30
owners: [backend-team]
source_file: SnakeAid.Tests/Unit/ConsultationPropertyTests.cs
---

# ConsultationPropertyTests — Tài liệu test

## Mục đích

Property-based tests sử dụng FsCheck.Xunit để kiểm chứng tính đúng đắn của logic phát hiện consultation hết giờ và endpoint lịch sử tư vấn expert. Mỗi test chạy 100 iterations với dữ liệu ngẫu nhiên.

## Danh sách Property Tests

### Property 1: BookingService.AutoCompleteElapsedScheduledConsultationsAsync slot-elapsed detection filter

**Validates**: Requirements 1.1, 1.3

Với bất kỳ tập hợp `ConsultationBooking` ngẫu nhiên, logic `BookingService.AutoCompleteElapsedScheduledConsultationsAsync` chỉ trả về booking thỏa đồng thời:
- `Status == Confirmed`
- `ConsultationId != null`
- `TimeSlot.EndTime <= DateTime.UtcNow`
- `Consultation.Status != Completed`

Booking có status khác hoặc slot chưa hết giờ KHÔNG được trả về.

### Property 2: BookingService.AutoCompleteElapsedEmergencyConsultationsAsync emergency-elapsed detection filter

**Validates**: Requirements 1.1, 1.3

Với bất kỳ tập hợp `Consultation` ngẫu nhiên, logic `BookingService.AutoCompleteElapsedEmergencyConsultationsAsync` chỉ trả về consultation thỏa đồng thời:
- `Status == Ongoing`
- `Type == Emergency`
- `StartTime + 30 phút <= DateTime.UtcNow`

### Property 7: Expert history completeness

**Validates**: Requirements 6.2, 6.6

Với expert có N consultation (cả Scheduled và Emergency), khi gọi `GetExpertConsultationsAsync` không filter, `totalItems` phải bằng N.

### Property 8: Expert history filtering

**Validates**: Requirements 6.4, 6.5

Với bất kỳ tổ hợp filter (status, type), tất cả items trong kết quả phải thỏa mãn điều kiện filter. Test thử cả 4 tổ hợp: không filter, chỉ status, chỉ type, cả hai.

### Property 9: Expert history pagination

**Validates**: Requirements 6.3, 7.4

Với `totalItems = T`, `pageSize = S`, `pageNumber = P` hợp lệ:
- Số items trả về `<= S`
- `currentPage == P`
- `totalItems == T`
- `totalPages == ceil(T / S)`

### Property 10: Expert response shape by type

**Validates**: Requirements 7.1, 7.2, 7.3, 8.1, 8.2

Với mỗi consultation trong kết quả:
- Luôn có: `consultationId`, `type`, `status`, `userId`
- Scheduled: `bookingId != null`, `slotStartTime != null`, `slotEndTime != null`, `emergencyRequestId == null`
- Emergency: `emergencyRequestId != null`, `bookingId == null`, `slotStartTime == null`, `slotEndTime == null`, `price == null`

### Property 11: Expert history sorting

**Validates**: Requirements 7.5

Danh sách trả về phải sắp xếp `startTime` giảm dần: `items[i].StartTime >= items[i+1].StartTime` cho mọi i.

## Cách tiếp cận test

- Property 1, 2: test trực tiếp predicate logic (pure function) — không cần mock service
- Property 7–11: test `GetExpertConsultationsAsync` qua in-memory UnitOfWork/Repository mock, evaluate real service predicates trên dữ liệu ngẫu nhiên
- Mỗi test cap số lượng entity ở 10–20 để đảm bảo performance

## Test Infrastructure

- `InMemoryUnitOfWork` + `InMemoryRepository<T>`: mock repository trả dữ liệu pre-populated, evaluate predicate trên LINQ-to-Objects
- `NoOpPaymentService`: stub `IConsultationPaymentService` cho constructor injection
- Không dùng database thật — tất cả test chạy in-memory
