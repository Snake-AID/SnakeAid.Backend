# Bash Testing Guide

## Yêu cầu

Backend phải đang chạy trước khi chạy test.

```powershell
dotnet run --project SnakeAid.Api
```

Chờ log `Now listening on: <URL>` + `Application started.` Lấy URL từ log đó — nếu khác default, override:

```bash
BASE_URL="http://localhost:5000" bash SnakeAid.Tests/Bash/run-all.sh
```

Dependencies: `curl` (có sẵn Git Bash), `jq` (`winget install jqlang.jq`, restart terminal sau cài).

## Cấu trúc

```
Bash/
├── helpers/
│   ├── auth.sh          # Login + token caching (~58 phút)
│   └── http.sh          # GET, POST, GET_PUBLIC, GET_RAW, POST_ACTION
├── endpoints/           # Test từng endpoint
│   ├── consultations.sh
│   ├── wallet.sh
│   ├── reviews.sh
│   └── snake-search.sh
├── flows/               # Multi-step journeys
│   └── member-flow.sh
└── run-all.sh
```

Token cached trong `/tmp/snakeaid-token-{role}`, tự reuse trong ~58 phút.

## Chạy

```powershell
# Toàn bộ
& "C:\Program Files\Git\bin\bash.exe" SnakeAid.Tests/Bash/run-all.sh

# 1 endpoint
& "C:\Program Files\Git\bin\bash.exe" SnakeAid.Tests/Bash/endpoints/wallet.sh

# Xoá token cache
& "C:\Program Files\Git\bin\bash.exe" -c "rm -f /tmp/snakeaid-token-*"
```

## Helpers

Scripts source helpers từ `helpers/`:

```bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" member   # hoặc expert, rescuer, admin, all
source "$SCRIPT_DIR/../helpers/http.sh"
```

| Function | Mô tả |
|----------|-------|
| `GET /path` | GET + auth + jq |
| `GET_PUBLIC /path` | GET không auth + jq |
| `POST /path '{"body"}'` | POST + auth + JSON + jq |
| `POST_ACTION /path` | POST + auth, không body |
| `POST_RAW /path '{"body"}'` | POST + auth + JSON, raw JSON |
| `POST_ACTION_RAW /path` | POST + auth, không body, raw JSON |
| `GET_RAW /path` | GET + auth, raw JSON (cho pipe) |
| `GET_PUBLIC_RAW /path` | GET không auth, raw JSON |

Token: `$TOKEN` (member mặc định), `$MEMBER_TOKEN`, `$EXPERT_TOKEN`, `$RESCUER_TOKEN`, `$ADMIN_TOKEN`.

## Thêm script mới

```bash
#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../helpers/auth.sh" member
source "$SCRIPT_DIR/../helpers/http.sh"

echo "=== Mô tả ==="
GET "/api/your-endpoint"
```

Đặt trong `endpoints/` (đơn lẻ) hoặc `flows/` (multi-step). Cập nhật `run-all.sh` sau khi thêm.
