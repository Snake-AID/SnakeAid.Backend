#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"

echo "==============================="
echo "  MEMBER FLOW"
echo "==============================="

echo ""
echo "--- Consultations (all) ---"
GET "/api/users/me/consultations?pageNumber=1&pageSize=5"

echo ""
echo "--- Wallet top-up ---"
POST "/api/wallet/topup" '{"amount": 50000}'

echo ""
echo "--- Consultation review ---"
CONSUL_ID=$(GET_RAW "/api/users/me/consultations?pageNumber=1&pageSize=1" | jq -r '.data.items[0].consultationId // "NONE"')
if [ "$CONSUL_ID" != "NONE" ]; then
  GET "/api/consultations/$CONSUL_ID/reviews"
else
  echo "No consultations found"
fi

echo ""
echo "--- Snake search ---"
GET_PUBLIC_RAW "/api/snake-species/search?q=ophiophagus" | jq '{status: .status_code, results: (.data | length)}'
