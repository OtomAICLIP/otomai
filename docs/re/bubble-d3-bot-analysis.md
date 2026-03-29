# Bubble.D3.Bot Repository Analysis

**Source**: [github.com/AlpaGit/Bubble.D3.Bot](https://github.com/AlpaGit/Bubble.D3.Bot)
**Date**: 2026-03-29
**Relevance**: Production-grade Dofus 3.0 (Unity) bot framework — directly relevant to our RE goals

## Overview

Bubble.D3.Bot is a .NET 9 multi-account bot framework for Dofus 3.0 by AlpaGit. It includes a full game client implementation with combat AI, treasure hunting, pathfinding, account creation, and multi-instance orchestration. The codebase contains ~137K LOC across 1,782 C# files.

## Architecture

```
BubbleBot.Cli (main bot runtime)
├── Bubble.Core (networking, DB, crypto, logging)
├── Bubble.Shared (protocol, Ankama API)
├── Bubble.DamageCalculation (combat math)
│   └── Bubble.Core.Datacenter (game data from Unity assets)
│       ├── Bubble.Core.Unity (asset extraction via AssetsTools.NET)
│       └── Bubble.SourceGenerators (codegen for protocol, ORM, services)
```

Utility projects: AccountCreation, Connect, Subscribe, ShieldDisable, ProxyAssociate, Split, FrigostAdapter.

## 1. Network Protocol (Critical for our RE)

### Transport
- **Raw TCP sockets** with SOCKS5 proxy support
- IPv4/IPv6 dual-mode
- Keep-alive with 30-second ping interval

### Serialization: Protocol Buffers 3
- Uses **protobuf-net** (Marc Gravell's library)
- Message framing: `[VarInt32 length][Protobuf data]`
- **1,344 auto-generated protobuf message classes** in `libs/Bubble.Shared/Protocol/`

### Message Envelope
All messages wrapped in `GameMessage` (file: `libs/Bubble.Shared/Protocol/Game/Hco.cs`):
```
GameMessage {
  oneof Content {
    Event event = 1;    // Server-pushed events
    Request request = 2; // Client requests
    Response response = 4; // Server responses
  }
}
```

Each variant contains:
- `Uid` (int): Request/response correlation (-1 for events)
- `Content.TypeUrl`: `"type.ankama.com/{shortCode}"` (e.g., "iwv" for IdentificationRequest)
- `Content.Value`: Serialized protobuf bytes

### TypeUrl System
Messages use short 3-letter codes (e.g., "iwv", "iws", "jpn", "hdo") mapped to full type URLs. These are likely obfuscated class names from the Unity client.

### Key Protocol Messages
| TypeUrl | Class | Purpose |
|---------|-------|---------|
| `iwv` | IdentificationRequest | Login with ticket_key + language |
| `iws` | PingRequest | Keep-alive (quiet flag) |
| `jpn` | DateRequest | Time sync |
| `hdo` | TreasureHuntEvent | Hunt state with checkpoints/steps |

### Source Generators (Important Pattern)
- **MessageDispatcher**: Scans `[MessageHandler]` methods, generates switch-based routing by ContentCase + TypeUrl, auto-deserializes protobuf
- **MessageFactory**: Generates `Create(typeUrl, bytes)` factory for dynamic message instantiation
- **DatacenterSourceGenerator**: Generates game data object factories
- **DatabaseSourceGenerator**: ORM codegen for PostgreSQL

## 2. Authentication Flow

### OAuth2 PKCE (Same as our findings)
1. Generate PKCE code_verifier + SHA256 code_challenge
2. `GET https://auth.ankama.com/login/ankama` with client_id=102, redirect_uri=zaap://login
3. Extract `state` from response HTML
4. Submit credentials to `/login/ankama/form` with AWS WAF token cookie
5. Extract auth code from redirect
6. Exchange code for access_token + refresh_token at `/token`
7. User-Agent: `Zaap 3.12.21`

### HAAPI Integration
- Account info: `GET https://haapi.ankama.com/json/Ankama/v5/Account/Account` with apikey header

### Game Server Auth
1. Send `IdentificationRequest` with ticket_key (OAuth token)
2. Server responds with character list
3. Select character, enter world

## 3. AWS WAF Bypass

Two implementations found (ShieldDisable + AccountCreation):

### Custom Solver (AwsBypassService)
- Hits challenge endpoint: `https://3f38f7f4f368.83dbb5dc.eu-south-1.token.awswaf.com/.../inputs`
- Solves proof-of-work challenges:
  - **Type 1**: Scrypt-based hashing with HMAC
  - **Type 2**: SHA256 computational proof
- Spoofs browser fingerprint metrics (Canvas hash, WebGL, Chrome 129, plugin detection)
- Uses AES-GCM encryption with hardcoded key + identifier "KramerAndRio"
- Injects `aws-waf-token` cookie on `auth.ankama.com`

### Capsolver Fallback (CaptchaService)
- Task type: `AntiAwsWafTaskProxyLess`
- Extracts awsKey, awsIv, awsContext from `window.gokuProps` in registration page
- Polls for solution with 5s interval, 100s timeout

## 4. Game Data Extraction from Unity

### Datacenter System
Game data loaded from Unity asset bundles at runtime:
- `data_assets_itemsroot.asset.bundle` -> Items
- `data_assets_spellsroot.asset.bundle` -> Spells, SpellLevels, SpellStates
- World graph serialization -> Pathfinding edges

Uses **AssetsTools.NET** + **AssetsTools.NET.Cpp2IL** for Unity asset parsing and **AssetRipper.TextureDecoder** for textures.

### Key Data Structures
| Type | Fields | Source |
|------|--------|--------|
| Items | Id, TypeId, Level, Name, RecipeIds, PossibleEffectRids | Asset bundle |
| Spells | Id, TypeId, SpellLevels, BasePreviewZoneDescr | Asset bundle |
| SpellLevels | Effects, CriticalEffects, Range, ApCost | Asset bundle |
| Monsters | Id, Grades(Level,HP,Resistances), Drops, Spells | Asset bundle |
| MapPositions | Id, PosX, PosY, SubAreaId, WorldMap, Capability flags | Asset bundle |
| MapData | Id, Cells(560), InteractiveElements, Actors, Adjacent maps | JSON/binary |
| WorldGraphEdge | From(MapId,ZoneId) -> To(MapId,ZoneId), Transitions | Serialized |

### Cell Model (560 cells per map)
```
Cell { Id, Speed, LinkedZone, Mov(walkable), Los(line-of-sight),
       NonWalkableDuringFight, FarmCell, Red/Blue(team zones), Arrow }
LinkedZoneRp = (LinkedZone & 0xF0) >> 4
```

## 5. Pathfinding

### World-Level: A* on WorldGraph
- Vertices: (MapId, ZoneId, Uid)
- Edges: Transitions between map zones
- Async callback-based with fallback to alternative linked zones

### Map-Level: A* on Cell Grid
- 8-directional movement (cardinal + diagonal)
- Search limit: 500 nodes
- Heuristic: Manhattan distance
- Entity-blocked cells: +20 cost penalty
- Movement velocities: Walk(480), Run(170), Mount(135) per cell

## 6. Combat AI

### 9-Step Turn Pipeline
```
NotStarted -> CastSummon -> CastBoost -> CastSpell -> CastSpell2
-> Move -> ReCastAfterMove -> ReCastAfterMove2 -> Move2 -> ReCastAfterMove3
```

### Fight State
- `FightInfo`: 1300+ lines, tracks actors, turn queue, AI step, preparation phase
- `FightActor`: ActorId, CellId, Level, IsAlly/IsEnemy, Effects, Stats (AP/MP/Resistances)
- `SpellWrapper`: Cooldowns, casts/turn, range, AoE zone shapes
- Auto-surrender after turn 40
- `DamageCalculationTranslator`: Bridges game stats to damage engine

### Damage Calculation Library (43K LOC)
Full spell effect simulation with fighter state management, effect durations, resistance calculations.

## 7. Bot Orchestration

### Multi-Account
- `BotManager`: ConcurrentDictionary of BotClient instances
- Per-account proxy resolution (SOCKS5)
- Spectre.Console live dashboard for monitoring
- Health monitoring: 35s message timeout, 5-min movement timeout

### Operational Modes
- **Treasure Hunt**: State machine with clue solving, anti-AFK (20s timeout, escalating response)
- **Trajet (Farm Route)**: JSON-defined waypoints with monster filters, auto-fight
- **Bank**: Auto-deposit inventory
- **Koli (PvP Arena)**: Dedicated client (BotKoliClient, 10K+ lines)

### Scaling
- `BubbleBot.Split`: Shards accounts into batches of 25, spawns separate processes
- 1-minute reconnection backoff
- Hardware ID randomization per account

## 8. Account Management Tools

### AccountCreation Flow
1. AWS WAF bypass (custom solver or Capsolver fallback)
2. Disposable email via Kopeechka.store API
3. Register with random names/birthdate
4. Email confirmation (6-digit code extraction)
5. Enable then disable Ankama Shield 2FA
6. Save credentials

### ShieldDisable
- Logs into account security page
- Enables 2FA, registers device
- Immediately disables 2FA
- Uses email code extraction from Kopeechka

## Key Takeaways for OtomAI

### Directly Reusable
1. **Protocol structure**: GameMessage envelope with TypeUrl routing is confirmed Dofus 3.0 architecture
2. **Protobuf serialization**: VarInt32 length-prefixed frames over TCP
3. **1,344 message definitions**: Complete protocol coverage (though names are obfuscated)
4. **OAuth2 PKCE flow**: Matches our existing auth spec exactly (client_id=102, Zaap UA)
5. **AWS WAF bypass approach**: Custom proof-of-work solver pattern
6. **Unity asset extraction**: AssetsTools.NET pipeline for game data
7. **Cell/map model**: 560 cells, LinkedZone encoding, capability flags
8. **World graph pathfinding**: A* on vertex/edge model

### Architecture Patterns to Adopt
1. **Source generators** for message dispatch/factory — eliminates boilerplate
2. **Event-driven network + state-machine gameplay** separation
3. **GameRuntimeState** as single state container (85 properties)
4. **Per-account proxy isolation** with SOCKS5

### Gaps / Outdated Elements
1. Windows-only (reads from `%AppData%\zaap\`) — we need cross-platform
2. No memory manipulation or client hooking — pure network bot
3. Obfuscated TypeUrl codes need mapping to semantic names
4. No anti-cheat analysis beyond network-level proxy/HWID rotation
5. Combat AI is relatively simple (fixed step pipeline) — room for improvement
