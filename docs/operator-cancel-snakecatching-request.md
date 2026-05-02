# Operator Cancel Snake Catching Request - Flow & Implementation Guide

## 📋 Overview

This document describes the complete flow of operator cancellation for snake catching requests, including automatic refund processing, and provides a Flutter implementation guide.

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
│ 2. Check for paid transactions:                            │
│    - Query: CatchingPayment or CatchingDeposit             │
│    - RefId = requestId, ExternalTransactionId != null      │
│                                                             │
│ 3. Compute refundable amount:                              │
│    - refundable = sum(paid) - sum(already_refunded)       │
│                                                             │
│ 4. IF refundable > 0:                                      │
│    - Build RefundTransactionRequest                        │
│    - Call RefundSnakeCatchingTransactionAsync()            │
│    - Creates CatchingRefund Transaction                    │
│    - Credit User Wallet                                    │
│                                                             │
│ 5. Send Notification:                                      │
│    - Type: REQUEST_CANCELLED                              │
│    - To: userId (request creator)                         │
│    - Content: Reason, cancellation details                │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼ Response: 200 OK
┌─────────────────────────────────────────────────────────────┐
│ FLUTTER CLIENT                                              │
│ - Display: "Request cancelled successfully"                │
│ - Show: Refund status (if applicable)                      │
│ - Refresh request list                                     │
└─────────────────────────────────────────────────────────────┘
```

---

## 📊 Database State Changes

### Transaction Records Created/Updated

| Step | TransactionType | ReferenceId | Amount | Description |
|------|------------------|-------------|--------|-------------|
| 1 | CatchingPayment | requestId | X VND | Original payment (pre-existing) |
| 2 | CatchingDeposit | requestId | Y VND | Deposit (if any) (pre-existing) |
| 3 | CatchingRefund | requestId | (X+Y) VND | Refund transaction created |

### SnakeCatchingRequest Record

```
BEFORE:
  Status: Assigned / Pending / Confirmed
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

AFTER (if refund processed):
  Balance: B + (X+Y) VND
  LastUpdatedAt: <current_utc_time>
```

---

## 🔐 Authorization & Validation

### Requirements
- **Auth Header**: Bearer token (JWT)
- **Role**: `Operator` or `Admin`
- **Request Ownership**: Operator can cancel any request (not restricted by userId)

### Error Cases

| Condition | HTTP Status | Error Code |
|-----------|-------------|-----------|
| Unauthenticated | 401 | Unauthorized |
| Not Operator/Admin | 403 | Forbidden |
| Request not found | 404 | NotFound |
| Request already cancelled | 400 | BadRequest ("Cannot cancel already cancelled request") |
| Mission in EnRoute status | 400 | BadRequest ("Cannot cancel: rescuer en route") |
| Unknown error in refund | 200 | Success (cancellation succeeds, refund logged as error) |

---

## 📱 Flutter Implementation Guide

### 1. Define Models

```dart
/// Request body for operator cancellation
class OperatorCancelRequest {
  final String reason;

  OperatorCancelRequest({
    required this.reason,
  });

  Map<String, dynamic> toJson() => {
    'reason': reason,
  };
}

/// Response from operator cancel endpoint
class OperatorCancelResponse {
  final DetailSnakeCatchingRequestResponse requestDetails;
  final String message;

  OperatorCancelResponse({
    required this.requestDetails,
    required this.message,
  });

  factory OperatorCancelResponse.fromJson(Map<String, dynamic> json) {
    return OperatorCancelResponse(
      requestDetails: DetailSnakeCatchingRequestResponse.fromJson(json['data']),
      message: json['message'] ?? 'Request cancelled',
    );
  }
}
```

### 2. Create API Service

```dart
import 'package:http/http.dart' as http;
import 'dart:convert';

class SnakeCatchingApiService {
  final String baseUrl = 'https://api.snakeaid.com';
  final String token; // JWT token from auth

  SnakeCatchingApiService({required this.token});

  /// Cancel a snake catching request as operator
  /// Returns the updated request details with refund status
  Future<OperatorCancelResponse> operatorCancelRequest({
    required String requestId,
    required String reason,
  }) async {
    final url = Uri.parse(
      '$baseUrl/api/snakecatching/requests/operatorcancel/$requestId',
    );

    final headers = {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $token',
    };

    final body = jsonEncode(
      OperatorCancelRequest(reason: reason).toJson(),
    );

    try {
      final response = await http.patch(
        url,
        headers: headers,
        body: body,
      ).timeout(
        const Duration(seconds: 30),
        onTimeout: () => throw TimeoutException('Request timeout'),
      );

      if (response.statusCode == 200) {
        final jsonResponse = jsonDecode(response.body);
        return OperatorCancelResponse.fromJson(jsonResponse);
      } else if (response.statusCode == 404) {
        throw NotFoundException('Snake catching request not found');
      } else if (response.statusCode == 403) {
        throw UnauthorizedException('Not authorized to cancel this request');
      } else if (response.statusCode == 400) {
        final error = jsonDecode(response.body);
        throw BadRequestException(error['message'] ?? 'Invalid request');
      } else {
        throw Exception('Failed to cancel request: ${response.statusCode}');
      }
    } on SocketException {
      throw NetworkException('Network error occurred');
    } on TimeoutException {
      throw TimeoutException('Request timed out');
    } catch (e) {
      rethrow;
    }
  }
}
```

### 3. Create ViewModel/Provider

```dart
import 'package:flutter/material.dart';

class OperatorCancelViewModel extends ChangeNotifier {
  final SnakeCatchingApiService apiService;

  bool _isLoading = false;
  String? _error;
  String? _successMessage;

  bool get isLoading => _isLoading;
  String? get error => _error;
  String? get successMessage => _successMessage;

  OperatorCancelViewModel({required this.apiService});

  Future<bool> cancelRequest({
    required String requestId,
    required String reason,
  }) async {
    _isLoading = true;
    _error = null;
    _successMessage = null;
    notifyListeners();

    try {
      final response = await apiService.operatorCancelRequest(
        requestId: requestId,
        reason: reason,
      );

      _successMessage = response.message;
      _isLoading = false;
      notifyListeners();
      return true;
    } on NotFoundException catch (e) {
      _error = 'Request not found: ${e.message}';
    } on UnauthorizedException catch (e) {
      _error = 'Not authorized: ${e.message}';
    } on BadRequestException catch (e) {
      _error = 'Invalid request: ${e.message}';
    } on NetworkException catch (e) {
      _error = 'Network error: ${e.message}';
    } on TimeoutException catch (e) {
      _error = 'Request timeout: ${e.message}';
    } catch (e) {
      _error = 'Error cancelling request: $e';
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }
}
```

### 4. Create UI Screen

```dart
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class OperatorCancelDialog extends StatefulWidget {
  final String requestId;
  final VoidCallback onSuccess;

  const OperatorCancelDialog({
    Key? key,
    required this.requestId,
    required this.onSuccess,
  }) : super(key: key);

  @override
  State<OperatorCancelDialog> createState() => _OperatorCancelDialogState();
}

class _OperatorCancelDialogState extends State<OperatorCancelDialog> {
  final reasonController = TextEditingController();

  @override
  void dispose() {
    reasonController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Cancel Snake Catching Request'),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Text(
              'Please provide a reason for cancellation:',
              style: TextStyle(color: Colors.grey),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: reasonController,
              maxLines: 3,
              minLines: 2,
              decoration: InputDecoration(
                hintText: 'e.g., Rescuer unavailable, Wrong location...',
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
                contentPadding: const EdgeInsets.all(12),
              ),
            ),
            const SizedBox(height: 12),
            const Text(
              '💰 Money will be refunded if any payment was created',
              style: TextStyle(
                fontSize: 12,
                color: Colors.green,
                fontStyle: FontStyle.italic,
              ),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Cancel'),
        ),
        Consumer<OperatorCancelViewModel>(
          builder: (context, viewModel, _) {
            return ElevatedButton(
              onPressed: viewModel.isLoading
                  ? null
                  : () => _handleConfirmCancel(context, viewModel),
              child: viewModel.isLoading
                  ? const SizedBox(
                      height: 20,
                      width: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text('Confirm Cancel'),
            );
          },
        ),
      ],
    );
  }

  void _handleConfirmCancel(
    BuildContext context,
    OperatorCancelViewModel viewModel,
  ) async {
    final reason = reasonController.text.trim();

    if (reason.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please provide a cancellation reason')),
      );
      return;
    }

    final success = await viewModel.cancelRequest(
      requestId: widget.requestId,
      reason: reason,
    );

    if (!mounted) return;

    if (success) {
      Navigator.pop(context);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(viewModel.successMessage ?? 'Request cancelled'),
          backgroundColor: Colors.green,
        ),
      );
      widget.onSuccess();
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(viewModel.error ?? 'Failed to cancel request'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }
}
```

### 5. Usage in Request List Screen

```dart
class SnakeCatchingRequestListScreen extends StatefulWidget {
  @override
  State<SnakeCatchingRequestListScreen> createState() =>
      _SnakeCatchingRequestListScreenState();
}

class _SnakeCatchingRequestListScreenState
    extends State<SnakeCatchingRequestListScreen> {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Snake Catching Requests'),
      ),
      body: ListView.builder(
        itemCount: requests.length,
        itemBuilder: (context, index) {
          final request = requests[index];
          return RequestCard(
            request: request,
            onCancelPressed: () {
              showDialog(
                context: context,
                builder: (context) => OperatorCancelDialog(
                  requestId: request.id,
                  onSuccess: () {
                    // Refresh request list
                    setState(() {
                      _refreshRequests();
                    });
                  },
                ),
              );
            },
          );
        },
      ),
    );
  }

  Future<void> _refreshRequests() async {
    // Fetch updated request list
  }
}
```

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
    "assignedRescuerId": "750e8400-e29b-41d4-a716-446655440002"
  },
  "message": "Snake catching request cancelled by operator. Money will be refunded if any payment was created."
}
```

### Response (Error - 400 Bad Request)

```json
{
  "success": false,
  "error": {
    "code": "INVALID_CANCELLATION",
    "message": "Cannot cancel request. The rescuer is already on the way (En Route)."
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

1. **Cancellation is atomic** (within transaction):
   - Status change to Cancelled
   - CancellationReason stored
   - All within one DB transaction

2. **Refund processing is independent**:
   - Happens AFTER cancellation succeeds
   - If refund fails, cancellation is NOT rolled back
   - Error is logged but success response returned

3. **Notification sent after everything**:
   - User receives notification: reason for cancellation
   - Notification includes refund information if applicable

### Error Handling Strategy

```dart
// Good practice in Flutter:
try {
  await viewModel.cancelRequest(requestId: id, reason: reason);
} on BadRequestException catch (e) {
  // Cannot cancel (e.g., mission en route)
  showErrorDialog('Cannot cancel: ${e.message}');
} on UnauthorizedException catch (e) {
  // Not an operator
  showErrorDialog('Not authorized');
} on NetworkException catch (e) {
  // Network issue
  showRetryDialog('Network error: ${e.message}');
}
```

### Refund Status Verification

After cancellation, check refund status from response:

```dart
bool wasRefundProcessed = response.requestDetails.status == 'Cancelled'
    && /* check transaction history for CatchingRefund */;
    
// Or check user wallet balance change
```

---

## 📋 Testing Checklist

### Backend
- [ ] Operator (role-based) can cancel any request
- [ ] Non-operator cannot call this endpoint
- [ ] Cancellation reason stored correctly
- [ ] Refund transaction created (if payment exists)
- [ ] Wallet balance updated
- [ ] Notification sent to user
- [ ] Request status transitions correctly
- [ ] Already-cancelled requests cannot be cancelled again

### Flutter
- [ ] Dialog appears with reason field
- [ ] Submitting empty reason shows error
- [ ] Loading spinner appears during request
- [ ] Success message displays
- [ ] Request list refreshes
- [ ] Error messages display appropriately
- [ ] Network timeout handled
- [ ] Token expiration handled (401)

---

## 📞 Support & Debugging

### Common Issues

| Issue | Solution |
|-------|----------|
| 403 Forbidden | User role is not Operator/Admin |
| 404 Not Found | requestId is invalid/doesn't exist |
| 400 Can't cancel | Request in EnRoute status or already cancelled |
| Refund not applied | Check transaction history in database |
| No notification | Check NotificationQueueService logs |

### Logs to Check (Backend)

```
// Success case
"Operator cancelled snake catching request. RequestId: {id}, UserId: {uid}, Reason: {reason}"
"Operator cancel: refund processed for Request {id}, UserId: {uid}, RefundAmount: {amt}"

// Error case
"Failed to process refund during operator cancel for Request {id}"
"Error in operator cancel for snake catching request: {message}"
```

---

## 🔗 Related Documentation

- [Wallet & Transaction Flow](./wallet-transaction-flow.md)
- [Snake Catching Request API](./snake-catching-request-api.md)
- [Payment & Refund System](./payment-refund-system.md)
- [Notification System](./notification-system.md)

---

**Last Updated**: 2025-05-02  
**Status**: Active  
**Maintained By**: Backend Team
