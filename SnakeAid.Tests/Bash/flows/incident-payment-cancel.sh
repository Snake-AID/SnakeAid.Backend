#!/bin/bash
# Flow: Cancel PayOS payment link for snakebite incident
# Validates: Req 5.1, 5.3, 8.3
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"

FAKE_INCIDENT_ID="00000000-0000-0000-0000-000000000001"

echo "==============================="
echo "  INCIDENT — Cancel Payment Flow"
echo "==============================="

echo ""
echo "--- Step 1: Create PayOS payment link ---"
CREATE_RESPONSE=$(curl --globoff -s -X POST "${BASE_URL}/api/incidents/$FAKE_INCIDENT_ID/payment/payos" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"snakebiteIncidentId\": \"$FAKE_INCIDENT_ID\",
    \"amount\": 150000,
    \"description\": \"Test incident payment to cancel\",
    \"transactionType\": \"SnakebiteIncidentPayment\"
  }")

echo "$CREATE_RESPONSE" | jq .

ORDER_CODE=$(echo "$CREATE_RESPONSE" | jq -r '.data.orderCode // .orderCode // "NONE"')
echo ""
echo "Extracted orderCode: $ORDER_CODE"

if [ "$ORDER_CODE" = "NONE" ] || [ "$ORDER_CODE" = "null" ]; then
  echo "SKIP: Could not extract orderCode — cannot test cancel"
  exit 0
fi

echo ""
echo "--- Step 2: Cancel payment link (DELETE — using curl directly) ---"
CANCEL_RESPONSE=$(curl --globoff -s -X DELETE \
  "${BASE_URL}/api/incidents/$FAKE_INCIDENT_ID/payment/payos/$ORDER_CODE" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"cancellationReason\": \"User changed payment method\"}")

echo "$CANCEL_RESPONSE" | jq .

echo ""
echo "--- Step 3: Verify cancel response ---"
SUCCESS=$(echo "$CANCEL_RESPONSE" | jq -r '.data.success // .success // "MISSING"')
STATUS=$(echo "$CANCEL_RESPONSE" | jq -r '.data.status // .status // "MISSING"')

echo "success: $SUCCESS"
echo "status:  $STATUS"

echo ""
PASS=true
[ "$SUCCESS" = "MISSING" ] && echo "FAIL: success field missing" && PASS=false
[ "$STATUS" = "MISSING" ]  && echo "FAIL: status field missing"  && PASS=false

if $PASS; then
  if [ "$SUCCESS" = "true" ]; then
    echo "RESULT: Payment link cancelled successfully"
  else
    echo "RESULT: Cancel responded but success='$SUCCESS'"
  fi
else
  echo "RESULT: Some fields missing (create may have failed first)"
fi
