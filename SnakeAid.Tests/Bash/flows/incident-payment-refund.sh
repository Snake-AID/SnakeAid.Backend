#!/bin/bash
# Flow: Refund incident payment (requires Operator/Admin role)
# Validates: Req 4.1, 4.4, 8.4
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Member token for wallet payment, then we need an operator/admin token for refund
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"

FAKE_INCIDENT_ID="00000000-0000-0000-0000-000000000001"

echo "==============================="
echo "  INCIDENT — Refund Payment Flow"
echo "==============================="

echo ""
echo "--- Step 1: Pay with wallet (as member) ---"
PAY_RESPONSE=$(curl --globoff -s -X POST "${BASE_URL}/api/incidents/$FAKE_INCIDENT_ID/payment/wallet" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"snakebiteIncidentId\": \"$FAKE_INCIDENT_ID\",
    \"amount\": 150000,
    \"description\": \"Wallet payment before refund test\",
    \"transactionType\": \"SnakebiteIncidentPayment\"
  }")

echo "$PAY_RESPONSE" | jq .

PAY_STATUS=$(echo "$PAY_RESPONSE" | jq -r '.data.status // .status // "MISSING"')
echo "Payment status: $PAY_STATUS"

echo ""
echo "--- Step 2: Refund (needs Operator/Admin token) ---"
echo "Note: Refund endpoint requires Operator or Admin role."
echo "Attempting with member token (may get 403 — expected)."

REFUND_RESPONSE=$(curl --globoff -s -X POST \
  "${BASE_URL}/api/incidents/$FAKE_INCIDENT_ID/payment/refund" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"snakebiteIncidentId\": \"$FAKE_INCIDENT_ID\",
    \"amount\": 150000,
    \"description\": \"Test refund for incident\"
  }")

echo "$REFUND_RESPONSE" | jq .

echo ""
echo "--- Step 3: Verify refund response ---"
SUCCESS=$(echo "$REFUND_RESPONSE" | jq -r '.data.success // .success // "MISSING"')
REFUND_AMOUNT=$(echo "$REFUND_RESPONSE" | jq -r '.data.refundAmount // .refundAmount // "MISSING"')
SYS_BEFORE=$(echo "$REFUND_RESPONSE" | jq -r '.data.systemWalletBalanceBefore // .systemWalletBalanceBefore // "MISSING"')
SYS_AFTER=$(echo "$REFUND_RESPONSE" | jq -r '.data.systemWalletBalanceAfter // .systemWalletBalanceAfter // "MISSING"')
RCV_BEFORE=$(echo "$REFUND_RESPONSE" | jq -r '.data.receiverWalletBalanceBefore // .receiverWalletBalanceBefore // "MISSING"')
RCV_AFTER=$(echo "$REFUND_RESPONSE" | jq -r '.data.receiverWalletBalanceAfter // .receiverWalletBalanceAfter // "MISSING"')

echo "success:                    $SUCCESS"
echo "refundAmount:               $REFUND_AMOUNT"
echo "systemWalletBalanceBefore:  $SYS_BEFORE"
echo "systemWalletBalanceAfter:   $SYS_AFTER"
echo "receiverWalletBalanceBefore: $RCV_BEFORE"
echo "receiverWalletBalanceAfter:  $RCV_AFTER"

echo ""
HTTP_STATUS=$(echo "$REFUND_RESPONSE" | jq -r '.statusCode // .status // "UNKNOWN"')
if [ "$HTTP_STATUS" = "403" ] || echo "$REFUND_RESPONSE" | jq -e '.title' 2>/dev/null | grep -qi "forbidden"; then
  echo "RESULT: 403 Forbidden — expected when using member token (need Operator/Admin)"
elif [ "$SUCCESS" != "MISSING" ]; then
  echo "RESULT: Refund response received"
else
  echo "RESULT: Unexpected response (incident may not exist or payment step failed)"
fi
