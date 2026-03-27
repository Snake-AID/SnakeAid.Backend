#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/auth.sh" all

echo ""
echo "MEMBER_TOKEN: ${MEMBER_TOKEN:0:50}..."
echo "EXPERT_TOKEN: ${EXPERT_TOKEN:0:50}..."
echo "RESCUER_TOKEN: ${RESCUER_TOKEN:0:50}..."
