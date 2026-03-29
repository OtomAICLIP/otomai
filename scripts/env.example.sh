#!/usr/bin/env bash
# Copy this file to scripts/env.sh and fill in your values.
# Source it before running deploy.sh or test-harness.sh:
#   source scripts/env.sh

export WIN_HOST="192.168.1.XXX"      # Windows machine IP
export WIN_USER="otomai-agent"        # SSH username
export WIN_PORT="22"                  # SSH port
export WIN_KEY=""                     # Path to SSH private key (leave empty for password auth)
export DEPLOY_DIR="C:\\OtomAI"        # Remote directory for tool files
