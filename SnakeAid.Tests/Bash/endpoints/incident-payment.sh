#!/bin/bash
# Test SnakebiteIncident payment endpoints (controller: api/incidents)
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"

FAKE_INCIDENT_ID="00000000-0000-0000-0000-000000000001"

echo ""
echo "=== POST /api/incidents/{incidentId}/payment/payos — Create PayOS link ==="
POST "/api/incidents/$FAKE_INCIDENT_ID/payment/payos" "{
  \"snakebiteIncidentId\": \"$FAKE_INCIDENT_ID\",
  \"amount\": 150000,
  \"description\": \"Test incident payment\",
  \"transactionType\": \"SnakebiteIncidentPayment\"
}"

echo ""
echo "=== POST /api/incidents/{incidentId}/payment/wallet — Pay with wallet ==="
POST "/api/incidents/$FAKE_INCIDENT_ID/payment/wallet" "{
  \"snakebiteIncidentId\": \"$FAKE_INCIDENT_ID\",
  \"amount\": 150000,
  \"description\": \"Test wallet payment\",
  \"transactionType\": \"SnakebiteIncidentPayment\"
}"
