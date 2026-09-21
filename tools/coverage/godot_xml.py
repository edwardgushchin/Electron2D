#!/usr/bin/env python3
"""Export the declared Godot class reference API from a pinned source checkout.

Usage: python3 tools/coverage/godot_xml.py GODOT_CHECKOUT OUTPUT.json
"""

import json
import subprocess
import sys
from pathlib import Path
from xml.etree import ElementTree


SECTIONS = {
    "constructors": ("constructor", "constructor"),
    "methods": ("method", "method"),
    "signals": ("signal", "signal"),
    "members": ("member", "property"),
    "constants": ("constant", "constant"),
    "annotations": ("annotation", "annotation"),
    "theme_items": ("theme_item", "theme_item"),
    "operators": ("operator", "operator"),
}
CALLABLES = {"constructor", "method", "signal", "annotation", "operator"}


def git(checkout: Path, *args: str) -> str:
    return subprocess.check_output(
        ["git", "-C", str(checkout), *args], text=True, stderr=subprocess.DEVNULL
    ).strip()


def signature(kind: str, name: str, attributes: dict) -> str:
    if kind not in CALLABLES:
        declared_type = attributes.get("type")
        display = f"{declared_type} {name}" if declared_type else name
        if "enum" in attributes and kind != "enum":
            display += f" [{attributes['enum']}]"
        if "default" in attributes:
            display += f" = {attributes['default']}"
        elif "value" in attributes:
            display += f" = {attributes['value']}"
        return display
    parameters = []
    for param in sorted(attributes["params"], key=lambda p: int(p["index"])):
        value = f"{param['type']} {param['name']}"
        if "enum" in param:
            value += f" [{param['enum']}]"
        if "default" in param:
            value += f" = {param['default']}"
        parameters.append(value)
    returns = attributes["return"]
    result = returns.get("type", "void")
    if "enum" in returns:
        result += f" [{returns['enum']}]"
    return f"{name}({', '.join(parameters)}) -> {result}"


def member_id(owner: str, kind: str, name: str, attributes: dict) -> str:
    if kind in CALLABLES:
        parameter_types = ",".join(
            f"{param['type']}:{param.get('enum', '')}"
            for param in sorted(attributes["params"], key=lambda p: int(p["index"]))
        )
        name += f"({parameter_types})"
    elif kind == "enum_value":
        name = f"{attributes['enum']}.{name}"
    return f"{owner}::{kind}:{name}"


def export(checkout: Path) -> dict:
    paths = sorted(
        [*checkout.glob("doc/classes/*.xml"),
         *checkout.glob("modules/*/doc_classes/*.xml"),
         *checkout.glob("platform/*/doc_classes/*.xml")],
        key=lambda path: path.relative_to(checkout).as_posix(),
    )
    if not paths:
        raise ValueError(f"No Godot class XML files in {checkout}")

    types = []
    type_ids = set()
    for path in paths:
        root = ElementTree.parse(path).getroot()
        if root.tag != "class" or not root.get("name"):
            raise ValueError(f"Invalid class XML: {path}")
        name = root.attrib["name"]
        if name in type_ids:
            raise ValueError(f"Duplicate class: {name}")
        type_ids.add(name)
        source = path.relative_to(checkout).as_posix()
        members = []
        member_ids = set()

        def add(kind: str, member_name: str, attributes: dict) -> None:
            display = signature(kind, member_name, attributes)
            identifier = member_id(name, kind, member_name, attributes)
            if identifier in member_ids:
                raise ValueError(f"Duplicate member: {identifier} in {source}")
            member_ids.add(identifier)
            members.append({"id": identifier, "kind": kind, "name": member_name,
                            "signature": display, "attributes": attributes, "source": source})

        for section in root:
            if section.tag in {"brief_description", "description", "tutorials"}:
                continue
            if section.tag not in SECTIONS:
                raise ValueError(f"Unknown class section {section.tag} in {source}")
            child_tag, kind = SECTIONS[section.tag]
            for member in section:
                if member.tag != child_tag:
                    raise ValueError(f"Unknown {section.tag} child {member.tag} in {source}")
                member_name = member.get("name")
                if not member_name:
                    raise ValueError(f"Unnamed {kind} in {source}")
                attributes = dict(member.attrib)
                if kind in CALLABLES:
                    attributes["return"] = dict(member.find("return").attrib) if member.find("return") is not None else {}
                    attributes["params"] = [dict(param.attrib) for param in member.findall("param")]
                    attributes["returns_error"] = [dict(error.attrib) for error in member.findall("returns_error")]
                    extra = {child.tag for child in member} - {"return", "param", "description", "returns_error"}
                    if extra:
                        raise ValueError(f"Unknown {kind} children {extra} in {source}")
                member_kind = "enum_value" if kind == "constant" and "enum" in attributes else kind
                add(member_kind, member_name, attributes)

        enum_values = [item for item in members if item["kind"] == "enum_value"]
        enums = sorted({item["attributes"]["enum"] for item in enum_values})
        for enum_name in enums:
            add("enum", enum_name, {"name": enum_name, "is_bitfield": any(
                item["attributes"].get("is_bitfield") == "true"
                for item in enum_values if item["attributes"]["enum"] == enum_name
            )})
        types.append({"id": f"class:{name}", "name": name, "inherits": root.get("inherits"),
                      "api_type": root.get("api_type"), "attributes": dict(root.attrib),
                      "source": source, "members": sorted(members, key=lambda item: item["id"])})

    return {"godot_version": git(checkout, "describe", "--tags", "--exact-match"),
            "godot_commit": git(checkout, "rev-parse", "HEAD"),
            "types": sorted(types, key=lambda item: item["name"])}


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit(__doc__)
    manifest = export(Path(sys.argv[1]).resolve())
    Path(sys.argv[2]).write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
