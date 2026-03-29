"""
Parse Bubble.D3.Bot generated C# protobuf files into a structured schema registry.

Extracts: class names, TypeUrl codes, fields (number, name, type, required),
nested classes, enums, and oneof groups from protobuf-net generated C# files.
"""

import re
import json
from pathlib import Path
from dataclasses import dataclass, field, asdict
from typing import Optional


@dataclass
class EnumValue:
    name: str
    proto_name: str
    number: int


@dataclass
class EnumDef:
    name: str
    proto_name: str  # e.g. "kbn"
    values: list[EnumValue] = field(default_factory=list)


@dataclass
class FieldDef:
    number: int
    proto_name: str  # obfuscated name from proto, e.g. "dzmt"
    csharp_name: str  # PascalCase C# name, e.g. "Dzmt"
    csharp_type: str  # e.g. "int", "long", "string", "List<TreasureHuntFlag>"
    required: bool = False
    is_list: bool = False
    is_oneof: bool = False
    oneof_group: Optional[str] = None
    oneof_discriminator: Optional[int] = None
    default_value: Optional[str] = None


@dataclass
class MessageDef:
    class_name: str  # human-readable, e.g. "PortalUseRequest"
    type_url: str  # short code, e.g. "hcx"
    source_file: str  # e.g. "Hcx.cs"
    proto_input: str  # e.g. "hcx.proto"
    fields: list[FieldDef] = field(default_factory=list)
    nested_messages: list["MessageDef"] = field(default_factory=list)
    enums: list[EnumDef] = field(default_factory=list)
    oneof_groups: dict[str, list[str]] = field(default_factory=dict)  # group_name -> field names


# Regex patterns for parsing
RE_CLASS = re.compile(
    r'\[global::ProtoBuf\.ProtoContract\((?:Name\s*=\s*@"(\w+)")?\)\]\s*'
    r'public\s+partial\s+class\s+(\w+)\s*:\s*global::ProtoBuf\.IExtensible,\s*IProtoMessage'
)
RE_TYPE_URL = re.compile(
    r'public\s+static\s+string\s+TypeUrl\s*=>\s*"([^"]+)"'
)
RE_PROTO_MEMBER = re.compile(
    r'\[global::ProtoBuf\.ProtoMember\((\d+),\s*Name\s*=\s*@"(\w+)"\)\]'
)
RE_PROPERTY = re.compile(
    r'public\s+(required\s+)?(global::System\.Collections\.Generic\.List<(\w+)>|[\w.]+)\s+(\w+)\s*\{\s*get;\s*set;\s*\}'
)
RE_ONEOF_PROPERTY = re.compile(
    r'public\s+([\w.]+)\s+(\w+)\s*\{\s*\n?\s*get\s*=>\s*__pbn__(\w+)\.Is\((\d+)\)'
)
RE_ENUM = re.compile(
    r'\[global::ProtoBuf\.ProtoContract\(Name\s*=\s*@"(\w+)"\)\]\s*'
    r'public\s+enum\s+(\w+)'
)
RE_ENUM_VALUE = re.compile(
    r'\[global::ProtoBuf\.ProtoEnum\(Name\s*=\s*@"(\w+)"\)\]\s*'
    r'(\w+)\s*=\s*(\d+)'
)
RE_INPUT_PROTO = re.compile(r'//\s*Input:\s*(\S+\.proto)')
RE_DEFAULT_VALUE = re.compile(
    r'\[global::System\.ComponentModel\.DefaultValue\(([^)]+)\)\]'
)


def parse_file(filepath: Path) -> list[MessageDef]:
    """Parse a single C# proto file and return all message definitions found."""
    content = filepath.read_text(encoding="utf-8")
    source_file = filepath.name

    # Extract proto input name
    m = RE_INPUT_PROTO.search(content)
    proto_input = m.group(1) if m else ""

    # Find the top-level class only (first one in file)
    cm = RE_CLASS.search(content)
    if not cm:
        return []

    class_name = cm.group(2)
    body, body_start_abs = _extract_brace_body(content, cm.end())
    if body is None:
        return []

    msg = _parse_single_class(body, class_name, source_file, proto_input)
    return [msg] if msg else []


def _extract_brace_body(text: str, start: int) -> tuple[Optional[str], int]:
    """Find the first { ... } block starting from `start`. Returns (body, abs_start)."""
    brace_count = 0
    body_start = None
    for j in range(start, len(text)):
        if text[j] == '{':
            if brace_count == 0:
                body_start = j + 1
            brace_count += 1
        elif text[j] == '}':
            brace_count -= 1
            if brace_count == 0:
                return text[body_start:j], body_start
    return None, start


def _get_own_body(body: str) -> str:
    """Return the class body with nested class/enum definitions replaced by blank lines.

    This ensures field/enum extraction only finds members at the current nesting level.
    """
    # Find nested class definitions and blank them out
    nested_pattern = re.compile(
        r'\[global::ProtoBuf\.ProtoContract\((?:Name\s*=\s*@"\w+")?\)\]\s*\n'
        r'\s*public\s+(?:partial\s+class|enum)\s+\w+',
        re.MULTILINE
    )

    result = body
    # Iteratively remove nested definitions from deepest to shallowest
    while True:
        matches = list(nested_pattern.finditer(result))
        if not matches:
            break
        # Process last match first (deepest nesting) to preserve positions
        replaced = False
        for m in reversed(matches):
            # Find the { ... } block after the match
            search_start = m.end()
            brace_count = 0
            block_start = None
            block_end = None
            for j in range(search_start, len(result)):
                if result[j] == '{':
                    if brace_count == 0:
                        block_start = j
                    brace_count += 1
                elif result[j] == '}':
                    brace_count -= 1
                    if brace_count == 0:
                        block_end = j + 1
                        break
            if block_start is not None and block_end is not None:
                # Replace the entire nested definition with whitespace (preserve line count)
                removed = result[m.start():block_end]
                replacement = '\n' * removed.count('\n')
                result = result[:m.start()] + replacement + result[block_end:]
                replaced = True
                break  # Re-scan after modification
        if not replaced:
            break

    return result


def _parse_single_class(body: str, class_name: str, source_file: str, proto_input: str) -> Optional[MessageDef]:
    """Parse a single class from its body content."""
    # Extract TypeUrl from full body (before stripping nested)
    type_url_m = RE_TYPE_URL.search(body)
    type_url = type_url_m.group(1) if type_url_m else ""

    msg = MessageDef(
        class_name=class_name,
        type_url=type_url,
        source_file=source_file,
        proto_input=proto_input,
    )

    # Get body with nested definitions stripped for field/enum extraction
    own_body = _get_own_body(body)

    # Extract fields and enums from own body only
    _extract_fields(own_body, msg)
    _extract_enums(own_body, msg)

    # Extract nested messages recursively from full body
    _extract_nested_messages(body, msg, source_file, proto_input)

    return msg


def _extract_fields(body: str, msg: MessageDef):
    """Extract field definitions from a class body."""
    lines = body.split('\n')
    pending_member = None
    pending_default = None

    for line in lines:
        # Check for DefaultValue attribute
        default_m = RE_DEFAULT_VALUE.search(line)
        if default_m:
            pending_default = default_m.group(1).strip('"').strip("'")
            continue

        # Check for ProtoMember attribute
        member_m = RE_PROTO_MEMBER.search(line)
        if member_m:
            pending_member = (int(member_m.group(1)), member_m.group(2))
            continue

        # Check for standard property (get; set;)
        if pending_member:
            prop_m = RE_PROPERTY.search(line)
            if prop_m:
                required = prop_m.group(1) is not None
                full_type = prop_m.group(2)
                list_type = prop_m.group(3)
                prop_name = prop_m.group(4)

                is_list = list_type is not None
                csharp_type = f"List<{list_type}>" if is_list else _clean_type(full_type)

                f = FieldDef(
                    number=pending_member[0],
                    proto_name=pending_member[1],
                    csharp_name=prop_name,
                    csharp_type=csharp_type,
                    required=required,
                    is_list=is_list,
                    default_value=pending_default,
                )
                msg.fields.append(f)
                pending_member = None
                pending_default = None
                continue

        # Check for oneof-style property (DiscriminatedUnionObject pattern)
        oneof_m = re.search(
            r'public\s+([\w.]+)\s+(\w+)\s*$', line.strip()
        )
        if not oneof_m and pending_member:
            # Try multi-line oneof detection
            if '__pbn__' in line and '.Is(' in line:
                disc_m = re.search(r'__pbn__(\w+)\.Is\((\d+)\)', line)
                if disc_m:
                    group_name = disc_m.group(1)
                    disc_num = int(disc_m.group(2))
                    # The pending_member has the field info
                    # Look backward for the property type/name
                    type_m = re.search(
                        r'public\s+([\w.]+)\s+(\w+)', lines[lines.index(line) - 1] if lines.index(line) > 0 else ""
                    )
                    if type_m and pending_member:
                        f = FieldDef(
                            number=pending_member[0],
                            proto_name=pending_member[1],
                            csharp_name=type_m.group(2),
                            csharp_type=_clean_type(type_m.group(1)),
                            required=False,
                            is_oneof=True,
                            oneof_group=group_name,
                            oneof_discriminator=disc_num,
                        )
                        msg.fields.append(f)
                        if group_name not in msg.oneof_groups:
                            msg.oneof_groups[group_name] = []
                        msg.oneof_groups[group_name].append(pending_member[1])
                        pending_member = None
                        pending_default = None

    # Second pass: detect oneof fields via the DiscriminatedUnion pattern
    oneof_pattern = re.compile(
        r'\[global::ProtoBuf\.ProtoMember\((\d+),\s*Name\s*=\s*@"(\w+)"\)\]\s*\n'
        r'\s*public\s+([\w.]+)\s+(\w+)\s*\{\s*\n'
        r'\s*get\s*=>\s*__pbn__(\w+)\.Is\((\d+)\)',
        re.MULTILINE
    )
    for om in oneof_pattern.finditer(body):
        field_num = int(om.group(1))
        proto_name = om.group(2)
        csharp_type = _clean_type(om.group(3))
        csharp_name = om.group(4)
        group_name = om.group(5)
        disc = int(om.group(6))

        # Don't duplicate if already found
        if any(f.number == field_num and f.proto_name == proto_name for f in msg.fields):
            continue

        f = FieldDef(
            number=field_num,
            proto_name=proto_name,
            csharp_name=csharp_name,
            csharp_type=csharp_type,
            required=False,
            is_oneof=True,
            oneof_group=group_name,
            oneof_discriminator=disc,
        )
        msg.fields.append(f)
        if group_name not in msg.oneof_groups:
            msg.oneof_groups[group_name] = []
        msg.oneof_groups[group_name].append(proto_name)


def _extract_enums(body: str, msg: MessageDef):
    """Extract enum definitions from a class body."""
    enum_pattern = re.compile(
        r'\[global::ProtoBuf\.ProtoContract\(Name\s*=\s*@"(\w+)"\)\]\s*\n'
        r'\s*public\s+enum\s+(\w+)\s*\{([^}]*)\}',
        re.MULTILINE | re.DOTALL
    )
    for em in enum_pattern.finditer(body):
        proto_name = em.group(1)
        enum_name = em.group(2)
        enum_body = em.group(3)

        enum_def = EnumDef(name=enum_name, proto_name=proto_name)
        for vm in RE_ENUM_VALUE.finditer(enum_body):
            enum_def.values.append(EnumValue(
                name=vm.group(2),
                proto_name=vm.group(1),
                number=int(vm.group(3)),
            ))
        msg.enums.append(enum_def)


def _extract_nested_messages(body: str, parent_msg: MessageDef, source_file: str, proto_input: str):
    """Extract nested message class definitions (immediate children only)."""
    # Find nested ProtoContract classes (not enums)
    nested_class_re = re.compile(
        r'\[global::ProtoBuf\.ProtoContract\((?:Name\s*=\s*@"(\w+)")?\)\]\s*\n'
        r'\s*public\s+partial\s+class\s+(\w+)\s*:\s*global::ProtoBuf\.IExtensible,\s*IProtoMessage',
        re.MULTILINE
    )

    # To find only immediate children (not deeply nested), track brace depth
    # relative to the parent body. Only process classes at depth 0.
    depth = 0
    i = 0
    while i < len(body):
        if body[i] == '{':
            depth += 1
            i += 1
            continue
        elif body[i] == '}':
            depth -= 1
            i += 1
            continue

        # Only look for nested classes at depth 0 (direct children of parent)
        if depth == 0:
            m = nested_class_re.match(body, i)
            if m:
                nested_class_name = m.group(2)
                nested_body, _ = _extract_brace_body(body, m.end())
                if nested_body is not None:
                    nested_msg = _parse_single_class(nested_body, nested_class_name, source_file, proto_input)
                    if nested_msg:
                        parent_msg.nested_messages.append(nested_msg)
                i = m.end()
                continue

        i += 1


def _clean_type(type_str: str) -> str:
    """Clean up C# type strings."""
    type_str = type_str.replace("global::System.Collections.Generic.List<", "List<")
    type_str = type_str.replace("global::", "")
    type_str = type_str.strip()
    if type_str.startswith("required "):
        type_str = type_str[9:]
    return type_str


def parse_directory(proto_dir: Path) -> list[MessageDef]:
    """Parse all C# proto files in a directory."""
    messages = []
    for cs_file in sorted(proto_dir.glob("*.cs")):
        try:
            file_msgs = parse_file(cs_file)
            messages.extend(file_msgs)
        except Exception as e:
            print(f"Warning: Failed to parse {cs_file.name}: {e}")
    return messages


def parse_mappings(mappings_path: Path) -> dict[str, str]:
    """Load game_mappings.json."""
    return json.loads(mappings_path.read_text(encoding="utf-8"))
