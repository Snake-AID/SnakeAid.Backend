#!/bin/bash
# Test PayOS shared callback endpoints (controller: api/v1/payos)
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"

FAKE_TX_ID="00000000-0000-0000-0000-000000000001"

echo ""
echo "=== POST /api/v1/payos/confirm-payment (auth required) ==="
POST "/api/v1/payos/confirm-payment" "{
  \"transactionId\": \"$FAKE_TX_ID\"
}"

echo ""
echo "=== POST /api/v1/payos/confirm-payment — empty transactionId ==="
POST "/api/v1/payos/confirm-payment" '{
  "transactionId": "00000000-0000-0000-0000-000000000000"
}'

echo ""
echo "=== GET /api/v1/payos/return (public, simulates PayOS redirect) ==="
GET_PUBLIC "/api/v1/payos/return?code=00&id=test123&cancel=false&status=PAID&orderCode=999999"

echo ""
echo "=== GET /api/v1/payos/cancel (public, simulates PayOS cancel redirect) ==="
GET_PUBLIC "/api/v1/payos/cancel?code=01&id=test123&cancel=true&status=CANCELLED&orderCode=999999"

echo ""
echo "=== POST /api/v1/payos/webhook (public, no auth — will fail verify but tests routing) ==="
curl --globoff -s -X POST "${BASE_URL}/api/v1/payos/webhook" \
  -H "Content-Type: application/json" \
  -d '{"data":{"description":"CATCHING-999999"}}' | jq .
