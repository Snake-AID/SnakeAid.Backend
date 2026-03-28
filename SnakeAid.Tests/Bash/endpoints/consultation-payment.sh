#!/bin/bash
# Test Consultation payment endpoints (controller: api/consultations/payments)
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"

FAKE_BOOKING_ID="00000000-0000-0000-0000-000000000001"
FAKE_REQUEST_ID="00000000-0000-0000-0000-000000000002"
FAKE_TX_ID="00000000-0000-0000-0000-000000000003"

echo ""
echo "=== POST /api/consultations/scheduled/{bookingId}/payments — Pay scheduled booking ==="
POST "/api/consultations/scheduled/$FAKE_BOOKING_ID/payments" '{
  "paymentMethod": "PayOs"
}'

echo ""
echo "=== POST /api/consultations/instant/{requestId}/payments — Pay emergency request ==="
POST "/api/consultations/instant/$FAKE_REQUEST_ID/payments" '{
  "paymentMethod": "PayOs"
}'

echo ""
echo "=== POST /api/consultations/payments/confirm — Confirm consultation payment ==="
POST "/api/consultations/payments/confirm" "{
  \"transactionId\": \"$FAKE_TX_ID\"
}"
