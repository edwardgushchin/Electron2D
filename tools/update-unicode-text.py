#!/usr/bin/env python3
"""Restore the pinned, mechanically adapted Unicode source closure and test fixture."""

import argparse
import hashlib
import json
from pathlib import Path
import re
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
VENDOR = ROOT / "src/Vendor/UnicodeText"
MANIFEST = VENDOR / "manifest.json"


def digest(data):
    return hashlib.sha256(data).hexdigest()


def adapt(data):
    text = data.decode("utf-8-sig").replace("\r\n", "\n")
    text = text.replace("Avalonia.Media.TextFormatting", "Electron2D.TextFormatting")
    text = text.replace("Avalonia.Utilities", "Electron2D.TextFormatting.Utilities")
    return re.sub(r"(?m)^    public (?=(?:(?:readonly|ref|static|sealed|partial) )*(?:class|struct|enum|record)\b)", "    internal ", text).encode()


def choose(cases, limit):
    if limit is None or len(cases) <= limit:
        return cases
    indices = set(range(64)) | set(range(len(cases) - 64, len(cases)))
    indices.update(round(index * (len(cases) - 1) / (limit - 129)) for index in range(limit - 128))
    return [cases[index] for index in sorted(indices)]


def fixture(manifest, downloads, full=False):
    result = {"unicodeVersion": manifest["unicodeVersion"], "upstreamCommit": manifest["commit"], "sources": [], "selection": "All grapheme/word/line-break cases; first/last64 plus evenly spaced bidi cases. No expected results changed."}
    keys = {"GraphemeBreakTest.txt": "grapheme", "LineBreakTest.txt": "lineBreak", "WordBreakTest.txt": "wordBreak", "BidiCharacterTest.txt": "bidiCharacters", "BidiTest.txt": "bidiClasses"}
    for source in manifest["conformance"]:
        lines = downloads[source["url"]].decode("utf-8-sig").splitlines()
        cases = []
        levels = order = ""
        for number, line in enumerate(lines, 1):
            data = line.split("#", 1)[0].strip()
            if not data:
                continue
            if data.startswith("@Levels:"):
                levels = data.partition(":")[2].strip()
            elif data.startswith("@Reorder:"):
                order = data.partition(":")[2].strip()
            elif not data.startswith("@"):
                item = {"line": number, "data": data}
                if source["name"] == "BidiTest.txt":
                    item.update(levels=levels, order=order)
                cases.append(item)
        selected = choose(cases, None if full else source.get("limit"))
        result[keys[source["name"]]] = selected
        result["sources"].append({**source, "totalCases": len(cases), "selectedCases": len(selected)})
    if full:
        result["selection"] = "All official cases; no expected results changed."
    return (json.dumps(result, ensure_ascii=False, separators=(",", ":")) + "\n").encode()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Verify local hashes without network access.")
    parser.add_argument("--full-fixture", type=Path, help="Additionally write all official cases outside the committed bounded fixture.")
    args = parser.parse_args()
    manifest = json.loads(MANIFEST.read_text())
    outputs = manifest["files"] + [manifest["fixture"]]
    if args.check:
        for entry in outputs:
            data = (ROOT / entry["destination"]).read_bytes()
            if digest(data) != entry["sha256"]:
                raise SystemExit("Hash mismatch: " + entry["destination"])
        print(f"Unicode source/fixture hashes verified: {len(outputs)} files.")
        return

    downloads = {}
    for entry in manifest["files"] + manifest["conformance"]:
        if entry["url"] in downloads:
            continue
        request = urllib.request.Request(entry["url"], headers={"User-Agent": "Electron2D-Unicode-vendoring"})
        with urllib.request.urlopen(request, timeout=30) as response:
            data = response.read()
        if digest(data) != entry["upstreamSHA256"]:
            raise SystemExit("Upstream hash mismatch: " + entry["url"])
        downloads[entry["url"]] = data

    prepared = []
    for entry in manifest["files"]:
        data = downloads[entry["url"]]
        if entry.get("adapt"):
            data = adapt(data)
        if digest(data) != entry["sha256"]:
            raise SystemExit("Adapted hash mismatch: " + entry["destination"])
        prepared.append((ROOT / entry["destination"], data))
    data = fixture(manifest, downloads)
    if digest(data) != manifest["fixture"]["sha256"]:
        raise SystemExit("Conformance fixture hash mismatch")
    prepared.append((ROOT / manifest["fixture"]["destination"], data))
    for path, data in prepared:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
    if args.full_fixture:
        args.full_fixture.write_bytes(fixture(manifest, downloads, full=True))
    print(f"Restored {len(prepared)} pinned Unicode source/license/fixture files.")


if __name__ == "__main__":
    main()
