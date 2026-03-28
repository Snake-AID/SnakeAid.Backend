# Bash Testing Guide

## Cấu trúc

```
Bash/
├── helpers/
│   ├── auth.sh                  # Shared login, token caching
│   └── http.sh                  # GET, POST, GET_PUBLIC, GET_RAW helpers
├── endpoints/                   # Test từng endpoint riêng lẻ
│   ├── consultations.sh
│   ├── wallet.sh
│   ├── reviews.sh
│   └── snake-search.sh
├── flows/                       # Multi-step user journeys
│   └── member-flow.sh
├── run-all.sh
└── AGENTS.md
```

## Chạy

```bash
# Từ PowerShell (project root):
& "C:\Program Files\Git\bin\bash.exe" SnakeAid.Tests/Bash/run-all.sh

# Chạy 1 endpoint:
& "C:\Program Files\Git\bin\bash.exe" SnakeAid.Tests/Bash/endpoints/wallet.sh

# Chạy 1 flow:
& "C:\Program Files\Git\bin\bash.exe" SnakeAid.Tests/Bash/flows/member-flow.sh

# Force re-login:
rm /tmp/snakeaid-token-*
```

## Auth

`helpers/auth.sh` + `helpers/http.sh` nằm trong helpers/. Tất cả scripts source chúng:

```bash
# Từ endpoints/ hoặc flows/
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"
```

Token cached trong `/tmp/snakeaid-token-{role}`, tự reuse trong ~58 phút.

## Quy tắc

- Dùng `helpers/http.sh` để giảm boilerplate — output vẫn trực tiếp ra terminal
- Không hide response vào file trung gian
- Print section headers: `echo "=== description ==="` 
- Endpoints không cần auth (snake search) dùng `GET_PUBLIC` thay vì `GET`

## HTTP Helpers (`helpers/http.sh`)

Source sau auth.sh. Cung cấp:

| Function | Mục đích |
|----------|---------|
| `GET /path` | GET với auth, pretty-print JSON |
| `GET_PUBLIC /path` | GET không auth, pretty-print JSON |
| `POST /path '{"body"}'` | POST với auth + JSON body, pretty-print |
| `POST_ACTION /path` | POST với auth, không body |
| `GET_RAW /path` | GET với auth, trả raw JSON (cho pipe/parse) |
| `GET_PUBLIC_RAW /path` | GET không auth, trả raw JSON |

Tất cả dùng `$BASE_URL` (default `http://[::1]:8080`) và `$TOKEN` từ auth.sh.

## Thêm script mới

Endpoint test → `endpoints/{feature}.sh`:
```bash
#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"

echo ""
echo "=== Mô tả test ==="
GET "/api/your-endpoint"
POST "/api/your-endpoint" '{"field": "value"}'
```

Flow test → `flows/{role}-flow.sh`:
```bash
#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" all
source "$SCRIPT_DIR/../helpers/http.sh"

# Step 1: member action
TOKEN="$MEMBER_TOKEN"
GET "/api/users/me/consultations"

# Step 2: expert action  
TOKEN="$EXPERT_TOKEN"
GET "/api/experts/me/consultation-bookings"
```

Sau khi thêm, cập nhật `run-all.sh` để include script mới.

## Post-Implementation Testing

1. Viết script trong `endpoints/` hoặc `flows/`
2. Start server: `dotnet run --project SnakeAid.Api`
3. Chạy script, verify response trực tiếp trên terminal
4. Check: `is_success`, `status_code`, response shape, error messages
5. Regression: `run-all.sh` sau mỗi thay đổi

## Dependencies

| Tool | Mục đích | Cài đặt |
|------|---------|---------|
| `curl` | HTTP calls | Có sẵn trong Git Bash |
| `jq` | JSON pretty-print + parse | `winget install jqlang.jq` |
| Git Bash | Shell runtime | Có sẵn khi cài Git for Windows |

Trước khi chạy test, verify dependencies:
```bash
jq --version    # cần jq-1.6+
curl --version  # có sẵn
```

Nếu `jq` chưa có:
```powershell
winget install jqlang.jq --accept-package-agreements
# Restart terminal sau khi cài để PATH cập nhật
```
