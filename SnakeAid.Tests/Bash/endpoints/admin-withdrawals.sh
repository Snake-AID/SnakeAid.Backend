#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" all  # Get all tokens including admin
source "$SCRIPT_DIR/../helpers/http.sh"

echo ""
echo "=== ADMIN WITHDRAWAL ENDPOINTS ==="

# Switch to admin token for admin operations
TOKEN="$ADMIN_TOKEN"

echo ""
echo "=== GET /api/admin/withdrawals ==="
echo "Testing getting all withdrawals for admin..."
GET "/api/admin/withdrawals"

echo ""
echo "=== GET /api/admin/withdrawals/pending ==="
echo "Testing getting pending withdrawals..."
GET "/api/admin/withdrawals/pending"

# Get a pending withdrawal ID for testing
PENDING_WITHDRAWAL_ID=$(GET_RAW "/api/admin/withdrawals/pending" | jq -r '.[0].id // empty')

if [ -n "$PENDING_WITHDRAWAL_ID" ]; then
    echo ""
    echo "=== GET /api/admin/withdrawals/{id} ==="
    echo "Testing getting withdrawal details by ID..."
    GET "/api/admin/withdrawals/$PENDING_WITHDRAWAL_ID"

    echo ""
    echo "=== POST /api/admin/withdrawals/{id}/approve ==="
    echo "Testing approval of pending withdrawal..."
    POST_ACTION "/api/admin/withdrawals/$PENDING_WITHDRAWAL_ID/approve"

    echo ""
    echo "=== POST /api/admin/withdrawals/{id}/complete ==="
    echo "Testing completion of approved withdrawal..."
    POST_ACTION "/api/admin/withdrawals/$PENDING_WITHDRAWAL_ID/complete"

    echo ""
    echo "=== Verify final status ==="
    GET "/api/admin/withdrawals/$PENDING_WITHDRAWAL_ID"
else
    echo "No pending withdrawal found to test with"
fi

# Test rejection with another withdrawal
PENDING_WITHDRAWAL_ID2=$(GET_RAW "/api/admin/withdrawals/pending" | jq -r '.[0].id // empty')

if [ -n "$PENDING_WITHDRAWAL_ID2" ]; then
    echo ""
    echo "=== POST /api/admin/withdrawals/{id}/reject ==="
    echo "Testing rejection of pending withdrawal..."
    POST "/api/admin/withdrawals/$PENDING_WITHDRAWAL_ID2/reject" '{"reason": "Test rejection - insufficient verification"}'

    echo ""
    echo "=== Verify rejection ==="
    GET "/api/admin/withdrawals/$PENDING_WITHDRAWAL_ID2"
fi

echo ""
echo "=== Testing admin endpoints with member token (should fail) ==="
MEMBER_TOKEN_BACKUP="$TOKEN"
TOKEN="$MEMBER_TOKEN"

echo "Testing GET /api/admin/withdrawals with member token..."
GET "/api/admin/withdrawals" || echo "Expected: Access denied for member token"

# Restore admin token
TOKEN="$MEMBER_TOKEN_BACKUP"