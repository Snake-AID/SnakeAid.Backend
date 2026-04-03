#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"

echo ""
echo "=== WALLET WITHDRAWAL ENDPOINTS ==="

echo ""
echo "=== POST /api/withdrawals/create ==="
echo "Testing withdrawal creation with valid data..."
POST "/api/withdrawals/create" '{
  "amount": 50000,
  "bankAccount": "1020951024",
  "bankName": "Ngân hàng TMCP Ngoại Thương Việt Nam",
  "accountHolderName": "NGUYEN VAN DUY KHIEM",
  "bankBin": "970436"
}'

echo ""
echo "=== Testing withdrawal creation with insufficient balance ==="
POST "/api/withdrawals/create" '{
  "amount": 999999999,
  "bankAccount": "1234567890",
  "bankName": "Ngân hàng TMCP Sài Gòn Thương Tín (Sacombank)",
  "accountHolderName": "NGUYEN VAN DUY KHIEM",
  "bankBin": "970400"
}'

echo ""
echo "=== GET /api/withdrawals/me ==="
echo "Testing getting user's withdrawal history..."
GET "/api/withdrawals/me"

# Get the first withdrawal ID from the list for further testing
WITHDRAWAL_ID=$(GET_RAW "/api/withdrawals/me" | jq -r '.data[0].id // empty')

if [ -n "$WITHDRAWAL_ID" ]; then
    echo ""
    echo "=== GET /api/withdrawals/{id} ==="
    echo "Testing getting specific withdrawal details..."
    GET "/api/withdrawals/$WITHDRAWAL_ID"

    echo ""
    echo "=== POST /api/withdrawals/{id}/cancel ==="
    echo "Testing cancellation of pending withdrawal..."
    POST_ACTION "/api/withdrawals/$WITHDRAWAL_ID/cancel"
else
    echo "No withdrawal found to test with"
fi

echo ""
echo "=== Testing withdrawal with invalid bank account ==="
POST "/api/withdrawals/create" '{
  "amount": 100000,
  "bankAccount": "123",
  "bankName": "Ngân hàng TMCP Sài Gòn Thương Tín (Sacombank)",
  "accountHolderName": "NGUYEN VAN DUY KHIEM",
  "bankBin": "970400"
}'

echo ""
echo "=== Testing withdrawal with amount below minimum ==="
POST "/api/withdrawals/create" '{
  "amount": 10000,
  "bankAccount": "1234567890",
  "bankName": "Ngân hàng TMCP Sài Gòn Thương Tín (Sacombank)",
  "accountHolderName": "NGUYEN VAN DUY KHIEM",
  "bankBin": "970400"
}'

echo ""
echo "=== Testing withdrawal with amount above maximum ==="
POST "/api/withdrawals/create" '{
  "amount": 10000000,
  "bankAccount": "1234567890",
  "bankName": "Ngân hàng TMCP Sài Gòn Thương Tín (Sacombank)",
  "bankBin": "970400"
}'
