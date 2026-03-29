#!/usr/bin/env bash
#
# deploy.sh — Deploy and run OtomAI tools on a remote Windows machine via SSH.
#
# Usage:
#   ./scripts/deploy.sh <tool-name> [extra-args...]
#
# Environment variables (set these or use scripts/env.sh):
#   WIN_HOST     — Windows machine IP or hostname
#   WIN_USER     — SSH username (default: otomai-agent)
#   WIN_PORT     — SSH port (default: 22)
#   WIN_KEY      — Path to SSH private key (optional, uses ssh-agent or password if unset)
#   DEPLOY_DIR   — Remote directory for tool files (default: C:\OtomAI)
#
# Examples:
#   ./scripts/deploy.sh account-creator --count 1 --proxy-mode direct
#   WIN_HOST=192.168.1.50 ./scripts/deploy.sh account-creator

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Defaults
WIN_HOST="${WIN_HOST:?'Set WIN_HOST to the Windows machine IP or hostname'}"
WIN_USER="${WIN_USER:-otomai-agent}"
WIN_PORT="${WIN_PORT:-22}"
DEPLOY_DIR="${DEPLOY_DIR:-C:\\OtomAI}"
TOOL_NAME="${1:?'Usage: deploy.sh <tool-name> [extra-args...]'}"
shift

TOOL_DIR="$REPO_ROOT/tools/$TOOL_NAME"
if [ ! -d "$TOOL_DIR" ]; then
    echo "Error: tool directory not found: $TOOL_DIR"
    exit 1
fi

# Build SSH options
SSH_OPTS=(-o StrictHostKeyChecking=accept-new -o ConnectTimeout=10 -p "$WIN_PORT")
SCP_OPTS=(-o StrictHostKeyChecking=accept-new -o ConnectTimeout=10 -P "$WIN_PORT")
if [ -n "${WIN_KEY:-}" ]; then
    SSH_OPTS+=(-i "$WIN_KEY")
    SCP_OPTS+=(-i "$WIN_KEY")
fi

REMOTE="${WIN_USER}@${WIN_HOST}"
REMOTE_TOOL_DIR="${DEPLOY_DIR}\\${TOOL_NAME}"

echo "=== OtomAI Deploy ==="
echo "Host:   $REMOTE:$WIN_PORT"
echo "Tool:   $TOOL_NAME"
echo "Remote: $REMOTE_TOOL_DIR"
echo ""

# Step 1: Ensure remote directory exists
echo "[1/4] Creating remote directory..."
ssh "${SSH_OPTS[@]}" "$REMOTE" "if not exist \"$REMOTE_TOOL_DIR\" mkdir \"$REMOTE_TOOL_DIR\""

# Step 2: Copy tool files (exclude __pycache__, .pyc)
echo "[2/4] Copying tool files..."
# Create a temp tarball excluding junk, extract on remote side
TAR_TMP=$(mktemp /tmp/otomai-deploy-XXXXXX.tar.gz)
tar -czf "$TAR_TMP" -C "$TOOL_DIR" --exclude='__pycache__' --exclude='*.pyc' .
scp "${SCP_OPTS[@]}" "$TAR_TMP" "$REMOTE:${DEPLOY_DIR}\\${TOOL_NAME}-deploy.tar.gz"
rm -f "$TAR_TMP"

# Extract on Windows (requires tar, available on Win10 1803+)
ssh "${SSH_OPTS[@]}" "$REMOTE" "cd /d \"$REMOTE_TOOL_DIR\" && tar -xzf \"${DEPLOY_DIR}\\${TOOL_NAME}-deploy.tar.gz\" && del \"${DEPLOY_DIR}\\${TOOL_NAME}-deploy.tar.gz\""

# Step 3: Install Python dependencies if requirements.txt exists
echo "[3/4] Installing dependencies..."
ssh "${SSH_OPTS[@]}" "$REMOTE" "cd /d \"$REMOTE_TOOL_DIR\" && if exist requirements.txt (python -m pip install -r requirements.txt --quiet)"

# Step 4: Run the tool
echo "[4/4] Running $TOOL_NAME..."
echo "--- Remote output begins ---"
ssh "${SSH_OPTS[@]}" "$REMOTE" "cd /d \"$REMOTE_TOOL_DIR\" && python main.py $*"
EXIT_CODE=$?
echo "--- Remote output ends ---"
echo ""
echo "Exit code: $EXIT_CODE"
exit $EXIT_CODE
