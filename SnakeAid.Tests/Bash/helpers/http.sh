#!/bin/bash
# HTTP helpers — source this after auth.sh
# Giảm boilerplate, output vẫn trực tiếp ra terminal.

BASE_URL="${BASE_URL:-http://[::1]:8080}"

# GET với auth
# Usage: GET /api/users/me/consultations?status=Ongoing
GET() {
  curl --globoff -s -X GET "${BASE_URL}$1" \
    -H "Authorization: Bearer $TOKEN" | python -m json.tool 2>/dev/null
}

# GET không cần auth (public endpoints)
# Usage: GET_PUBLIC /api/snake-species/search?q=cobra
GET_PUBLIC() {
  curl --globoff -s -X GET "${BASE_URL}$1" | python -m json.tool 2>/dev/null
}

# POST với auth + JSON body
# Usage: POST /api/wallet/topup '{"amount": 50000}'
POST() {
  curl --globoff -s -X POST "${BASE_URL}$1" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "$2" | python -m json.tool 2>/dev/null
}

# POST không có body (action endpoints)
# Usage: POST_ACTION /api/consultations/{id}/end
POST_ACTION() {
  curl --globoff -s -X POST "${BASE_URL}$1" \
    -H "Authorization: Bearer $TOKEN" | python -m json.tool 2>/dev/null
}

# GET với auth, trả raw JSON (cho pipe/parse tiếp)
# Usage: CONSUL_ID=$(GET_RAW /api/users/me/consultations?pageNumber=1 | python -c "...")
GET_RAW() {
  curl --globoff -s -X GET "${BASE_URL}$1" \
    -H "Authorization: Bearer $TOKEN"
}

# GET public, trả raw JSON
GET_PUBLIC_RAW() {
  curl --globoff -s -X GET "${BASE_URL}$1"
}
