"""
Schema registry: stores parsed proto schemas as versioned JSON snapshots.
Supports diffing between versions to detect Ankama protocol changes.
"""

import json
import hashlib
from datetime import datetime, timezone
from pathlib import Path
from dataclasses import asdict
from typing import Optional

from parser import MessageDef, FieldDef, EnumDef


def message_to_dict(msg: MessageDef) -> dict:
    """Convert a MessageDef to a serializable dict."""
    d = {
        "class_name": msg.class_name,
        "type_url": msg.type_url,
        "source_file": msg.source_file,
        "proto_input": msg.proto_input,
        "fields": [],
        "nested_messages": [],
        "enums": [],
        "oneof_groups": msg.oneof_groups,
    }
    for f in msg.fields:
        d["fields"].append({
            "number": f.number,
            "proto_name": f.proto_name,
            "csharp_name": f.csharp_name,
            "csharp_type": f.csharp_type,
            "required": f.required,
            "is_list": f.is_list,
            "is_oneof": f.is_oneof,
            "oneof_group": f.oneof_group,
            "oneof_discriminator": f.oneof_discriminator,
            "default_value": f.default_value,
        })
    for nm in msg.nested_messages:
        d["nested_messages"].append(message_to_dict(nm))
    for e in msg.enums:
        d["enums"].append({
            "name": e.name,
            "proto_name": e.proto_name,
            "values": [{"name": v.name, "proto_name": v.proto_name, "number": v.number} for v in e.values],
        })
    return d


def dict_to_message(d: dict) -> MessageDef:
    """Reconstruct a MessageDef from a dict."""
    msg = MessageDef(
        class_name=d["class_name"],
        type_url=d["type_url"],
        source_file=d["source_file"],
        proto_input=d["proto_input"],
        oneof_groups=d.get("oneof_groups", {}),
    )
    for fd in d.get("fields", []):
        msg.fields.append(FieldDef(
            number=fd["number"],
            proto_name=fd["proto_name"],
            csharp_name=fd["csharp_name"],
            csharp_type=fd["csharp_type"],
            required=fd.get("required", False),
            is_list=fd.get("is_list", False),
            is_oneof=fd.get("is_oneof", False),
            oneof_group=fd.get("oneof_group"),
            oneof_discriminator=fd.get("oneof_discriminator"),
            default_value=fd.get("default_value"),
        ))
    for nd in d.get("nested_messages", []):
        msg.nested_messages.append(dict_to_message(nd))
    for ed in d.get("enums", []):
        from parser import EnumValue
        enum_def = EnumDef(name=ed["name"], proto_name=ed["proto_name"])
        for ev in ed.get("values", []):
            enum_def.values.append(EnumValue(
                name=ev["name"], proto_name=ev["proto_name"], number=ev["number"]
            ))
        msg.enums.append(enum_def)
    return msg


def _dedup_messages(messages: list[MessageDef]) -> dict[str, dict]:
    """Build type_url -> message dict, keeping first occurrence for duplicate TypeUrls."""
    result = {}
    for msg in messages:
        if msg.type_url and msg.type_url not in result:
            result[msg.type_url] = message_to_dict(msg)
    return result


class SchemaRegistry:
    """Manages versioned proto schema snapshots."""

    def __init__(self, registry_dir: Path):
        self.registry_dir = registry_dir
        self.registry_dir.mkdir(parents=True, exist_ok=True)

    def save_snapshot(self, messages: list[MessageDef], version: Optional[str] = None,
                      game_version: Optional[str] = None) -> Path:
        """Save a schema snapshot. Returns the snapshot file path."""
        if version is None:
            version = datetime.now(timezone.utc).strftime("%Y%m%d_%H%M%S")

        snapshot = {
            "version": version,
            "game_version": game_version,
            "created_at": datetime.now(timezone.utc).isoformat(),
            "message_count": len(messages),
            "messages": _dedup_messages(messages),
        }

        # Compute content hash for dedup
        content_str = json.dumps(snapshot["messages"], sort_keys=True)
        snapshot["content_hash"] = hashlib.sha256(content_str.encode()).hexdigest()[:16]

        filepath = self.registry_dir / f"schema_{version}.json"
        filepath.write_text(json.dumps(snapshot, indent=2, ensure_ascii=False), encoding="utf-8")
        return filepath

    def load_snapshot(self, version: str) -> Optional[dict]:
        """Load a specific snapshot by version."""
        filepath = self.registry_dir / f"schema_{version}.json"
        if not filepath.exists():
            return None
        return json.loads(filepath.read_text(encoding="utf-8"))

    def latest_snapshot(self) -> Optional[dict]:
        """Load the most recent snapshot."""
        snapshots = sorted(self.registry_dir.glob("schema_*.json"), reverse=True)
        if not snapshots:
            return None
        return json.loads(snapshots[0].read_text(encoding="utf-8"))

    def list_versions(self) -> list[str]:
        """List all available snapshot versions."""
        return sorted(
            f.stem.replace("schema_", "")
            for f in self.registry_dir.glob("schema_*.json")
        )
