# SOS Dispatch - Cancellation Reason Contract

Cap nhat: 2026-04-14

Tai lieu nay tong hop cac thay doi backend lien quan den luong huy dispatch request, de Flutter client (rescuer app) va FE operator implement dong nhat.

## 1) Muc tieu thay doi

Truoc day, ly do huy request duoc truyen dang text roi rac. Hien tai da chuan hoa thanh reason code de:

- FE parse on dinh, khong phu thuoc vao text.
- Operator dashboard filter/report de dang hon.
- Van giu message than thien cho nguoi dung.

## 2) Reason code chuan

Nguon dinh nghia:

- `DispatchRequestCancelReasonCodes`

Danh sach code:

- `CANCELLED_BY_MEMBER`
- `CANCELLED_BY_OPERATOR`
- `CANCELLED_BY_REDISPATCH`

Y nghia:

- `CANCELLED_BY_MEMBER`: Member huy ca SOS (hoac huy luong lien quan), cac pending request bi huy theo.
- `CANCELLED_BY_OPERATOR`: Operator chu dong huy request pending.
- `CANCELLED_BY_REDISPATCH`: Operator dispatch rescuer moi, cac pending request cua rescuers khac bi auto-cancel.

## 3) Thay doi du lieu backend

### 3.1 RescuerRequest.DeclineReason

- Khi `Status = Cancelled`, truong `DeclineReason` se luu reason code (khong luu sentence free-text nua).
- Khi `Status = Declined`, `DeclineReason` van la ly do tu rescuer (free text) nhu truoc.

Quy tac nay giup FE phan biet ro:

- `Declined` => ly do nguoi dung nhap.
- `Cancelled` => ly do he thong/chuc nang theo code.

### 3.2 DispatchIncidentAsync (redispatch)

Khi operator dispatch rescuer moi cho cung incident:

- Pending requests cua rescuers khac se bi set `Cancelled`.
- `DeclineReason` cua cac request do = `CANCELLED_BY_REDISPATCH`.
- Sau commit transaction, backend gui notify huy toi tung rescuer bi anh huong.

## 4) Thay doi contract SignalR

Hub lien quan: `RescuerHub`
Event lien quan: `RequestCancelled`

### 4.1 Payload toi rescuer (client specific)

Event `RequestCancelled` gui den rescuer online gom:

- `RequestId: Guid`
- `ReasonCode: string`
- `Message: string`

### 4.2 Payload toi Monitors

Event `RequestCancelled` gui den group `Monitors` gom:

- `RequestId: Guid`
- `RescuerId: Guid`
- `ReasonCode: string`
- `Message: string`

### 4.3 Message mapping server

Backend map reason code sang message user-facing:

- `CANCELLED_BY_REDISPATCH` => "Yeu cau da bi huy vi dieu phoi vien da dieu phoi cho cuu ho vien khac."
- `CANCELLED_BY_OPERATOR` => "Yeu cau da bi huy boi dieu phoi vien."
- mac dinh (hoac khong co code) => "Yeu cau da bi huy boi nguoi dung."

## 5) Huong dan cho Flutter rescuer app

### 5.1 Model de nghi

```dart
class RequestCancelledEvent {
  final String requestId;
  final String? reasonCode;
  final String? message;

  RequestCancelledEvent({
    required this.requestId,
    this.reasonCode,
    this.message,
  });
}
```

### 5.2 Handling de nghi

- Uu tien xu ly theo `reasonCode`.
- Dung `message` de hien thi toast/dialog.
- Neu `reasonCode` null (truong hop backward compatibility), fallback nhu `CANCELLED_BY_MEMBER`.

Pseudo logic:

```text
if reasonCode == CANCELLED_BY_REDISPATCH -> hien thi badge "Da chuyen cho rescuer khac"
if reasonCode == CANCELLED_BY_OPERATOR -> hien thi "Operator da huy"
else -> hien thi "Member da huy"
```

## 6) Huong dan cho FE operator

### 6.1 Man hinh dispatch history

Khi render row `RescuerRequest`:

- Neu `Status = Cancelled`, doc `DeclineReason` nhu reason code.
- Mapping sang label UI:
  - `CANCELLED_BY_REDISPATCH` => "Redispatch"
  - `CANCELLED_BY_OPERATOR` => "Operator cancelled"
  - `CANCELLED_BY_MEMBER` => "Member cancelled"

### 6.2 Loc/Thong ke

Nen them bo loc tren bang lich su:

- Cancelled by member
- Cancelled by operator
- Cancelled by redispatch

Neu can report KPI, dung reason code thay vi parse text.

## 7) Backward compatibility

- Method notify cancel tren service co tham so reason code optional.
- Neu call site cu khong truyen code, backend se coi nhu `CANCELLED_BY_MEMBER`.

## 8) Checklist test cho client teams

1. Redispatch:

- Tao 2 pending requests cung incident (A, B), dispatch lai cho B.
- A nhan `RequestCancelled` voi `ReasonCode = CANCELLED_BY_REDISPATCH`.

1. Operator cancel:

- Operator huy request pending.
- Rescuer nhan `RequestCancelled` voi `ReasonCode = CANCELLED_BY_OPERATOR`.

1. Member cancel incident:

- Member huy incident co pending requests.
- Tung rescuer pending nhan `RequestCancelled` voi `ReasonCode = CANCELLED_BY_MEMBER`.

1. UI history:

- Operator FE hien thi dung nhan reason theo code.

1. Fallback:

- Gia lap payload khong co `ReasonCode`, app van hien thi duoc message mac dinh.

## 9) Luu y nghiep vu

Khong tao state moi `Taken` cho dispatch request.
Ly do "bi lay don do redispatch" duoc bieu dien bang:

- `Status = Cancelled`
- `DeclineReason = CANCELLED_BY_REDISPATCH`

Cach nay giu state machine gon, nhung van du semantic cho FE va analytics.
