# Bash Testing Guide

## Execution Environment

- All API testing uses Git Bash scripts in this folder.
- Execute from PowerShell: `& "C:\Program Files\Git\bin\bash.exe" SnakeAid.Tests/Bash/test-all.sh`
- Execute from Git Bash: `bash SnakeAid.Tests/Bash/test-all.sh`
- Do not use PowerShell-native `curl` or `Invoke-WebRequest`.

## Auth System

`auth.sh` is the shared login helper. All scripts source it.

```bash
source "$SCRIPT_DIR/auth.sh" member   # provides $TOKEN, $MEMBER_TOKEN
source "$SCRIPT_DIR/auth.sh" expert   # provides $EXPERT_TOKEN
source "$SCRIPT_DIR/auth.sh" all      # provides all 3 tokens
```

Token caching:
- Tokens are cached in `/tmp/snakeaid-token-{role}`.
- Subsequent calls within ~58 minutes reuse cached token (no API call).
- Force re-login: `rm /tmp/snakeaid-token-*`

## curl Conventions

- Always use `curl --globoff` for IPv6 URLs (`http://[::1]:8080/...`).
- Pipe through `python -m json.tool` for readable output.
- Use `-s` (silent) to suppress progress bars.
- Print section headers: `echo "=== TEST: description ==="`.

## Available Scripts

| Script | Purpose | Auth scope |
|--------|---------|------------|
| `auth.sh` | Shared login helper (source, not run) | configurable |
| `test-login.sh` | Login all 3 roles, print tokens | all |
| `test-topup.sh` | Wallet top-up via PayOS | member |
| `test-endpoints.sh` | Consultation endpoints + topup | member |
| `test-snake-search.sh` | Snake species search (ILIKE) | none (public) |
| `test-all.sh` | Full suite: consultations + topup + reviews + snake search | member |

## Adding New Test Scripts

Template:

```bash
#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/auth.sh" member  # or expert, rescuer, all

echo ""
echo "=== TEST: your feature ==="
curl --globoff -s -X GET "http://[::1]:8080/api/your-endpoint" \
  -H "Authorization: Bearer $TOKEN" | python -m json.tool 2>/dev/null
```

Rules:
- Name: `test-{feature}.sh`
- Always source `auth.sh` (never inline login logic)
- Use `$TOKEN` for member, `$EXPERT_TOKEN` for expert, `$RESCUER_TOKEN` for rescuer
- Keep scripts idempotent where possible
- One script per feature or flow

## Post-Implementation Testing Workflow

When you finish implementing a feature, follow this cycle:

### 1. Write the test script first

```bash
# Create SnakeAid.Tests/Bash/test-{feature}.sh
# Cover: happy path, error cases, auth checks
```

### 2. Start server and run

```
# From PowerShell (project root):
dotnet run --project SnakeAid.Api

# From another terminal:
& "C:\Program Files\Git\bin\bash.exe" SnakeAid.Tests/Bash/test-{feature}.sh
```

### 3. Validate response

Check:
- `is_success: true` for happy paths
- Correct `status_code` (200, 400, 403, 404, 409)
- Response shape matches DTO (all expected fields present)
- Error messages are descriptive

### 4. Test edge cases

Add cases for:
- Missing/invalid auth token (expect 401)
- Wrong role (expect 403)
- Invalid input (expect 400/422)
- Conflict states (expect 409)
- Not found (expect 404)

### 5. Multi-role testing

If the feature involves multiple roles (e.g., user creates, expert accepts):

```bash
source "$SCRIPT_DIR/auth.sh" all

# Step 1: member creates
curl ... -H "Authorization: Bearer $MEMBER_TOKEN" ...

# Step 2: expert acts
curl ... -H "Authorization: Bearer $EXPERT_TOKEN" ...
```

### 6. Regression check

After fixing a bug or changing behavior, run `test-all.sh` to verify nothing else broke:

```bash
& "C:\Program Files\Git\bin\bash.exe" SnakeAid.Tests/Bash/test-all.sh
```

## Request Shape Rules

- Preserve request shape exactly when goal is behavior matching:
  - Keep IPv6 URLs as-is (`http://[::1]:8080/...`)
  - Keep headers unless normalization is explicitly requested
  - Keep body shape and field casing
- If normalized request is needed for diagnosis, separate it from exact reproduction and state clearly it is diagnostic.
