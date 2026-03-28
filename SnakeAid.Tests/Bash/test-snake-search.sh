#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "=== TEST 1: Search by scientific name ==="
curl --globoff -s "http://[::1]:8080/api/snake-species/search?q=ophiophagus" | python -m json.tool 2>/dev/null

echo ""
echo "=== TEST 2: Search by common name (case-insensitive via ILIKE) ==="
curl --globoff -s "http://[::1]:8080/api/snake-species/search?q=Bungarus" | python -m json.tool 2>/dev/null

echo ""
echo "=== TEST 3: Empty query ==="
curl --globoff -s "http://[::1]:8080/api/snake-species/search?q=" | python -m json.tool 2>/dev/null

echo ""
echo "=== TEST 4: No match ==="
curl --globoff -s "http://[::1]:8080/api/snake-species/search?q=dinosaur" | python -m json.tool 2>/dev/null
