#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"

echo ""
echo "=== GET /api/users/me/consultations (all) ==="
GET "/api/users/me/consultations?pageNumber=1&pageSize=5"

echo ""
echo "=== GET /api/users/me/consultations?status=Ongoing ==="
GET "/api/users/me/consultations?status=Ongoing&pageNumber=1&pageSize=5"

echo ""
echo "=== GET /api/users/me/consultations?status=Completed ==="
GET "/api/users/me/consultations?status=Completed"

echo ""
echo "=== GET /api/users/me/consultations?type=Emergency ==="
GET "/api/users/me/consultations?type=Emergency"
