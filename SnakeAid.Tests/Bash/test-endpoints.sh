#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/auth.sh" member

echo ""
echo "=== TEST 1: GET /api/users/me/consultations (all) ==="
curl --globoff -s -X GET "http://[::1]:8080/api/users/me/consultations?pageNumber=1&pageSize=5" \
  -H "Authorization: Bearer $TOKEN" | python -m json.tool 2>/dev/null

echo ""
echo "=== TEST 2: GET /api/users/me/consultations?status=Ongoing ==="
curl --globoff -s -X GET "http://[::1]:8080/api/users/me/consultations?status=Ongoing&pageNumber=1&pageSize=5" \
  -H "Authorization: Bearer $TOKEN" | python -m json.tool 2>/dev/null

echo ""
echo "=== TEST 3: POST /api/wallet/topup ==="
curl --globoff -s -X POST "http://[::1]:8080/api/wallet/topup" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"amount": 50000}' | python -m json.tool 2>/dev/null
