#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/auth.sh" member

echo ""
echo "=== POST /api/wallet/topup ==="
curl --globoff -s -X POST "http://[::1]:8080/api/wallet/topup" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"amount": 50000}' | python -m json.tool 2>/dev/null
