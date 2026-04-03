# Wallet Withdrawal Testing Guide

## Overview
This directory contains comprehensive bash-based integration tests for the wallet withdrawal feature, following the existing SnakeAid testing patterns.

## Test Structure

### Endpoint Tests (`endpoints/`)
- **`withdrawals.sh`** - User withdrawal endpoints testing
- **`admin-withdrawals.sh`** - Admin withdrawal management testing
- **`wallet.sh`** - Updated with bank directory endpoint testing

### Flow Tests (`flows/`)
- **`withdrawal-flow.sh`** - Complete end-to-end withdrawal journey testing

## Prerequisites

1. **Backend running**: Start SnakeAid.Api before running tests
   ```powershell
   dotnet run --project SnakeAid.Api
   ```

2. **Dependencies**: Ensure `jq` and `curl` are available
   ```powershell
   winget install jqlang.jq
   # curl is available in Git Bash
   ```

3. **Test data**: Tests assume default test users exist with sufficient wallet balances

## Running Tests

### Run All Tests
```powershell
& "C:\Program Files\Git\bin\bash.exe" SnakeAid.Tests/Bash/run-all.sh
```

### Run Specific Test Suites

#### User Withdrawal Endpoints
```powershell
& "C:\Program Files\Git\bin\bash.exe" SnakeAid.Tests/Bash/endpoints/withdrawals.sh
```

#### Admin Withdrawal Endpoints
```powershell
& "C:\Program Files\Git\bin\bash.exe" SnakeAid.Tests/Bash/endpoints/admin-withdrawals.sh
```

#### Complete Withdrawal Flow
```powershell
& "C:\Program Files\Git\bin\bash.exe" SnakeAid.Tests/Bash/endpoints/withdrawal-flow.sh
```

### Clear Token Cache
```powershell
& "C:\Program Files\Git\bin\bash.exe" -c "rm -f /tmp/snakeaid-token-*"
```

## Test Coverage

### User Withdrawal Tests (`withdrawals.sh`)
- ✅ Valid withdrawal creation
- ✅ Insufficient balance validation
- ✅ Bank account format validation
- ✅ Amount range validation (min/max)
- ✅ Withdrawal history retrieval
- ✅ Individual withdrawal details
- ✅ Withdrawal cancellation

### Admin Withdrawal Tests (`admin-withdrawals.sh`)
- ✅ Admin withdrawal listing
- ✅ Pending withdrawal filtering
- ✅ Withdrawal approval workflow
- ✅ Withdrawal rejection workflow
- ✅ Withdrawal completion workflow
- ✅ Role-based access control validation

### Bank Directory Tests (`wallet.sh`)
- ✅ Bank list retrieval
- ✅ Bank data validation

### End-to-End Flow Tests (`withdrawal-flow.sh`)
- ✅ Complete user-to-admin-to-completion workflow
- ✅ QR code generation verification
- ✅ Status transition validation
- ✅ Balance deduction verification

## Test Data

### Default Test Users
- **Member**: Default authenticated user with wallet balance
- **Admin**: Administrative user with approval permissions

### Bank Data
- Sacombank (970400) - Transfer supported
- VIB (970405) - Transfer supported
- BIDV (970418) - Transfer supported

### Windows Git Bash note
- On Windows Git Bash, JSON bodies containing Vietnamese Unicode text can bind unreliably for this endpoint.
- The bash withdrawal scripts intentionally use ASCII-safe bank labels such as `VCB` and `Sacombank`.
- This is a test harness constraint, not a backend API contract limitation. Direct API calls with UTF-8 JSON still work.

### Withdrawal Limits
- Minimum: 50,000 VND
- Maximum: 5,000,000 VND per request
- Daily limit: 10,000,000 VND per user

## Expected Behaviors

### Success Cases
- Valid withdrawals are created with `Pending` status
- Admin approval generates QR codes and changes status to `Approved`
- Admin completion deducts wallet balance and marks as `Completed`
- Users can cancel pending withdrawals
- Bank directory returns valid Vietnamese bank data

### Error Cases
- Insufficient balance → 400 Bad Request
- Invalid bank account format → 400 Bad Request
- Amount below minimum → 400 Bad Request
- Amount above maximum → 400 Bad Request
- Unauthorized admin access → 403 Forbidden
- Non-existent withdrawal → 404 Not Found
- Invalid status transitions → 409 Conflict

## Troubleshooting

### Common Issues

1. **Authentication failures**
   - Clear token cache: `rm -f /tmp/snakeaid-token-*`
   - Ensure backend is running and accessible

2. **Test data issues**
   - Ensure test users exist with sufficient balances
   - Check database migration has been applied

3. **Network issues**
   - Verify backend URL (default: http://localhost:5000)
   - Override if needed: `BASE_URL="http://localhost:8080" bash run-all.sh`

### Debug Mode
Add debug output by modifying test scripts to show raw responses:
```bash
# Instead of GET "/api/path"
curl -s -H "Authorization: Bearer $TOKEN" "$BASE_URL/api/path" | jq '.'
```

## Integration Notes

- Tests use the same authentication helpers as other endpoint tests
- Token caching prevents repeated logins (58-minute expiry)
- Tests assume clean database state; run migrations before testing
- Bank directory data is mocked for testing (VietQRHelper integration assumed working)
