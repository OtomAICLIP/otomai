#!/usr/bin/env python3
"""
proto-sync: Automated protobuf schema synchronization for OtomAI.

Parses Bubble.D3.Bot generated C# proto files, maintains a versioned schema
registry, diffs changes between game versions, and regenerates OtomAI-compatible
C# message classes and game_mappings.json.

Usage:
    python main.py snapshot [--source DIR] [--version VER] [--game-version VER]
    python main.py diff [--old VER] [--new VER]
    python main.py generate [--version VER] [--output DIR] [--namespace NS]
    python main.py sync [--source DIR] [--output DIR] [--mappings-out PATH]
    python main.py status
"""

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

# Resolve imports relative to this script
sys.path.insert(0, str(Path(__file__).parent))

from parser import parse_directory, parse_mappings
from registry import SchemaRegistry, message_to_dict
from diff import diff_snapshots
from codegen import generate_all, generate_mappings


# Default paths (relative to repo root)
REPO_ROOT = Path(__file__).resolve().parent.parent.parent
BUBBLE_PROTO_DIR = REPO_ROOT / "vendor" / "Bubble.D3.Bot" / "libs" / "Bubble.Shared" / "Protocol" / "Game"
BUBBLE_MAPPINGS = REPO_ROOT / "vendor" / "Bubble.D3.Bot" / "BubbleBot.Cli" / "Data" / "game_mappings.json"
REGISTRY_DIR = Path(__file__).resolve().parent / "registry"
OTOMAI_PROTO_DIR = REPO_ROOT / "src" / "libs" / "OtomAI.Protocol" / "Messages" / "Game"
OTOMAI_MAPPINGS = REPO_ROOT / "src" / "libs" / "OtomAI.Protocol" / "Data" / "game_mappings.json"


def cmd_snapshot(args):
    """Parse source C# files and save a schema snapshot."""
    source_dir = Path(args.source) if args.source else BUBBLE_PROTO_DIR
    if not source_dir.exists():
        print(f"Error: Source directory not found: {source_dir}")
        return 1

    print(f"Parsing C# proto files from: {source_dir}")
    messages = parse_directory(source_dir)
    print(f"  Found {len(messages)} message definitions")

    # Count top-level (non-dot TypeUrl) messages
    top_level = [m for m in messages if not m.type_url.startswith(".")]
    print(f"  Top-level messages: {len(top_level)}")

    registry = SchemaRegistry(REGISTRY_DIR)
    filepath = registry.save_snapshot(
        messages,
        version=args.version,
        game_version=args.game_version,
    )
    print(f"  Snapshot saved: {filepath}")

    # Also load existing mappings if available
    if BUBBLE_MAPPINGS.exists():
        mappings = parse_mappings(BUBBLE_MAPPINGS)
        print(f"  Reference mappings: {len(mappings)} entries")

    return 0


def cmd_diff(args):
    """Diff two schema snapshots."""
    registry = SchemaRegistry(REGISTRY_DIR)
    versions = registry.list_versions()

    if not versions:
        print("No snapshots in registry. Run 'snapshot' first.")
        return 1

    if args.old and args.new:
        old_snap = registry.load_snapshot(args.old)
        new_snap = registry.load_snapshot(args.new)
    elif len(versions) >= 2:
        old_snap = registry.load_snapshot(versions[-2])
        new_snap = registry.load_snapshot(versions[-1])
    else:
        print("Need at least 2 snapshots to diff (or specify --old and --new).")
        return 1

    if not old_snap or not new_snap:
        print("Error: Could not load one or both snapshots.")
        return 1

    report = diff_snapshots(old_snap, new_snap)
    print(report.to_markdown())
    return 0


def cmd_generate(args):
    """Generate C# files from a schema snapshot."""
    registry = SchemaRegistry(REGISTRY_DIR)

    if args.version:
        snap = registry.load_snapshot(args.version)
    else:
        snap = registry.latest_snapshot()

    if not snap:
        print("No snapshot found. Run 'snapshot' first.")
        return 1

    output_dir = Path(args.output) if args.output else OTOMAI_PROTO_DIR
    namespace = args.namespace
    timestamp = datetime.now(timezone.utc).isoformat()

    print(f"Generating C# files from snapshot {snap['version']}")
    messages = snap.get("messages", {})
    generated = generate_all(messages, output_dir, timestamp, namespace)
    print(f"  Generated {len(generated)} files in: {output_dir}")

    # Generate mappings
    mappings = generate_mappings(messages)
    mappings_out = Path(args.mappings_out) if args.mappings_out else OTOMAI_MAPPINGS
    mappings_out.parent.mkdir(parents=True, exist_ok=True)
    mappings_out.write_text(json.dumps(mappings, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"  Generated mappings ({len(mappings)} entries): {mappings_out}")

    return 0


def cmd_sync(args):
    """Full sync: snapshot → generate → update mappings."""
    source_dir = Path(args.source) if args.source else BUBBLE_PROTO_DIR
    output_dir = Path(args.output) if args.output else OTOMAI_PROTO_DIR

    if not source_dir.exists():
        print(f"Error: Source directory not found: {source_dir}")
        return 1

    # Step 1: Parse and snapshot
    print("=== Step 1: Parsing source proto files ===")
    messages = parse_directory(source_dir)
    top_level = [m for m in messages if not m.type_url.startswith(".")]
    print(f"  Parsed {len(top_level)} top-level messages ({len(messages)} total)")

    registry = SchemaRegistry(REGISTRY_DIR)
    old_snap = registry.latest_snapshot()

    version = datetime.now(timezone.utc).strftime("%Y%m%d_%H%M%S")
    filepath = registry.save_snapshot(messages, version=version, game_version=args.game_version)
    new_snap = registry.load_snapshot(version)
    print(f"  Snapshot saved: {filepath}")

    # Step 2: Diff (if previous snapshot exists)
    if old_snap:
        print("\n=== Step 2: Diffing against previous snapshot ===")
        report = diff_snapshots(old_snap, new_snap)
        if report.has_changes:
            print(report.to_markdown())
            # Save diff report
            diff_path = REGISTRY_DIR / f"diff_{old_snap['version']}_to_{version}.md"
            diff_path.write_text(report.to_markdown(), encoding="utf-8")
            print(f"  Diff report saved: {diff_path}")
        else:
            print("  No changes detected.")
    else:
        print("\n=== Step 2: No previous snapshot (first run) ===")

    # Step 3: Generate C# files
    print("\n=== Step 3: Generating C# files ===")
    timestamp = datetime.now(timezone.utc).isoformat()
    generated = generate_all(new_snap["messages"], output_dir, timestamp, args.namespace)
    print(f"  Generated {len(generated)} files in: {output_dir}")

    # Step 4: Update mappings
    print("\n=== Step 4: Updating game_mappings.json ===")
    mappings = generate_mappings(new_snap["messages"])
    mappings_out = Path(args.mappings_out) if args.mappings_out else OTOMAI_MAPPINGS
    mappings_out.parent.mkdir(parents=True, exist_ok=True)
    mappings_out.write_text(json.dumps(mappings, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"  Updated mappings ({len(mappings)} entries): {mappings_out}")

    print("\n=== Sync complete ===")
    return 0


def cmd_status(args):
    """Show registry status."""
    registry = SchemaRegistry(REGISTRY_DIR)
    versions = registry.list_versions()

    print(f"Registry: {REGISTRY_DIR}")
    print(f"Snapshots: {len(versions)}")

    if versions:
        latest = registry.latest_snapshot()
        print(f"Latest: {versions[-1]}")
        print(f"  Messages: {latest.get('message_count', '?')}")
        print(f"  Hash: {latest.get('content_hash', '?')}")
        print(f"  Game version: {latest.get('game_version', 'unknown')}")

    # Check source availability
    print(f"\nBubble.D3.Bot source: {'✓' if BUBBLE_PROTO_DIR.exists() else '✗'} {BUBBLE_PROTO_DIR}")
    print(f"Bubble.D3.Bot mappings: {'✓' if BUBBLE_MAPPINGS.exists() else '✗'} {BUBBLE_MAPPINGS}")
    print(f"OtomAI output dir: {'✓' if OTOMAI_PROTO_DIR.exists() else '✗'} {OTOMAI_PROTO_DIR}")
    print(f"OtomAI mappings: {'✓' if OTOMAI_MAPPINGS.exists() else '✗'} {OTOMAI_MAPPINGS}")

    return 0


def main():
    parser = argparse.ArgumentParser(
        description="proto-sync: Automated protobuf schema synchronization for OtomAI",
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    # snapshot
    snap = subparsers.add_parser("snapshot", help="Parse C# proto files and save a schema snapshot")
    snap.add_argument("--source", help="Source directory with C# proto files")
    snap.add_argument("--version", help="Snapshot version label")
    snap.add_argument("--game-version", help="Dofus game version (e.g. 3.5.8.9)")

    # diff
    d = subparsers.add_parser("diff", help="Diff two schema snapshots")
    d.add_argument("--old", help="Old snapshot version")
    d.add_argument("--new", help="New snapshot version")

    # generate
    gen = subparsers.add_parser("generate", help="Generate C# files from a snapshot")
    gen.add_argument("--version", help="Snapshot version to generate from")
    gen.add_argument("--output", help="Output directory for C# files")
    gen.add_argument("--namespace", help="C# namespace for generated files")
    gen.add_argument("--mappings-out", help="Path for generated game_mappings.json")

    # sync
    s = subparsers.add_parser("sync", help="Full sync: snapshot + diff + generate + update mappings")
    s.add_argument("--source", help="Source directory with C# proto files")
    s.add_argument("--output", help="Output directory for C# files")
    s.add_argument("--namespace", help="C# namespace for generated files")
    s.add_argument("--mappings-out", help="Path for generated game_mappings.json")
    s.add_argument("--game-version", help="Dofus game version")

    # status
    subparsers.add_parser("status", help="Show registry status")

    args = parser.parse_args()

    commands = {
        "snapshot": cmd_snapshot,
        "diff": cmd_diff,
        "generate": cmd_generate,
        "sync": cmd_sync,
        "status": cmd_status,
    }

    return commands[args.command](args)


if __name__ == "__main__":
    sys.exit(main() or 0)
