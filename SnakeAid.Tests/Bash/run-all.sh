#!/bin/bash
# Run all endpoint tests + member flow
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "==============================="
echo "  ENDPOINT TESTS"
echo "==============================="

bash "$SCRIPT_DIR/endpoints/consultations.sh"
echo ""
bash "$SCRIPT_DIR/endpoints/wallet.sh"
echo ""
bash "$SCRIPT_DIR/endpoints/reviews.sh"
echo ""
bash "$SCRIPT_DIR/endpoints/snake-search.sh"
echo ""
bash "$SCRIPT_DIR/endpoints/snakecatching-payment.sh"
echo ""
bash "$SCRIPT_DIR/endpoints/payos-callback.sh"
echo ""
bash "$SCRIPT_DIR/endpoints/incident-payment.sh"
echo ""
bash "$SCRIPT_DIR/endpoints/consultation-payment.sh"

echo ""
echo "==============================="
echo "  FLOW TESTS"
echo "==============================="

bash "$SCRIPT_DIR/flows/member-flow.sh"
