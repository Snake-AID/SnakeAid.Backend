# Operator Cancel Snake Catching Request - Flow & Implementation Guide

## 📋 Overview

This document describes the complete flow of operator cancellation for snake catching requests in **Pending** and **Confirmed** status, including automatic deposit refund processing, and provides a Flutter implementation guide.

---

## 🔄 Flow Architecture

### High-Level Flow

```
┌─────────────────────────────────────────────────────────────┐
│ FLUTTER CLIENT (Operator)                                   │
│ - Authenticated as Operator/Admin                           │
│ - Selects reason for cancellation                           │
└────────────────────┬────────────────────────────────────────┘
                     │ PATCH /api/snakecatching/requests/operatorcancel/{requestId}
                     │ Body: { "reason": "..." }
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ BACKEND API (SnakeCatchingRequestController)               │
│ - Validates Authorization (Operator/Admin)                  │
│ - Validates Request Status (Pending/Confirmed only)         │
│ - Calls OperatorCancelSnakeCatchingRequestAsync()          │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ SERVICE LAYER (SnakeCatchingRequestService)                │
│                                                             │
│ 1. ExecuteInTransactionAsync():                            │
│    - Set Status = Cancelled                                │
│    - Set CancellationReason = reason                       │
│    - Update SnakeCatchingRequest                           │
│    - Return DetailSnakeCatchingRequestResponse             │
│                                                             │
│ 2. Check for deposit transactions:                         │
│    - Query: CatchingDeposit only                           │
│    - RefId = requestId, ExternalTransactionId != null      │
│                                                             │
│ 3. Compute refundable amount:                              │
│    - refundable = sum(deposit) - sum(already_refunded)     │
│                                                             │
│ 4. IF refundable > 0:                                      │
│    - Build RefundTransactionRequest                        │
│    - Call RefundSnakeCatchingTransactionAsync()            │
│    - Creates CatchingRefund Transaction                    │
│    - Credit User Wallet                                    │
│                                                             │
│ 5. Send Notification:                                      │
│    - Type: SNAKE_CATCHING_REQUEST_CANCELLED_BY_OPERATOR    │
│    - To: userId (customer), rescuer (if assigned)          │
│    - Content: Cancellation reason + deposit refund info    │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼ Response: 200 OK
┌─────────────────────────────────────────────────────────────┐
│ FLUTTER CLIENT                                              │
│ - Display: "Request cancelled successfully"                │
│ - Show: Refund status (if deposit was paid)                │
│ - Refresh request list                                     │
└─────────────────────────────────────────────────────────────┘
```

---

## 📊 Database State Changes

### Transaction Records Created/Updated

| Step | TransactionType | ReferenceId | Amount | Description |
|------|------------------|-------------|--------|-------------|
| 1 | CatchingDeposit | requestId | Y VND | Deposit payment (pre-existing) |
| 2 | CatchingRefund | requestId | Y VND | Refund transaction created |

### SnakeCatchingRequest Record

```
BEFORE:
  Status: Pending / Confirmed
  CancellationReason: null
  UpdatedAt: <previous_time>

AFTER:
  Status: Cancelled
  CancellationReason: "Operator specified reason"
  UpdatedAt: <current_utc_time>
```

### Wallet Record (User)

```
BEFORE:
  Balance: B VND

AFTER (if deposit refund processed):
  Balance: B + Y VND
  LastUpdatedAt: <current_utc_time>
```

---

## 🔐 Authorization & Validation

### Requirements
- **Auth Header**: Bearer token (JWT)
- **Role**: `Operator` or `Admin`
- **Request Status**: Only `Pending` and `Confirmed` status can be cancelled
- **Request Ownership**: Operator can cancel any request (not restricted by userId)

### Error Cases

| Condition | HTTP Status | Error Code |
|-----------|-------------|-----------|
| Unauthenticated | 401 | Unauthorized |
| Not Operator/Admin | 403 | Forbidden |
| Request not found | 404 | NotFound |
| Invalid status (not Pending/Confirmed) | 400 | BadRequest ("Cannot cancel request with status {status}. Only Pending and Confirmed requests can be cancelled by operator.") |
| Request already cancelled | 400 | BadRequest ("Cannot cancel already cancelled request") |
| Unknown error in refund | 200 | Success (cancellation succeeds, refund logged as error) |

---



---

## 🔍 API Contract

### Request

```http
PATCH /api/snakecatching/requests/operatorcancel/{requestId}
Authorization: Bearer <jwt_token>
Content-Type: application/json

{
  "reason": "Rescuer unavailable due to weather conditions"
}
```

### Response (Success - 200 OK)

```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "userId": "650e8400-e29b-41d4-a716-446655440001",
    "status": "Cancelled",
    "cancellationReason": "Rescuer unavailable due to weather conditions",
    "location": {
      "latitude": 10.8391267,
      "longitude": 106.8413534
    },
    "estimatedPrice": 50000,
    "distanceKm": 33.33,
    "createdAt": "2025-05-02T10:30:00Z",
    "updatedAt": "2025-05-02T11:45:00Z",
    "assignedRescuerId": null
  },
  "message": "Snake catching request cancelled by operator. Deposit will be refunded if any deposit was paid."
}
```

### Response (Error - 400 Bad Request - Invalid Status)

```json
{
  "success": false,
  "error": {
    "code": "BAD_REQUEST",
    "message": "Cannot cancel request with status Assigned. Only Pending and Confirmed requests can be cancelled by operator."
  }
}
```

### Response (Error - 404 Not Found)

```json
{
  "success": false,
  "error": {
    "code": "NOT_FOUND",
    "message": "Snake catching request not found"
  }
}
```

---

## ⚡ Key Implementation Notes

### Transaction Flow Details

1. **Status validation**: Only `Pending` and `Confirmed` requests can be cancelled by operator
   - `Pending`: Request created but not yet confirmed
   - `Confirmed`: Request confirmed but rescuer not yet assigned

2. **Deposit refund only**: Only `CatchingDeposit` transactions are refunded
   - No `CatchingPayment` refund in this flow
   - Deposit amount is displayed in notification

3. **Cancellation is atomic** (within transaction):
   - Status change to Cancelled
   - CancellationReason stored
   - All within one DB transaction

4. **Refund processing is independent**:
   - Happens AFTER cancellation succeeds
   - If refund fails, cancellation is NOT rolled back
   - Error is logged but success response returned

5. **Notification sent after everything**:
   - Customer receives push notification with deposit refund info
   - Rescuer receives notification if assigned (edge case)
   - Operator group receives real-time update

### Notification Details

#### Customer Notification:
- **Has Deposit**: "Yêu cầu bị hủy - Hoàn tiền" + "Yêu cầu #{code} đã bị hủy bởi điều phối viên. Tiền cọc {amount} VND sẽ được hoàn lại."
- **No Deposit**: "Yêu cầu bị hủy" + "Yêu cầu #{code} đã bị hủy bởi điều phối viên."

#### Rescuer Notification (if assigned):
- "Nhiệm vụ bị hủy" + "Yêu cầu #{code} đã bị hủy bởi điều phối viên."


**Last Updated**: 2025-05-02  
**Status**: Active  
**Maintained By**: Backend Team
