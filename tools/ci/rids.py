"""Keep the shared CI matrix in sync with the runtime project's accepted RIDs."""

import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[2]


def matrix():
    rows = json.loads((ROOT / "tools/ci/rids.json").read_text())
    project = ET.parse(ROOT / "Electron2D.csproj")
    accepted = {}
    for item in project.findall("./PropertyGroup/Electron2DNativePlatform"):
        for rid in re.findall(r"'\$\(RuntimeIdentifier\)' == '([^']+)'", item.get("Condition", "")):
            accepted[rid] = item.text
    actual = {row["rid"]: row["platform"] for row in rows}
    if len(actual) != len(rows) or actual != accepted:
        raise ValueError(f"CI RID/platform mapping differs from the runtime project: {actual} != {accepted}")
    for row in rows:
        if row["suite"] not in {"full", "portable", "android", "apple-simulator", "apple-device", "browser"}:
            raise ValueError(f"Unknown test suite: {row}")
    return rows


if __name__ == "__main__":
    print("matrix=" + json.dumps({"include": matrix()}, separators=(",", ":")))
