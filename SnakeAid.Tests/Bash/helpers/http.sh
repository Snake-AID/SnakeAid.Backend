#!/bin/bash
# HTTP helpers — source this after auth.sh
# Giảm boilerplate, output trực tiếp ra terminal.

BASE_URL="${BASE_URL:-http://[::1]:8080}"

# GET với auth
GET() {
  curl --globoff -s -X GET "${BASE_URL}$1" \
    -H "Authorization: Bearer $TOKEN" | jq .
}

# GET không cần auth
GET_PUBLIC() {
  curl --globoff -s -X GET "${BASE_URL}$1" | jq .
}

# POST với auth + JSON body
POST() {
  curl --globoff -s -X POST "${BASE_URL}$1" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "$2" | jq .
}

# POST không có body
POST_ACTION() {
  curl --globoff -s -X POST "${BASE_URL}$1" \
    -H "Authorization: Bearer $TOKEN" | jq .
}

# POST với auth + JSON body, trả raw JSON
POST_RAW() {
  curl --globoff -s -X POST "${BASE_URL}$1" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "$2"
}

# POST với auth, không body, trả raw JSON
POST_ACTION_RAW() {
  curl --globoff -s -X POST "${BASE_URL}$1" \
    -H "Authorization: Bearer $TOKEN"
}

# GET với auth, trả raw JSON (cho pipe/parse tiếp)
GET_RAW() {
  curl --globoff -s -X GET "${BASE_URL}$1" \
    -H "Authorization: Bearer $TOKEN"
}

# GET public, trả raw JSON
GET_PUBLIC_RAW() {
  curl --globoff -s -X GET "${BASE_URL}$1"
}
