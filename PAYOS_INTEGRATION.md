# PayOS Payment Integration for Snake Catching Service

## Tổng quan

Payment flow cho snake catching service đã được triển khai sử dụng PayOS gateway. Flow này tuân thủ yêu cầu:

- ✅ **KHÔNG thay đổi** entity `Transaction`
- ✅ **KHÔNG thêm field** vào database
- ✅ Sử dụng `ReferenceId` cho `SnakeCatchingRequestId`
- ✅ Sử dụng `TransactionType` cho loại giao dịch
- ✅ Sử dụng `Description` để lưu `orderCode` (có thể parse lại)
- ✅ Sử dụng `ExternalTransactionId` cho PayOS transaction reference
- ✅ Trả về thông tin PayOS (checkoutUrl, orderCode, etc.) trong Response DTO

## Cấu trúc Files

### 1. Settings & Configuration
- `SnakeAid.Core/Settings/PayOsOptions.cs` - PayOS settings
- `appsettings.json` - PayOS configuration (ClientId, ApiKey, ChecksumKey, URLs)
- `Program.cs` - Configure PayOsOptions từ configuration

### 2. Models
- `SnakeAid.Service/Services/PayOs/Models/`
  - `PayOsItemPayload.cs`
  - `PayOsLinkCreateContext.cs`
  - `PayOsLinkCreated.cs`
  - `PayOsLinkInformation.cs`
  - `PayOsWebhookData.cs`

### 3. Interfaces
- `SnakeAid.Service/Interfaces/IPayOsClient.cs` - PayOS SDK wrapper
- `SnakeAid.Service/Interfaces/IPayOsPaymentService.cs` - Payment service interface

### 4. Implementations
- `SnakeAid.Service/Services/PayOs/PayOsClient.cs` - Wrapper for Net.payOS SDK
- `SnakeAid.Service/Services/PayOs/PayOsPaymentService.cs` - Business logic

### 5. DTOs
**Requests:**
- `SnakeAid.Core/Requests/PayOs/CreateSnakeCatchingPaymentRequest.cs`
- `SnakeAid.Core/Requests/PayOs/CancelPaymentLinkRequest.cs`
- `SnakeAid.Core/Requests/PayOs/ConfirmPaymentRequest.cs`

**Responses:**
- `SnakeAid.Core/Responses/PayOs/SnakeCatchingPaymentResponse.cs`
- `SnakeAid.Core/Responses/PayOs/CancelPaymentLinkResponse.cs`
- `SnakeAid.Core/Responses/PayOs/PayOsWebhookResponse.cs`

### 6. Controller
- `SnakeAid.Api/Controllers/PayOsController.cs` - REST API endpoints

## Payment Flow

### 1. Create Payment Link

**Endpoint:** `POST /api/v1/PayOs/create-payment-link`

**Request:**
```json
{
  "snakeCatchingRequestId": "guid",
  "senderId": "guid",        // Customer/Requester
  "receiverId": "guid",      // Catcher/Rescuer
  "amount": 500000,
  "description": "Payment for snake catching service"
}
```

**Response:**
```json
{
  "success": true,
  "message": "PayOS payment link created successfully",
  "data": {
    "transactionId": "guid",
    "snakeCatchingRequestId": "guid",
    "amount": 500000,
    "status": "Pending",
    "checkoutUrl": "https://pay.payos.vn/...",
    "orderCode": 1707728123456,
    "paymentLinkId": "...",
    "expiresAt": null,
    "provider": "PayOS",
    "gatewayRawResponse": {
      "orderCode": 1707728123456,
      "paymentLinkId": "...",
      "checkoutUrl": "...",
      "amount": 500000,
      "status": "PENDING",
      "currency": "VND"
    }
  }
}
```

**Quá trình:**
1. Validate `SnakeCatchingRequest` exists và status = `Assigned` hoặc `Finished`
2. Validate sender và receiver users exist
3. Generate unique `orderCode` (timestamp + random)
4. Tạo `Transaction` record:
   - `UserId` = `senderId`
   - `ReferenceId` = `SnakeCatchingRequestId`
   - `TransactionType` = `CatchingPayment`
   - `Description` = `"SNAKEAID-{orderCode} - Snake catching payment"`
   - `PaymentMethod` = `"PayOS"`
5. Call PayOS API để tạo payment link
6. Trả về response với `checkoutUrl` để redirect user

### 2. Payment Webhook (Automatic)

**Endpoint:** `POST /api/v1/PayOs/webhook` (AllowAnonymous)

**Khi payment thành công:**
1. PayOS gọi webhook với payload signature
2. Verify webhook signature
3. Parse `orderCode` từ payload
4. Tìm `Transaction` bằng cách match `Description` prefix: `"SNAKEAID-{orderCode}"`
5. Update `Transaction`:
   - `ExternalTransactionId` = PayOS transaction reference
   - `CreatedAt` = transaction datetime from PayOS
6. Update `SnakeCatchingRequest.Status` = `Paid`
7. **Tạo Payout Transaction:**
   - `UserId` = `receiver` (from `AssignedRescuerId`)
   - `ReferenceId` = same `SnakeCatchingRequestId`
   - `TransactionType` = `CatcherPayout`
   - `Amount` = same amount (hoặc trừ commission nếu cần)

**Response:**
```json
{
  "success": true,
  "message": "Payment processed successfully",
  "data": {
    "success": true,
    "transactionId": "guid",
    "payoutTransactionId": "guid",
    "orderCode": 1707728123456,
    "amount": 500000,
    "transactionReference": "FT21070110145632",
    "transactionDateTime": "2024-02-12T10:30:00Z"
  }
}
```

### 3. Manual Confirm (Fallback)

**Endpoint:** `POST /api/v1/PayOs/confirm-payment`

**Request:**
```json
{
  "transactionId": "guid"
}
```

Sử dụng khi webhook không được gọi (network issues, etc.). Controller sẽ:
1. Get payment info từ PayOS bằng `orderCode`
2. Verify payment đã được paid
3. Process giống như webhook flow

### 4. Cancel Payment Link

**Endpoint:** `POST /api/v1/PayOs/cancel-payment-link/{orderCode}`

**Request:**
```json
{
  "cancellationReason": "Customer cancelled"
}
```

Hủy payment link nếu chưa thanh toán.

## Transaction Records

### Flow thành công tạo 2 Transactions:

#### 1. Payment Transaction (sender trả tiền)
```
Id: new Guid
UserId: senderId (customer)
ReferenceId: SnakeCatchingRequestId
Amount: 500000
Currency: "VND"
TransactionType: CatchingPayment (20)
Description: "SNAKEAID-1707728123456 - Snake catching payment"
PaymentMethod: "PayOS"
ExternalTransactionId: "FT21070110145632" (PayOS transaction reference)
CreatedAt: 2024-02-12T10:30:00Z
```

#### 2. Payout Transaction (receiver nhận tiền)
```
Id: new Guid
UserId: receiverId (catcher/rescuer)
ReferenceId: SnakeCatchingRequestId (same)
Amount: 500000
Currency: "VND"
TransactionType: CatcherPayout (21)
Description: "Payout for snake catching request {id}"
PaymentMethod: "PayOS"
ExternalTransactionId: "FT21070110145632" (same reference)
CreatedAt: 2024-02-12T10:30:00Z
```

## Tích hợp Frontend

### 1. Tạo payment và redirect user
```typescript
async function createPayment(requestId: string, senderId: string, receiverId: string, amount: number) {
  const response = await fetch('/api/v1/PayOs/create-payment-link', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      snakeCatchingRequestId: requestId,
      senderId: senderId,
      receiverId: receiverId,
      amount: amount,
      description: 'Snake catching service payment'
    })
  });

  const result = await response.json();
  
  if (result.success) {
    // Redirect user to PayOS checkout page
    window.location.href = result.data.checkoutUrl;
  }
}
```

### 2. Xử lý return URL
Khi user thanh toán xong, PayOS redirect về `ReturnUrl` được config trong `appsettings.json`.
Frontend cần:
1. Show loading state
2. Poll API để check payment status
3. Hoặc chờ webhook update database rồi query status

```typescript
async function checkPaymentStatus(transactionId: string) {
  // Poll every 2 seconds
  const interval = setInterval(async () => {
    const response = await fetch(`/api/v1/transactions/${transactionId}`);
    const transaction = await response.json();
    
    if (transaction.externalTransactionId) {
      clearInterval(interval);
      // Payment success
      showSuccessMessage();
      redirectToSuccessPage();
    }
  }, 2000);
}
```

## Testing

### 1. Test với PayOS Sandbox
- Sử dụng test credentials trong `appsettings.json`
- PayOS cung cấp test cards và bank accounts

### 2. Test Webhook Locally
Sử dụng ngrok để expose local webhook endpoint:
```bash
ngrok http 5001
```
Update `WebhookConfirmUrl` trong appsettings.json với ngrok URL.

### 3. Manual Confirm Testing
Nếu không thể test webhook, sử dụng manual confirm endpoint với `transactionId`.

## Security Notes

1. **Webhook Signature Verification**: PayOS webhook được verify bằng signature
2. **HTTPS Only**: Production phải dùng HTTPS cho webhook URL
3. **API Authentication**: Tất cả endpoints (trừ webhook) require authentication
4. **Credentials**: Store PayOS credentials trong environment variables, không commit vào git

## Troubleshooting

### Payment Link không tạo được
- Check PayOS credentials trong appsettings.json
- Verify `SnakeCatchingRequest` status = `Assigned` hoặc `Finished`
- Check sender và receiver users exist

### Webhook không được gọi
- Verify `WebhookConfirmUrl` accessible từ internet (dùng ngrok cho local)
- Check PayOS dashboard cho webhook logs
- Sử dụng manual confirm endpoint như fallback

### Transaction không được tạo
- Check database connection
- Verify `Transaction` entity và `TransactionType` enum correct

## Migration & Deployment

**✅ KHÔNG CẦN MIGRATION** - Solution này không thay đổi database schema.

**Deployment Steps:**
1. Update `appsettings.json` với production PayOS credentials
2. Update URLs (ReturnUrl, CancelUrl, WebhookConfirmUrl) với production domain
3. Deploy application
4. Verify webhook endpoint accessible từ PayOS servers
5. Test payment flow end-to-end

## Future Enhancements

1. **Commission Calculation**: Tính platform fee khi tạo payout transaction
2. **Webhook Retry**: Implement retry logic cho webhook failures
3. **Payment Expiry**: Set expiry time cho payment links
4. **Refund Flow**: Implement refund với `TransactionType.CatchingRefund`
5. **Payment History**: Dashboard để xem payment history
6. **Analytics**: Track payment success rate, failure reasons, etc.
