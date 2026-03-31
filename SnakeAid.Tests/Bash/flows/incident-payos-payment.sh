#!/bin/bash
# Flow: Full PayOS payment for snakebite incident
# Validates: Req 1.1, 1.2, 6.3, 8.1
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"

FAKE_INCIDENT_ID="00000000-0000-0000-0000-000000000001"

echo "==============================="
echo "  INCIDENT — PayOS Payment Flow"
echo "==============================="

echo ""
echo "--- Step 1: Create PayOS payment link ---"
RESPONSE=$(curl --globoff -s -X POST "${BASE_URL}/api/incidents/$FAKE_INCIDENT_ID/payment/payos" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"snakebiteIncidentId\": \"$FAKE_INCIDENT_ID\",
    \"amount\": 150000,
    \"description\": \"Test incident PayOS payment\",
    \"transactionType\": \"SnakebiteIncidentPayment\"
  }")

echo "$RESPONSE" | jq .

echo ""
echo "--- Step 2: Verify response fields ---"
STATUS=$(echo "$RESPONSE" | jq -r '.data.status // .status // "MISSING"')
CHECKOUT_URL=$(echo "$RESPONSE" | jq -r '.data.checkoutUrl // .checkoutUrl // "MISSING"')
TRANSACTION_ID=$(echo "$RESPONSE" | jq -r '.data.transactionId // .transactionId // "MISSING"')
ORDER_CODE=$(echo "$RESPONSE" | jq -r '.data.orderCode // .orderCode // "MISSING"')

echo "status:        $STATUS"
echo "checkoutUrl:   $CHECKOUT_URL"
echo "transactionId: $TRANSACTION_ID"
echo "orderCode:     $ORDER_CODE"

echo ""
PASS=true
[ "$STATUS" = "MISSING" ]        && echo "FAIL: status missing"        && PASS=false
[ "$CHECKOUT_URL" = "MISSING" ]  && echo "FAIL: checkoutUrl missing"   && PASS=false
[ "$TRANSACTION_ID" = "MISSING" ] && echo "FAIL: transactionId missing" && PASS=false
[ "$ORDER_CODE" = "MISSING" ]    && echo "FAIL: orderCode missing"     && PASS=false

if $PASS; then
  echo "RESULT: All expected fields present"
else
  echo "RESULT: Some fields missing (incident may not exist or not in Finished state)"
fi
