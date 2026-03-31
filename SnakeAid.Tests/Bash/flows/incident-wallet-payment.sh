#!/bin/bash
# Flow: Full wallet payment for snakebite incident
# Validates: Req 2.2, 6.2, 8.2
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"

FAKE_INCIDENT_ID="00000000-0000-0000-0000-000000000001"

echo "==============================="
echo "  INCIDENT — Wallet Payment Flow"
echo "==============================="

echo ""
echo "--- Step 1: Pay with wallet ---"
RESPONSE=$(curl --globoff -s -X POST "${BASE_URL}/api/incidents/$FAKE_INCIDENT_ID/payment/wallet" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"snakebiteIncidentId\": \"$FAKE_INCIDENT_ID\",
    \"amount\": 150000,
    \"description\": \"Test incident wallet payment\",
    \"transactionType\": \"SnakebiteIncidentPayment\"
  }")

echo "$RESPONSE" | jq .

echo ""
echo "--- Step 2: Verify response fields ---"
STATUS=$(echo "$RESPONSE" | jq -r '.data.status // .status // "MISSING"')
USER_BALANCE=$(echo "$RESPONSE" | jq -r '.data.userWalletBalanceAfter // .userWalletBalanceAfter // "MISSING"')
SYSTEM_BALANCE=$(echo "$RESPONSE" | jq -r '.data.systemWalletBalanceAfter // .systemWalletBalanceAfter // "MISSING"')

echo "status:                  $STATUS"
echo "userWalletBalanceAfter:  $USER_BALANCE"
echo "systemWalletBalanceAfter: $SYSTEM_BALANCE"

echo ""
PASS=true
[ "$STATUS" = "MISSING" ]         && echo "FAIL: status missing"                  && PASS=false
[ "$USER_BALANCE" = "MISSING" ]   && echo "FAIL: userWalletBalanceAfter missing"   && PASS=false
[ "$SYSTEM_BALANCE" = "MISSING" ] && echo "FAIL: systemWalletBalanceAfter missing" && PASS=false

if $PASS; then
  if [ "$STATUS" = "Escrowed" ]; then
    echo "RESULT: Wallet payment escrowed successfully"
  else
    echo "RESULT: Fields present but status='$STATUS' (expected 'Escrowed')"
  fi
else
  echo "RESULT: Some fields missing (incident may not exist or not in Finished state)"
fi
