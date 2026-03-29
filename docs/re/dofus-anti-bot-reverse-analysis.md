# Analysis: dofus-anti-bot-reverse

> Source: [github.com/krm35/dofus-anti-bot-reverse](https://github.com/krm35/dofus-anti-bot-reverse)
> License: GPL-3.0
> Last updated: 2025-02-18 (all commits on same day)
> Analyzed: 2026-03-29

---

## Summary

This is a **documentation-only repo** (README + LICENSE, no executable code) that reverse-engineers the **Dofus 3.0 in-game anti-bot challenge-response protocol**. It documents the cryptographic handshake a socket bot must complete to prove it is a legitimate client. The content is written in French.

**Key finding:** This repo documents the **game server verification protocol**, not the web/HAAPI auth flow we already documented in `ankama-auth-spec.md`. It covers the challenge that happens *after* a client connects to the game server via socket — a separate layer from account creation or launcher auth.

---

## What the Repo Documents

### 1. Diffie-Hellman Parameters

The challenge uses DH key exchange with RFC 2409 768-bit parameters:

| Variable | Value |
|----------|-------|
| `cvld` | `DHStandardGroups.rfc2409_768.P` (prime) |
| `cvle` | `DHStandardGroups.rfc2409_768.Q` (order) |
| `cvlf` | `DHStandardGroups.rfc2409_768.G` (generator) |

These are standard, well-known DH parameters from BouncyCastle.

### 2. Challenge Flow (4-step protocol)

```
Client                                Server
  |                                     |
  |    <-- ServerVerificationEvent --   |  (1) Server initiates
  |                                     |
  |  -- ClientChallengeInitRequest -->  |  (2) Client sends challengeKey
  |                                     |
  |    <-- ServerChallengeEvent -----   |  (3) Server responds (with or without Value)
  |                                     |
  |  -- ClientChallengeProofRequest --> |  (4) Client sends proof
  |                                     |
```

### 3. Client Key Generation (Step 2)

```
cvlg = random(1024 bytes) mod cvld       // ephemeral private-like value
cvlh = random(50 bytes) as BigInteger    // nonce
challengeKey = cvlf^cvlh mod cvld        // sent to server
```

### 4. UniqueDeviceId (Hardware Fingerprint)

The client generates a hardware fingerprint using:
- `Win32_BaseBoard.SerialNumber`
- `Win32_BIOS.SerialNumber`
- `Win32_OperatingSystem.SerialNumber`
- All network interface MAC addresses (limited to ~4 on Unity)

Process: `SHA1(concat(serials))` + append MACs → `SHA256(result)` → hex string.

**Important caveat:** Unity enumerates fewer network interfaces than .NET 9 standalone (5 vs 49). A bot must match the Unity enumeration behavior.

### 5. Proof Computation (Step 4)

**Case A — ServerChallengeEvent has no Value:**

```
b1 = cvlf^cvlg mod cvld
b2 = cvlf^cvlh mod cvld
hash = SHA256(concat(str(cvlf), str(b1), str(b2), uniqueDeviceId))
hash_reversed = reverse(hash_bytes)
o = BigInteger(hash_reversed, unsigned)
m = o * cvlg
a = cvlh + m
proof = str((a mod cvle + cvle) mod cvle)   // ensure positive
```

**Case B — ServerChallengeEvent has a Value:**

```
cv = parse(Value) * cvlg
l = cvlh + cv
proof = str((l mod cvle + cvle) mod cvle)   // ensure positive
```

Case B is simpler — the server provides a pre-computed challenge value, eliminating the need for SHA256 hashing and device fingerprinting in the proof itself.

---

## Assessment

### Accuracy & Currency

- **Likely accurate for Dofus 3.0 as of Feb 2025.** The C# code uses BouncyCastle and WMI APIs consistent with Unity/IL2CPP decompilation output.
- **No verification possible** — there's no runnable code, no tests, no CI. The README is the only artifact.
- **Single-day commit history** suggests a one-off documentation dump, not an actively maintained project.

### Functional Status

- **Not functional as-is.** This is documentation, not a tool. A developer would need to implement the protocol from these notes.
- The notes appear complete for the challenge-response flow but don't cover:
  - How to establish the initial socket connection
  - The game protocol framing/serialization
  - Other verification mechanisms that may exist
  - Whether the challenge parameters have been rotated since Feb 2025

### Relevance to Our Work

| Aspect | Relevance | Notes |
|--------|-----------|-------|
| DH challenge protocol | **High** | Essential for any socket-based bot |
| UniqueDeviceId generation | **High** | Required for challenge proof; must match Unity behavior |
| Obfuscated variable names (`cvld`, etc.) | **Medium** | Helps map decompiled IL2CPP code to protocol semantics |
| Packet names (`ServerVerificationEvent`, etc.) | **High** | Protocol message identifiers for our own RE work |
| RFC 2409 DH parameters | **Low** | Standard/well-known, but confirms which group Ankama chose |

### Key Takeaways for Our Anti-Bot Bypass

1. **The game server has its own challenge layer** independent of HAAPI/WAF auth. A working bot needs both the web auth flow (documented in `ankama-auth-spec.md`) AND this socket challenge.

2. **Hardware fingerprinting is part of the protocol.** The `UniqueDeviceId` is baked into the proof computation. We need to either:
   - Spoof realistic device IDs per bot instance, or
   - Extract the fingerprint generation from the Unity client

3. **The protocol is relatively standard DH-based ZKP.** It's a Schnorr-like identification scheme. Not custom crypto — uses well-known primitives (RFC 2409, SHA256, BouncyCastle).

4. **Two code paths exist** (with/without server Value). Both must be implemented.

5. **Unity network interface enumeration quirk** — the number of interfaces returned differs between Unity and standalone .NET. Bots must replicate Unity's behavior exactly.

---

## Recommendations

- **Verify these findings** against our own IL2CPP decompilation of the current Dofus 3.0 client to confirm nothing has changed since Feb 2025.
- **Implement the challenge-response** as a module in our bot framework once socket protocol work begins.
- **Build a UniqueDeviceId generator** that mimics Unity's enumeration behavior for spoofed hardware fingerprints.
- **Cross-reference packet names** (`ServerVerificationEvent`, `ClientChallengeInitRequest`, `ServerChallengeEvent`, `ClientChallengeProofRequest`) with our protocol RE to map the full handshake sequence.
