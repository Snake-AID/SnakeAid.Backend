#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" all  # Need both member and admin tokens
source "$SCRIPT_DIR/../helpers/http.sh"

echo ""
echo "=== COMPLETE WITHDRAWAL FLOW TEST ==="
echo "Testing end-to-end withdrawal process: Create → Approve → Complete"

# Step 1: Member creates a withdrawal request
echo ""
echo "=== STEP 1: Member creates withdrawal request ==="
TOKEN="$MEMBER_TOKEN"
CREATE_RESPONSE=$(POST_RAW "/api/withdrawals/create" '{
  "amount": 100000,
  "bankAccount": "1234567890",
  "bankName": "Ngân hàng TMCP Sài Gòn Thương Tín (Sacombank)",
  "bankBin": "970400"
}')

echo "Create response:"
echo "$CREATE_RESPONSE" | jq '.'

WITHDRAWAL_ID=$(echo "$CREATE_RESPONSE" | jq -r '.id')
echo "Withdrawal ID: $WITHDRAWAL_ID"

if [ -z "$WITHDRAWAL_ID" ] || [ "$WITHDRAWAL_ID" = "null" ]; then
    echo "Failed to create withdrawal - exiting test"
    exit 1
fi

# Step 2: Verify member can see their withdrawal
echo ""
echo "=== STEP 2: Member verifies withdrawal in history ==="
GET "/api/withdrawals/me"

# Step 3: Admin reviews pending withdrawals
echo ""
echo "=== STEP 3: Admin reviews pending withdrawals ==="
TOKEN="$ADMIN_TOKEN"
echo "Admin sees pending withdrawals:"
GET "/api/admin/withdrawals/pending"

# Step 4: Admin approves the withdrawal
echo ""
echo "=== STEP 4: Admin approves withdrawal ==="
APPROVE_RESPONSE=$(POST_ACTION_RAW "/api/admin/withdrawals/$WITHDRAWAL_ID/approve")
echo "Approve response:"
echo "$APPROVE_RESPONSE" | jq '.'

# Step 5: Verify approval generated QR code
echo ""
echo "=== STEP 5: Verify QR code generation ==="
DETAIL_RESPONSE=$(GET_RAW "/api/admin/withdrawals/$WITHDRAWAL_ID")
echo "Withdrawal details after approval:"
echo "$DETAIL_RESPONSE" | jq '.'

QR_PAYLOAD=$(echo "$DETAIL_RESPONSE" | jq -r '.vietQrPayload')
QR_IMAGE=$(echo "$DETAIL_RESPONSE" | jq -r '.vietQrImageBase64')

if [ -n "$QR_PAYLOAD" ] && [ "$QR_PAYLOAD" != "null" ]; then
    echo "✅ QR payload generated successfully"
else
    echo "❌ QR payload not generated"
fi

if [ -n "$QR_IMAGE" ] && [ "$QR_IMAGE" != "null" ]; then
    echo "✅ QR image generated successfully"
else
    echo "❌ QR image not generated"
fi

# Step 6: Admin completes the withdrawal (simulating successful bank transfer)
echo ""
echo "=== STEP 6: Admin completes withdrawal (simulates successful transfer) ==="
COMPLETE_RESPONSE=$(POST_ACTION_RAW "/api/admin/withdrawals/$WITHDRAWAL_ID/complete")
echo "Complete response:"
echo "$COMPLETE_RESPONSE" | jq '.'

# Step 7: Verify final status
echo ""
echo "=== STEP 7: Verify final completed status ==="
FINAL_DETAIL=$(GET_RAW "/api/admin/withdrawals/$WITHDRAWAL_ID")
echo "Final withdrawal details:"
echo "$FINAL_DETAIL" | jq '.'

FINAL_STATUS=$(echo "$FINAL_DETAIL" | jq -r '.status')
if [ "$FINAL_STATUS" = "Completed" ]; then
    echo "✅ Withdrawal flow completed successfully!"
else
    echo "❌ Withdrawal flow failed - final status: $FINAL_STATUS"
fi

# Step 8: Member verifies completion
echo ""
echo "=== STEP 8: Member verifies completion ==="
TOKEN="$MEMBER_TOKEN"
MEMBER_DETAIL=$(GET_RAW "/api/withdrawals/$WITHDRAWAL_ID")
echo "Member sees completed withdrawal:"
echo "$MEMBER_DETAIL" | jq '.'

echo ""
echo "=== WITHDRAWAL FLOW TEST COMPLETE ==="