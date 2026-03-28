#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/auth.sh" member

echo ""
echo "=== TEST 1: GET /api/users/me/consultations (all) ==="
curl --globoff -s -X GET "http://[::1]:8080/api/users/me/consultations?pageNumber=1&pageSize=5" \
  -H "Authorization: Bearer $TOKEN" | python -m json.tool 2>/dev/null

echo ""
echo "=== TEST 2: GET /api/users/me/consultations?status=Completed ==="
curl --globoff -s -X GET "http://[::1]:8080/api/users/me/consultations?status=Completed" \
  -H "Authorization: Bearer $TOKEN" | python -m json.tool 2>/dev/null

echo ""
echo "=== TEST 3: GET /api/users/me/consultations?type=Emergency ==="
curl --globoff -s -X GET "http://[::1]:8080/api/users/me/consultations?type=Emergency" \
  -H "Authorization: Bearer $TOKEN" | python -m json.tool 2>/dev/null

echo ""
echo "=== TEST 4: POST /api/wallet/topup ==="
curl --globoff -s -X POST "http://[::1]:8080/api/wallet/topup" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"amount": 50000}' | python -m json.tool 2>/dev/null

echo ""
echo "=== TEST 5: GET /api/consultations/{id}/reviews ==="
CONSUL_ID=$(curl --globoff -s -X GET "http://[::1]:8080/api/users/me/consultations?pageNumber=1&pageSize=1" \
  -H "Authorization: Bearer $TOKEN" | python -c "import sys,json; items=json.load(sys.stdin)['data']['items']; print(items[0]['consultationId'] if items else 'NONE')" 2>/dev/null)
echo "ConsultationId: $CONSUL_ID"
if [ "$CONSUL_ID" != "NONE" ]; then
  curl --globoff -s -X GET "http://[::1]:8080/api/consultations/$CONSUL_ID/reviews" \
    -H "Authorization: Bearer $TOKEN" | python -m json.tool 2>/dev/null
else
  echo "No consultations found to test review endpoint"
fi

echo ""
echo "=== TEST 6: GET /api/snake-species/search (ILIKE) ==="
curl --globoff -s "http://[::1]:8080/api/snake-species/search?q=ophiophagus" | python -c "import sys,json; d=json.load(sys.stdin); print(f\"  status={d['status_code']}, results={len(d['data'])}\")" 2>/dev/null
