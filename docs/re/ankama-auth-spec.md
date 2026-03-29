# Ankama Account Creation & Authentication - Technical Spec

> RE analysis of the Ankama account registration and authentication flow.
> Covers the web registration, HAAPI REST API, WAF/anti-bot protections, Zaap launcher protocol, and 2FA system.

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Account Registration (Web-Only)](#2-account-registration-web-only)
3. [HAAPI REST API (Login & Session)](#3-haapi-rest-api-login--session)
4. [AWS WAF Token Flow](#4-aws-waf-token-flow)
5. [Zaap Protocol (Launcher <-> Game)](#5-zaap-protocol-launcher---game)
6. [2FA / Ankama Shield](#6-2fa--ankama-shield)
7. [Anti-Bot Protections Summary](#7-anti-bot-protections-summary)
8. [Viable Approaches for Programmatic Account Creation](#8-viable-approaches-for-programmatic-account-creation)
9. [Reference Implementations](#9-reference-implementations)

---

## 1. Architecture Overview

```
                     Cloudflare / AWS WAF
                            |
    +----------+     +------+------+     +-----------------+
    | Web Form | --> | auth.ankama | --> | Account Created |
    | (browser)|     | .com        |     |                 |
    +----------+     +-------------+     +-----------------+
                            |
                     [No direct API]
                     [Web-only flow]

    +----------+     +-------------+     +-----------------+
    | Launcher | --> | haapi.ankama| --> | API Key + Token |
    | (Zaap)   |     | .com/v5     |     | (login only)    |
    +----------+     +-------------+     +-----------------+
         |
    Thrift (TCP :26116)
         |
    +----------+
    | Dofus.exe|
    +----------+
```

**Key finding:** Account creation has NO API endpoint. It is exclusively done through the web form at `auth.ankama.com`. The HAAPI API only handles login/session management for existing accounts.

---

## 2. Account Registration (Web-Only)

### Endpoints

| Step | Method | URL |
|------|--------|-----|
| Login page | GET | `https://auth.ankama.com/login` |
| Registration form | GET | `https://auth.ankama.com/register/ankama/form?redirect_uri=...&origin_tracker=...` |
| Form submission | POST | `https://auth.ankama.com/register/ankama/form-submit` |

### OAuth2 PKCE Flow

The login page initiates an OAuth2 Authorization Code flow with PKCE:
- `code_challenge` parameter in URL
- `redirect_uri` for callback
- `origin_tracker` for analytics

### Registration Form Fields

| Field | Type | Required |
|-------|------|----------|
| `state` | hidden (JWT/token) | yes |
| `email` | email | yes |
| `password` | password | yes |
| `lastname` | text | yes |
| `firstname` | text | yes |
| `birthday-day` | select | yes |
| `birthday-month` | select | yes |
| `birthday-year` | select | yes |
| `newsletterSubscribe` | checkbox | no |

### Custom Elements

- Form is wrapped in a `<secure-form>` custom web component
- `<secure-form>` is a **double-submit prevention wrapper only** (source: `main.ts-Buc2RfQ7.js`):
  1. On first submit: disables button, shows loader
  2. On subsequent submits: calls `preventDefault()`
  3. Does NOT interact with WAF tokens or `AwsWafIntegration`
- Form submission is standard HTML POST — no JS fetch override
- The `aws-waf-token` cookie is set silently by `challenge.js` and sent automatically by the browser with the POST

### Protection Layers (Registration)

1. **Cloudflare** - IP reputation, bot detection (blocks datacenter/VPN/proxy IPs)
2. **AWS WAF** - Browser fingerprinting via `challenge.js`, generates `aws-waf-token` cookie
3. **CAPTCHA** - AWS WAF visual grid CAPTCHA ("choose all the X") + Arkose Labs FunCAPTCHA (multi-round image classification)

---

## 3. HAAPI REST API (Login & Session)

**Base URL:** `https://haapi.ankama.com/json/Ankama/v5/`

**Content-Type:** `text/plain` (URL-encoded body: `param1=value1&param2=value2`)

**Protection:** Cloudflare with aggressive anti-bot. VPNs, TOR, public proxies, and cloud IPs are blocked. Residential IPs required. HTTP/2 required.

### 3.1 CreateApiKey (Login)

```
POST /json/Ankama/v5/Api/CreateApiKey
Content-Type: text/plain

login=user%40email.com&password=pass123&game_id=102&long_life_token=true&shop_key=ZAAP&payment_mode=OK&lang=en&certificate_id=&certificate_hash=
```

**Response (200):**
```json
{
  "key": "ab4a316b-1675-4a31-d99c-f71f97da1ac4",
  "account_id": 123456789,
  "ip": "1.2.3.4",
  "added_date": "2022-09-12T19:14:57+02:00",
  "data": {
    "shop": "ZAAP",
    "lang": "en",
    "payment_mode": "OK",
    "country": "US",
    "currency": "USD",
    "security_state": "SECURED",
    "security_detail": "CERTIFICATE_OK"
  },
  "game_id": 102,
  "certificate_id": 127397326,
  "refresh_token": "104d27cd-1e75-4ea7-b2a0-810cd2561d0b",
  "expiration_date": "2022-10-11T19:14:57+01:00"
}
```

The `key` field is used as the `apikey` header for all subsequent requests.

**Error (401):** `{"reason": "FAILED"}`

### 3.2 SignOnWithApiKey

```
POST /json/Ankama/v5/Account/SignOnWithApiKey
apikey: <key>
Content-Type: text/plain

game=102
```

Returns full account info: nickname, tag, security status, subscriptions, login history.

### 3.3 CreateToken (Game Token)

```
GET /json/Ankama/v5/Account/CreateToken?game=1&certificate_id=<id>&certificate_hash=<hash>
apikey: <key>
```

**Response:** `{"token": "d1dff3c10e57718505ab7b37f9ccb3f6"}`

Token lifespan: ~1 minute. Used to authenticate with the game server.

### 3.4 Full Login Sequence

1. `POST Api/CreateApiKey` - get API key + refresh token
2. `GET Shield/SecurityCode` - get 2FA email domain (if Ankama Shield enabled)
3. `GET Shield/ValidateCode` - submit 2FA code from email
4. `POST Account/SignOnWithApiKey` - get account info + session
5. `GET Account/Status` - check ToS acceptance
6. `GET Game/ListWithApiKey` - list game subscriptions
7. `GET Account/CreateToken?game=99` - get chat token
8. `GET Game/StartSessionWithApiKey` - start game session
9. `POST Account/SendDeviceInfos` - send device telemetry
10. `GET Account/CreateToken?game=1` - get Dofus game token (~1 min TTL)

### 3.5 Known game_id Values

| ID | Game |
|----|------|
| 1 | Dofus |
| 18 | Dofus Touch |
| 99 | Ankama Chat |
| 101 | Dofus 2 |
| 102 | Ankama Launcher |

### 3.6 Additional API URLs

| Service | URL |
|---------|-----|
| HAAPI Ankama | `https://haapi.ankama.com/json/Ankama/v5/` |
| HAAPI Dofus | `https://haapi.ankama.com/json/Dofus/v3/` |
| Shop API | `https://shop-api.ankama.com/` |
| Avatar | `https://avatar.ankama.com/users/{id}.png` |
| Chat Server | `zaap-chat.ankama.com:6337` |

---

## 4. AWS WAF Token Flow

### Challenge Script

Loaded from: `https://3f38f7f4f368.edge.sdk.awswaf.com/3f38f7f4f368/e1fcfc58118e/challenge.js`
> Hash rotates periodically. Previous: `ab89a1b580a1` (pre-2026-03-29).

Automatically loaded in `<head>` with `defer` attribute.

### Token Generation Pipeline

```
Page Load
    |
    v
challenge.js loads --> Fingerprint collection begins
    |
    v
Fingerprinting (parallel):
  - Canvas rendering fingerprint
  - Screen properties (resolution, color depth, DPI)
  - Navigator properties (UA, plugins, extensions)
  - WebGL capabilities (GPU, driver info)
  - Timing analysis (execution speed)
  - Cryptographic capabilities (WebCrypto API)
    |
    v
Behavioral telemetry (ongoing):
  - Mouse click positions
  - Keyboard press intervals
  - Form field focus duration
  - Text cut/copy/paste events
  - Scroll/wheel events
  - DOM visibility changes
    |
    v
Token assembly:
  EventCycleTelemetry --> UTF8Encoder --> CRC32Calculator
                                              |
                                              v
                                         HexEncoder --> JSONEncoder
                                              |
                                              v
                                    FinalChecksum (CRC#HexValue)
                                              |
                                              v
                                   aws-waf-token cookie set
```

### AwsWafIntegration API

> **NOTE (2026-03-29):** Live testing confirmed that `AwsWafIntegration` is NOT exposed as a global
> on `auth.ankama.com`. The `challenge.js` script operates silently — it collects fingerprints,
> generates the token, and sets the `aws-waf-token` cookie directly without any JS API. The methods
> below are from the AWS WAF SDK documentation but are NOT available on this site.

| Method | Purpose | Available? |
|--------|---------|------------|
| `AwsWafIntegration.fetch(url, options)` | Wrapper around `fetch()` that auto-attaches WAF token | **NO** |
| `AwsWafIntegration.getToken()` | Returns current token, stores in `aws-waf-token` cookie | **NO** |
| `AwsWafIntegration.hasToken()` | Boolean: is there an unexpired token? | **NO** |

### Token Cookie

- **Name:** `aws-waf-token`
- **Contents:** Encrypted blob containing timestamps (last challenge success, last CAPTCHA success), client fingerprint, behavioral signals
- **Storage:** Also uses `localStorage` buffer (max 10,240 bytes, time-expiring)
- **Encryption:** AWS-proprietary, not publicly documented

### Why POST Fails from Datacenter IPs (Confirmed 2026-03-29)

Live testing with Camoufox confirmed:
- **GET requests pass** from datacenter IPs — pages load, challenge.js runs, cookie is set
- **POST requests return 403** — `x-cache: Error from cloudfront` indicates edge-level rejection
- The `aws-waf-token` cookie IS present and sent with the POST request
- The 403 response body is the registration form re-rendered (server received the POST)
- **Root cause: CloudFront WAF applies stricter IP reputation scoring on POST than GET**
- Residential IPs are required for POST submissions

The WAF fingerprinting also detects:
- CDP (Chrome DevTools Protocol) artifacts
- Canvas/WebGL rendering inconsistencies from headless mode
- Missing or inconsistent navigator properties
- Timing anomalies from emulated environments

### CAPTCHA Details

Two layers observed:
1. **AWS WAF CAPTCHA** - Visual grid: "Choose all the [object]" - triggers after failed challenge validation
2. **Arkose Labs FunCAPTCHA** - Multi-round image classification with time limits - triggers on suspicious sessions

If a valid `aws-waf-token` is generated with realistic fingerprints, the CAPTCHA may not trigger at all.

---

## 5. Zaap Protocol (Launcher <-> Game)

Communication between the Ankama Launcher and Dofus Unity client uses **Apache Thrift** over local TCP.

### Connection Details

- **Default ports:** 26116, 26117
- **Protocol:** Thrift binary
- **Direction:** Game client connects to launcher

### Thrift Service Definition

```thrift
service ZaapService {
  string connect(1:string gameName, 2:string releaseName, 3:i32 instanceId, 4:string hash);
  string auth_getGameToken(1:string gameSession, 2:i32 gameId);
  string auth_getGameTokenWithWindowId(1:string gameSession, 2:i32 gameId, 3:i32 windowId);
  bool updater_isUpdateAvailable(1:string gameSession);
  string settings_get(1:string gameSession, 2:string key);
  void settings_set(1:string gameSession, 2:string key, 3:string value);
  string userInfo_get(1:string gameSession);
  void release_restartOnExit(1:string gameSession);
  void release_exitAndRepair(1:string gameSession);
  string zaapVersion_get(1:string gameSession);
  bool zaapMustUpdate_get(1:string gameSession);
  string askForGuestComplete(1:string gameSession);
  bool hasPremiumAccess(1:string gameSession);
  bool payArticle(1:string gameSession, 2:string shopApiKey, 3:i32 articleId, 4:OverlayPosition pos);
}
```

### Game Launch Command

```bash
Dofus.exe --port 26116 --gameName dofus --gameRelease dofus3 \
  --instanceId 1 --hash <UUID> --canLogin true --langCode fr \
  --autoConnectType 0 --connectionPort <authPort> \
  --configUrl http://127.0.0.1:<httpPort>/divazaap.json
```

### Environment Variables

```
ZAAP_PORT=26116
ZAAP_GAME=dofus
ZAAP_RELEASE=dofus3
ZAAP_INSTANCE_ID=1
ZAAP_HASH=<UUID>
```

### UserInfo Format

```json
{
  "id": "123",
  "type": "ANKAMA",
  "login": "user@email.com",
  "nickname": "PlayerName",
  "firstname": "John",
  "lastname": "Doe",
  "nicknameWithTag": "PlayerName#0000"
}
```

---

## 6. 2FA / Ankama Shield

### Certificate Storage

Path: `%APPDATA%\zaap\certificate\.certif<first32chars_SHA256(email)>`

Format: `<IV_hex>|<AES-128-CBC_encrypted_JSON_hex>`

### Key Derivation

Symmetric key: `MD5(platform + "," + arch + "," + SHA256(MachineGuid)_hex + "," + cpuCount + "," + cpuName)`

### Hardware Hash (hm1/hm2)

- `hm1` = first 32 bytes of `SHA256(platform + arch + SHA256(MachineGuid)_hex + username + osVersion + computerRam)` as hex
- `hm2` = `hm1` reversed (read right-to-left)

### Certificate Hash Computation

1. Decode `encodedCertificate` from base64
2. Decrypt with AES-256-ECB using `hm2` as key
3. `certificate_hash = SHA256(hm1 + decrypted_certificate)` as hex

### Shield Validation Flow

1. `GET Shield/SecurityCode` -> returns email domain
2. User receives 5-char alphanumeric code via email
3. `GET Shield/ValidateCode?game_id=102&code=AB12C&hm1=<hash>&hm2=<hash_reversed>&name=launcher-<PCUSER>`
4. Success returns `{"id": 127397326, "encodedCertificate": "<base64>"}`
5. Certificate stored locally for future logins

---

## 7. Anti-Bot Protections Summary

| Layer | Technology | Blocks |
|-------|-----------|--------|
| Network | Cloudflare | Datacenter IPs, VPNs, TOR, proxies |
| Application | AWS WAF | Headless browsers, automation tools |
| Fingerprint | challenge.js | Canvas/WebGL/navigator inconsistencies |
| Behavioral | WAF telemetry | Non-human interaction patterns |
| Challenge | AWS WAF CAPTCHA | Failed fingerprint challenges |
| Challenge | Arkose Labs FunCAPTCHA | Multi-round image classification |
| Rate limit | Per-IP | Max ~4 accounts per IP |
| Protocol | HTTP/2 required | Simple HTTP/1.1 clients |

### What Gets Blocked

- All cloud provider IPs (AWS, GCP, Azure, etc.)
- VPN exit nodes (ExpressVPN, Mullvad, etc.)
- TOR exit nodes
- Public proxy IPs
- Headless Chrome/Puppeteer/Playwright (POST requests)
- `curl`/`wget`/`requests` without full browser fingerprint
- HTTP/1.1 only clients

### What Works

- Residential ISP IPs only
- Real browser with human-like interaction patterns
- Proper HTTP/2 with full TLS fingerprint

---

## 8. Viable Approaches for Programmatic Account Creation

### Option A: Browser Automation with Residential Proxies (Most Proven)

- Use Selenium/Puppeteer with **undetected-chromedriver** or **Camoufox**
- Route through residential proxy pool (not datacenter)
- Human-like interaction timing (mouse movements, typing delays)
- CAPTCHA solving service integration (2captcha, anti-captcha, CapSolver)
- Rate limit: ~4 accounts per IP, rotate proxies
- **Prior art:** `dofus-account-generator` uses this approach

### Option B: HTTP Client with Full Fingerprint Emulation

- Replicate TLS fingerprint (JA3/JA4) matching a real browser
- Use `curl-impersonate` or `tls-client` library to match Chrome's TLS
- Generate realistic `aws-waf-token` by:
  1. Solving the challenge.js fingerprint collection
  2. Feeding realistic hardware/behavioral data
  3. Submitting to WAF challenge endpoint
- Use residential proxy for IP reputation
- **Risk:** Very high effort, WAF updates may break it

### Option C: Real Browser Session Harvesting

- Human manually opens browser, navigates to registration
- Script captures the live `aws-waf-token`, cookies, and `state` token
- Script replays the form submission with captured tokens
- Semi-automated: human solves CAPTCHA, script fills form
- **Simplest to implement but doesn't scale**

### Option D: CAPTCHA Service + Residential Proxy API

- Use a CAPTCHA-solving service that also provides residential browser sessions
- Services like CapSolver, AntiCaptcha offer built-in browser fingerprint emulation
- Submit the registration as a "task" to the CAPTCHA service
- **Most scalable but highest per-account cost**

### Recommendation

**Option A** is the most proven and balanced approach. The existing `dofus-account-generator` project demonstrates it works. Key requirements:
1. Residential proxy pool (rotating, not sticky)
2. CAPTCHA solving service API key
3. Undetected browser automation framework
4. Rate limiting to ~4 accounts per IP

---

## 9. Reference Implementations

| Project | Language | Purpose |
|---------|----------|---------|
| [DivaZaap](https://github.com/jordanamr/DivaZaap) | Go | Zaap Thrift protocol emulator for Dofus Unity |
| [ThriftAnkama](https://github.com/Daweyy/ThriftAnkama) | Node.js/Bun | Zaap mock server with extended Thrift service |
| [DofusWeb-API](https://github.com/Foohx/DofusWeb-API) | PHP | Web SSO auth via account.ankama.com |
| [dofus-api](https://github.com/nightwolf93/dofus-api) | CoffeeScript | SSO login via `account.ankama.com/sso` |
| [dofus-account-generator](https://github.com/AxelConceicao/dofus-account-generator) | Python/Selenium | Account creation with proxy rotation |
| [dofus-unity-protocol-builder](https://github.com/LuaxY/dofus-unity-protocol-builder) | - | Dofus Unity protocol message builder |
| [Dofus RE Thesis (UPC 2022)](https://upcommons.upc.edu/bitstream/handle/2117/386396/TFM_Game_Hacking__Reverse_engineering_Dofus.pdf) | PDF | Most comprehensive HAAPI documentation source |

---

*Generated by RE Engineer - OtomAI Project*
*Date: 2026-03-29*
