# Windows Testing Workflow

End-to-end workflow for developing on Linux and testing OtomAI tools on a remote Windows machine via SSH.

## Architecture

```
Linux (dev)                    Windows (test target)
┌──────────────┐   SSH/SCP    ┌──────────────────┐
│ otomai repo  │ ──────────►  │ C:\OtomAI\       │
│ tools/       │   deploy.sh  │   account-creator │
│ scripts/     │              │   <other tools>   │
│ logs/        │ ◄──────────  │   logs/           │
│              │  test-harness │                   │
└──────────────┘              └──────────────────┘
```

## One-Time Setup

### Board (Windows side)
Follow [docs/windows-ssh-setup.md](windows-ssh-setup.md) to enable OpenSSH Server and share connection details.

### Agent (Linux side)

1. Copy the env template and fill in connection details:
   ```bash
   cp scripts/env.example.sh scripts/env.sh
   # Edit scripts/env.sh with the Windows IP, username, etc.
   ```

2. Add `scripts/env.sh` to `.gitignore` (contains credentials):
   ```bash
   echo "scripts/env.sh" >> .gitignore
   ```

3. (Optional) Set up SSH key auth for passwordless automation:
   ```bash
   ssh-keygen -t ed25519 -f ~/.ssh/otomai-windows -N ""
   ssh-copy-id -i ~/.ssh/otomai-windows -p 22 otomai-agent@<WIN_HOST>
   # Then set WIN_KEY=~/.ssh/otomai-windows in scripts/env.sh
   ```

4. Test connectivity:
   ```bash
   source scripts/env.sh
   ssh -p $WIN_PORT $WIN_USER@$WIN_HOST "echo Connected OK"
   ```

## Daily Workflow

### Deploy and run a tool
```bash
source scripts/env.sh
./scripts/deploy.sh account-creator --count 1 --proxy-mode direct
```

This will:
1. Create `C:\OtomAI\account-creator\` on the Windows machine
2. Copy all tool files (excluding `__pycache__`)
3. Install Python dependencies from `requirements.txt`
4. Run `python main.py` with the provided arguments

### Run with test harness (deploy + log collection)
```bash
source scripts/env.sh
./scripts/test-harness.sh account-creator --count 1 --proxy-mode file
```

This wraps `deploy.sh` and additionally:
- Saves all output to `logs/account-creator/<timestamp>.log`
- Collects any remote log files the tool produced
- Prints a PASS/FAIL summary

### Check previous test results
```bash
ls -lt logs/account-creator/
cat logs/account-creator/<latest>.log
```

## Adding a New Tool

1. Create `tools/<tool-name>/` with a `main.py` entry point
2. Add a `requirements.txt` if the tool has Python dependencies
3. Deploy and test:
   ```bash
   ./scripts/test-harness.sh <tool-name>
   ```

The scripts use `main.py` as the entry point convention. If a tool uses a different entry point, modify `deploy.sh` or pass a wrapper.

## Troubleshooting

| Problem | Fix |
|---------|-----|
| `Connection refused` | Verify `sshd` is running on Windows: `Get-Service sshd` |
| `Connection timed out` | Check firewall, port forwarding, and IP address |
| `Permission denied` | Verify username/password or SSH key is correct |
| `python not found` on Windows | Install Python on Windows and ensure it's in PATH |
| `tar not found` on Windows | Requires Windows 10 1803+ (tar is built-in) |
