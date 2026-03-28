#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"

echo ""
echo "=== GET /api/consultations/{id}/reviews ==="
CONSUL_ID=$(GET_RAW "/api/users/me/consultations?pageNumber=1&pageSize=1" | python -c "import sys,json; items=json.load(sys.stdin)['data']['items']; print(items[0]['consultationId'] if items else 'NONE')" 2>/dev/null)
echo "ConsultationId: $CONSUL_ID"
if [ "$CONSUL_ID" != "NONE" ]; then
  GET "/api/consultations/$CONSUL_ID/reviews"
else
  echo "No consultations found to test review endpoint"
fi
