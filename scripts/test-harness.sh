#!/usr/bin/env bash
#
# test-harness.sh — Run an OtomAI tool on Windows via SSH, collect logs, and report results.
#
# Usage:
#   ./scripts/test-harness.sh <tool-name> [extra-args...]
#
# Environment variables (same as deploy.sh):
#   WIN_HOST, WIN_USER, WIN_PORT, WIN_KEY, DEPLOY_DIR
#
# Output:
#   - Logs saved to logs/<tool-name>/<timestamp>.log
#   - Summary printed to stdout
#   - Exit code 0 = pass, non-zero = fail
#
# Examples:
#   ./scripts/test-harness.sh account-creator --count 1 --proxy-mode direct
#   WIN_HOST=192.168.1.50 ./scripts/test-harness.sh account-creator

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

WIN_HOST="${WIN_HOST:?'Set WIN_HOST to the Windows machine IP or hostname'}"
WIN_USER="${WIN_USER:-otomai-agent}"
WIN_PORT="${WIN_PORT:-22}"
DEPLOY_DIR="${DEPLOY_DIR:-C:\\OtomAI}"
TOOL_NAME="${1:?'Usage: test-harness.sh <tool-name> [extra-args...]'}"
shift

# Prepare local log directory
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
LOG_DIR="$REPO_ROOT/logs/$TOOL_NAME"
mkdir -p "$LOG_DIR"
LOG_FILE="$LOG_DIR/${TIMESTAMP}.log"

SSH_OPTS=(-o StrictHostKeyChecking=accept-new -o ConnectTimeout=10 -p "$WIN_PORT")
if [ -n "${WIN_KEY:-}" ]; then
    SSH_OPTS+=(-i "$WIN_KEY")
fi

REMOTE="${WIN_USER}@${WIN_HOST}"
REMOTE_TOOL_DIR="${DEPLOY_DIR}\\${TOOL_NAME}"

echo "=== OtomAI Test Harness ==="
echo "Tool:      $TOOL_NAME"
echo "Host:      $REMOTE:$WIN_PORT"
echo "Log file:  $LOG_FILE"
echo "Started:   $(date -Iseconds)"
echo ""

# Write log header
{
    echo "=== OtomAI Test Run ==="
    echo "Tool:      $TOOL_NAME"
    echo "Args:      $*"
    echo "Host:      $REMOTE"
    echo "Started:   $(date -Iseconds)"
    echo "========================"
    echo ""
} > "$LOG_FILE"

# Step 1: Deploy latest code
echo "[1/3] Deploying latest code..."
"$SCRIPT_DIR/deploy.sh" "$TOOL_NAME" 2>&1 | tee -a "$LOG_FILE" && true
# deploy.sh will handle file copy + deps; we continue regardless

# Step 2: Run the tool and capture output
echo "[2/3] Running tool on Windows..."
EXIT_CODE=0
ssh "${SSH_OPTS[@]}" "$REMOTE" "cd /d \"$REMOTE_TOOL_DIR\" && python main.py $*" 2>&1 | tee -a "$LOG_FILE" || EXIT_CODE=$?

# Step 3: Collect any remote log files the tool may have produced
echo "[3/3] Collecting remote logs..."
REMOTE_LOGS="${REMOTE_TOOL_DIR}\\logs"
ssh "${SSH_OPTS[@]}" "$REMOTE" "if exist \"$REMOTE_LOGS\" (dir /b \"$REMOTE_LOGS\")" 2>/dev/null | while IFS= read -r rfile; do
    rfile=$(echo "$rfile" | tr -d '\r')
    if [ -n "$rfile" ]; then
        scp -o StrictHostKeyChecking=accept-new -P "$WIN_PORT" ${WIN_KEY:+-i "$WIN_KEY"} \
            "$REMOTE:${REMOTE_LOGS}\\${rfile}" "$LOG_DIR/${TIMESTAMP}-remote-${rfile}" 2>/dev/null || true
    fi
done

# Write log footer
{
    echo ""
    echo "========================"
    echo "Finished:  $(date -Iseconds)"
    echo "Exit code: $EXIT_CODE"
} >> "$LOG_FILE"

# Summary
echo ""
echo "=== Test Summary ==="
echo "Tool:      $TOOL_NAME"
echo "Result:    $([ $EXIT_CODE -eq 0 ] && echo 'PASS' || echo 'FAIL')"
echo "Exit code: $EXIT_CODE"
echo "Log:       $LOG_FILE"
echo "Finished:  $(date -Iseconds)"

exit $EXIT_CODE
