"""
Diff engine: compare two schema snapshots and report changes.

Detects: added/removed messages, added/removed/changed fields,
type changes, field renumbering, and enum changes.
"""

from dataclasses import dataclass, field
from typing import Optional


@dataclass
class FieldChange:
    type_url: str
    class_name: str
    field_proto_name: str
    change_type: str  # "added", "removed", "type_changed", "number_changed", "required_changed"
    old_value: Optional[str] = None
    new_value: Optional[str] = None


@dataclass
class MessageChange:
    type_url: str
    class_name: str
    change_type: str  # "added", "removed", "renamed"
    old_class_name: Optional[str] = None
    field_changes: list[FieldChange] = field(default_factory=list)


@dataclass
class DiffReport:
    old_version: str
    new_version: str
    added_messages: list[MessageChange] = field(default_factory=list)
    removed_messages: list[MessageChange] = field(default_factory=list)
    modified_messages: list[MessageChange] = field(default_factory=list)
    renamed_messages: list[MessageChange] = field(default_factory=list)

    @property
    def has_changes(self) -> bool:
        return bool(self.added_messages or self.removed_messages or
                    self.modified_messages or self.renamed_messages)

    @property
    def summary(self) -> str:
        parts = []
        if self.added_messages:
            parts.append(f"{len(self.added_messages)} added")
        if self.removed_messages:
            parts.append(f"{len(self.removed_messages)} removed")
        if self.modified_messages:
            parts.append(f"{len(self.modified_messages)} modified")
        if self.renamed_messages:
            parts.append(f"{len(self.renamed_messages)} renamed")
        return ", ".join(parts) if parts else "no changes"

    def to_markdown(self) -> str:
        lines = [
            f"# Proto Diff: {self.old_version} → {self.new_version}",
            f"**Summary:** {self.summary}",
            "",
        ]

        if self.added_messages:
            lines.append("## Added Messages")
            for mc in self.added_messages:
                lines.append(f"- `{mc.type_url}` → **{mc.class_name}**")
            lines.append("")

        if self.removed_messages:
            lines.append("## Removed Messages")
            for mc in self.removed_messages:
                lines.append(f"- `{mc.type_url}` → ~~{mc.class_name}~~")
            lines.append("")

        if self.renamed_messages:
            lines.append("## Renamed Messages")
            for mc in self.renamed_messages:
                lines.append(f"- `{mc.type_url}`: {mc.old_class_name} → **{mc.class_name}**")
            lines.append("")

        if self.modified_messages:
            lines.append("## Modified Messages")
            for mc in self.modified_messages:
                lines.append(f"### `{mc.type_url}` ({mc.class_name})")
                for fc in mc.field_changes:
                    if fc.change_type == "added":
                        lines.append(f"  - **+** field `{fc.field_proto_name}` ({fc.new_value})")
                    elif fc.change_type == "removed":
                        lines.append(f"  - **-** field `{fc.field_proto_name}` ({fc.old_value})")
                    elif fc.change_type == "type_changed":
                        lines.append(f"  - **~** field `{fc.field_proto_name}`: type {fc.old_value} → {fc.new_value}")
                    elif fc.change_type == "number_changed":
                        lines.append(f"  - **~** field `{fc.field_proto_name}`: number {fc.old_value} → {fc.new_value}")
                    elif fc.change_type == "required_changed":
                        lines.append(f"  - **~** field `{fc.field_proto_name}`: required {fc.old_value} → {fc.new_value}")
            lines.append("")

        return "\n".join(lines)


def diff_snapshots(old_snap: dict, new_snap: dict) -> DiffReport:
    """Compare two schema snapshots and produce a diff report."""
    old_msgs = old_snap.get("messages", {})
    new_msgs = new_snap.get("messages", {})
    old_version = old_snap.get("version", "unknown")
    new_version = new_snap.get("version", "unknown")

    report = DiffReport(old_version=old_version, new_version=new_version)

    old_urls = set(old_msgs.keys())
    new_urls = set(new_msgs.keys())

    # Added messages
    for url in sorted(new_urls - old_urls):
        msg = new_msgs[url]
        report.added_messages.append(MessageChange(
            type_url=url,
            class_name=msg["class_name"],
            change_type="added",
        ))

    # Removed messages
    for url in sorted(old_urls - new_urls):
        msg = old_msgs[url]
        report.removed_messages.append(MessageChange(
            type_url=url,
            class_name=msg["class_name"],
            change_type="removed",
        ))

    # Check for renamed (same type_url, different class_name) and modified
    for url in sorted(old_urls & new_urls):
        old_msg = old_msgs[url]
        new_msg = new_msgs[url]

        if old_msg["class_name"] != new_msg["class_name"]:
            report.renamed_messages.append(MessageChange(
                type_url=url,
                class_name=new_msg["class_name"],
                change_type="renamed",
                old_class_name=old_msg["class_name"],
            ))

        # Diff fields
        field_changes = _diff_fields(url, old_msg, new_msg)
        if field_changes:
            report.modified_messages.append(MessageChange(
                type_url=url,
                class_name=new_msg["class_name"],
                change_type="modified",
                field_changes=field_changes,
            ))

    return report


def _diff_fields(type_url: str, old_msg: dict, new_msg: dict) -> list[FieldChange]:
    """Diff the fields between two message versions."""
    changes = []
    class_name = new_msg["class_name"]

    old_fields = {f["proto_name"]: f for f in old_msg.get("fields", [])}
    new_fields = {f["proto_name"]: f for f in new_msg.get("fields", [])}

    # Added fields
    for name in sorted(set(new_fields) - set(old_fields)):
        nf = new_fields[name]
        changes.append(FieldChange(
            type_url=type_url,
            class_name=class_name,
            field_proto_name=name,
            change_type="added",
            new_value=f"{nf['csharp_type']} (#{nf['number']})",
        ))

    # Removed fields
    for name in sorted(set(old_fields) - set(new_fields)):
        of = old_fields[name]
        changes.append(FieldChange(
            type_url=type_url,
            class_name=class_name,
            field_proto_name=name,
            change_type="removed",
            old_value=f"{of['csharp_type']} (#{of['number']})",
        ))

    # Changed fields
    for name in sorted(set(old_fields) & set(new_fields)):
        of = old_fields[name]
        nf = new_fields[name]

        if of["csharp_type"] != nf["csharp_type"]:
            changes.append(FieldChange(
                type_url=type_url,
                class_name=class_name,
                field_proto_name=name,
                change_type="type_changed",
                old_value=of["csharp_type"],
                new_value=nf["csharp_type"],
            ))

        if of["number"] != nf["number"]:
            changes.append(FieldChange(
                type_url=type_url,
                class_name=class_name,
                field_proto_name=name,
                change_type="number_changed",
                old_value=str(of["number"]),
                new_value=str(nf["number"]),
            ))

        if of.get("required") != nf.get("required"):
            changes.append(FieldChange(
                type_url=type_url,
                class_name=class_name,
                field_proto_name=name,
                change_type="required_changed",
                old_value=str(of.get("required")),
                new_value=str(nf.get("required")),
            ))

    return changes
