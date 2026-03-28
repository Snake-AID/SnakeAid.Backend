#!/bin/bash
# Test SnakeCatching payment endpoints (new controller: api/snakecatching/payment)
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"

# Dùng một GUID giả để test — sẽ trả 400/500 nếu request không hợp lệ,
# nhưng đủ để verify endpoint reachable + auth + routing đúng.
FAKE_REQUEST_ID="00000000-0000-0000-0000-000000000001"

echo ""
echo "=== POST /api/snakecatching/payment/create-link ==="
POST "/api/snakecatching/payment/create-link" "{
  \"snakeCatchingRequestId\": \"$FAKE_REQUEST_ID\",
  \"amount\": 100000,
  \"description\": \"Test payment\",
  \"transactionType\": \"CatchingPayment\"
}"

echo ""
echo "=== POST /api/snakecatching/payment/cancel-link/999999 ==="
POST "/api/snakecatching/payment/cancel-link/999999" '{
  "cancellationReason": "Test cancel"
}'

echo ""
echo "=== POST /api/snakecatching/payment/transfer-to-rescuer ==="
POST "/api/snakecatching/payment/transfer-to-rescuer" "{
  \"snakeCatchingRequestId\": \"$FAKE_REQUEST_ID\"
}"
