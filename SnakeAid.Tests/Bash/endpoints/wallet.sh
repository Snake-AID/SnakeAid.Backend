#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"

echo ""
echo "=== WALLET ENDPOINTS ==="

echo ""
echo "=== POST /api/wallet/topup ==="
POST "/api/wallet/topup" '{"amount": 50000}'

echo ""
echo "=== GET /api/wallet/banks ==="
echo "Testing bank directory endpoint..."
GET "/api/wallet/banks"
