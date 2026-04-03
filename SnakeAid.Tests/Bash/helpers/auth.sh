#!/bin/bash
# Shared auth helper — source this from other scripts: source ./auth.sh [member|expert|rescuer|admin|all]
# Tokens are cached in /tmp/snakeaid-token-* and reused across scripts.
# To force re-login: rm /tmp/snakeaid-token-* then run again.

BASE_URL="http://[::1]:8080"
TOKEN_TTL=3500  # seconds before token is considered stale (server issues 1h tokens)

_token_file() {
  echo "/tmp/snakeaid-token-$1"
}

_is_token_fresh() {
  local file="$1"
  [ -f "$file" ] || return 1
  local age=$(( $(date +%s) - $(date -r "$file" +%s 2>/dev/null || echo 0) ))
  [ "$age" -lt "$TOKEN_TTL" ]
}

_login() {
  local email="$1" password="$2" label="$3"
  local cache_file
  cache_file=$(_token_file "$label")

  # Return cached token if fresh
  if _is_token_fresh "$cache_file"; then
    echo "[auth] $label token cached (reused)" >&2
    cat "$cache_file"
    return 0
  fi

  # Login and cache
  local resp
  resp=$(curl --globoff -s -X POST "$BASE_URL/api/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$email\",\"password\":\"$password\"}")
  local token
  token=$(echo "$resp" | jq -r '.data.accessToken // empty')
  if [ -z "$token" ] || [ "$token" = "None" ]; then
    echo "[auth] FAILED to login $label ($email)" >&2
    return 1
  fi
  echo "$token" > "$cache_file"
  echo "[auth] $label logged in (fresh)" >&2
  echo "$token"
}

AUTH_SCOPE="${1:-member}"

if [ "$AUTH_SCOPE" = "member" ] || [ "$AUTH_SCOPE" = "all" ]; then
  MEMBER_TOKEN=$(_login "khiemnguyen120218@gmail.com" "120216" "member")
  TOKEN="$MEMBER_TOKEN"
fi

if [ "$AUTH_SCOPE" = "expert" ] || [ "$AUTH_SCOPE" = "all" ]; then
  EXPERT_TOKEN=$(_login "khiemnguyen120217@gmail.com" "120216" "expert")
fi

if [ "$AUTH_SCOPE" = "rescuer" ] || [ "$AUTH_SCOPE" = "all" ]; then
  RESCUER_TOKEN=$(_login "khiemnguyen120216@gmail.com" "120216" "rescuer")
fi

if [ "$AUTH_SCOPE" = "admin" ] || [ "$AUTH_SCOPE" = "all" ]; then
  ADMIN_TOKEN=$(_login "demo.admin@snakeaid.test" "Demo@123" "admin")
fi
