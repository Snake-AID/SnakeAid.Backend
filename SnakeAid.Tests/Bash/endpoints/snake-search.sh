#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/http.sh"

echo "=== Search by scientific name ==="
GET_PUBLIC "/api/snake-species/search?q=ophiophagus"

echo ""
echo "=== Search case-insensitive (ILIKE) ==="
GET_PUBLIC "/api/snake-species/search?q=Bungarus"

echo ""
echo "=== Empty query ==="
GET_PUBLIC "/api/snake-species/search?q="

echo ""
echo "=== No match ==="
GET_PUBLIC "/api/snake-species/search?q=dinosaur"
