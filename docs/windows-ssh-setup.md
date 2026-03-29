# Windows SSH Setup Guide

This guide walks through enabling SSH access on your Windows machine so we can deploy and test tools remotely from the Linux development environment.

## Prerequisites

- Windows 10 (version 1809+) or Windows 11
- Administrator access on the Windows machine
- Network connectivity between this Linux server and your Windows machine

## Step 1: Install and Enable OpenSSH Server

Open **PowerShell as Administrator** and run:

```powershell
# Check if OpenSSH Server is already installed
Get-WindowsCapability -Online | Where-Object Name -like 'OpenSSH.Server*'

# Install if not present
Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0

# Start the SSH service
Start-Service sshd

# Set it to start automatically on boot
Set-Service -Name sshd -StartupType Automatic
```

Verify it is running:

```powershell
Get-Service sshd
# Should show Status: Running
```

## Step 2: Configure Firewall

Windows usually creates the firewall rule automatically. Verify:

```powershell
Get-NetFirewallRule -Name *ssh*
```

If no rule exists, create one:

```powershell
New-NetFirewallRule -Name sshd -DisplayName 'OpenSSH Server (sshd)' `
  -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22
```

## Step 3: Create a Dedicated User Account (Recommended)

Using a dedicated account keeps agent access separate from your personal account:

```powershell
# Create a local user for agent access
$Password = Read-Host -AsSecureString "Enter password for otomai-agent"
New-LocalUser -Name "otomai-agent" -Password $Password -Description "OtomAI remote testing account"

# Add to Users group (not Administrators unless needed)
Add-LocalGroupMember -Group "Users" -Member "otomai-agent"
```

Alternatively, you can use your existing Windows account — just share the username with us.

## Step 4: Test SSH Locally

From a regular PowerShell window on the Windows machine:

```powershell
ssh localhost
```

If it connects and prompts for credentials, SSH is working.

## Step 5: Find Your IP Address

```powershell
ipconfig
```

Look for the IPv4 address under your active network adapter (Wi-Fi or Ethernet). Example: `192.168.1.x`.

## Step 6: Network Accessibility

### Same LAN (easiest)
If the Linux server and Windows machine are on the same local network, the IP from Step 5 is all we need.

### Different Networks (port forwarding)
If on different networks, you'll need to forward port 22 on your router to your Windows machine's local IP:

1. Log into your router admin panel (usually `192.168.1.1`)
2. Find Port Forwarding settings
3. Add a rule: External port `22` → Internal IP `<your-windows-ip>` port `22`
4. Share your public IP (check at `https://ifconfig.me`)

### VPN Alternative
If port forwarding isn't possible, a VPN like Tailscale or WireGuard can create a private tunnel between the machines.

## What to Share With Us

Once setup is complete, provide:

1. **Windows IP** (or public IP + port if port forwarding)
2. **Username** to SSH into (`otomai-agent` or your account)
3. **Password** (or we can set up SSH key auth — preferred for automation)
4. **Confirmation** that `sshd` is running and you tested `ssh localhost`

We will then configure our deployment scripts with these details.
