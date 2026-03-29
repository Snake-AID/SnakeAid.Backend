#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"

echo ""
echo "=== POST /api/wallet/topup ==="
POST "/api/wallet/topup" '{"amount": 50000}'
