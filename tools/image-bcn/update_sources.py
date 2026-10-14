#!/usr/bin/env python3
"""Import pinned private BC/DDS codecs and their buffer-tool source closure."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
PINS = {"BCn": "51d3199b3bc374e0bdda3947d9f347cb6235a9a0", "Toolkit": "657c6971a8d42655c648336b781639ed96c2c49f"}
SEEDS = ["Memory/", "Helpers/", "Enumerables/", "Buffers/", "Streams/",
         "Extensions/ArrayExtensions.1D.cs", "Extensions/ArrayExtensions.2D.cs", "Extensions/ArrayExtensions.3D.cs",
         "Extensions/BoolExtensions.cs", "Extensions/StringExtensions.cs", "Extensions/ArrayPoolExtensions.cs",
         "Extensions/MemoryExtensions.cs", "Extensions/ReadOnlyMemoryExtensions.cs", "Extensions/ReadOnlySpanExtensions.cs",
         "Extensions/SpanExtensions.cs", "Extensions/HashCodeExtensions.cs", "Extensions/StreamExtensions.cs", "Extensions/IBufferWriterExtensions.cs"]
PREFIX = "#define NETSTANDARD2_1_OR_GREATER\n#nullable enable annotations\n#nullable disable warnings\n#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022\n"

def adapt(source, bcn):
    text = PREFIX + source.decode("utf-8-sig")
    text = text.replace("BCnEncoder", "Electron2DBlockCodecs").replace("CommunityToolkit.HighPerformance", "Electron2DBufferTools")
    text = re.sub(r"(?m)^(\s*)public (?=(?:(?:readonly|ref|static|sealed|abstract|unsafe|partial) )*(?:class|struct|enum|interface|delegate|record)\b)", r"\1internal ", text)
    if bcn:
        text = text.replace("MemoryMarshal.Cast<byte, T>(outputData)", "MemoryMarshal.Cast<byte, T>(outputData.AsSpan())")
        text = "#define NETSTANDARD2_1\n" + re.sub(r"\bHalf\b", "BCnHalf", text)
    return text.encode()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--bcn", type=Path, required=True)
    parser.add_argument("--toolkit", type=Path, required=True)
    parser.add_argument("--check", action="store_true", help="Compare a repeat import without writing")
    args = parser.parse_args()
    for name, repo in (("BCn", args.bcn), ("Toolkit", args.toolkit)):
        commit = subprocess.check_output(["git", "-C", str(repo), "rev-parse", "HEAD"], text=True).strip()
        if commit != PINS[name]: raise SystemExit(f"{name}: expected {PINS[name]}, got {commit}")
        source = repo / ("BCnEnc.Net" if name == "BCn" else "src/CommunityToolkit.HighPerformance")
        files = {}
        original = {}
        for path in sorted(source.rglob("*.cs")):
            relative = path.relative_to(source).as_posix()
            if name == "Toolkit" and not any(relative == seed or seed.endswith("/") and relative.startswith(seed) for seed in SEEDS): continue
            data = path.read_bytes(); files[relative] = adapt(data, name == "BCn"); original[relative] = hashlib.sha256(data).hexdigest()
        for output, path in (("LICENSE-MIT", repo / "LICENSE-MIT"), ("UNLICENSE", repo / "UNLICENSE")) if name == "BCn" else (("LICENSE", repo / "License.md"),):
            data = path.read_bytes(); files[output] = data; original[output] = hashlib.sha256(data).hexdigest()
        manifest = {"repository": "https://github.com/Nominom/BCnEncoder.NET" if name == "BCn" else "https://github.com/CommunityToolkit/dotnet", "commit": PINS[name],
                    "adaptations": "Private namespaces/types, target symbols and annotation/diagnostic policy; BCn Half identifier and explicit writable Span casts. Algorithms unchanged.",
                    "files": {key: {"sha256": hashlib.sha256(data).hexdigest(), "upstream_sha256": original[key]} for key, data in files.items()}}
        files["manifest.json"] = (json.dumps(manifest, indent=2) + "\n").encode()
        destination = ROOT / "src/Vendor" / name
        actual = {p.relative_to(destination).as_posix() for p in destination.rglob("*") if p.is_file()}
        if args.check and actual != set(files): raise SystemExit(f"{name}: source closure changed")
        for relative, data in files.items():
            target = destination / relative
            if args.check:
                if target.read_bytes() != data: raise SystemExit(f"{name}/{relative}: repeat import differs")
            else:
                target.parent.mkdir(parents=True, exist_ok=True); target.write_bytes(data)
        if not args.check:
            for removed in actual - set(files): (destination / removed).unlink()
        notice = ROOT / "licence" / ("BCnEncoder-LICENSE.txt" if name == "BCn" else "BufferTools-LICENSE.txt")
        data = files["LICENSE-MIT" if name == "BCn" else "LICENSE"]
        if args.check:
            if notice.read_bytes() != data: raise SystemExit(f"{notice.name}: license differs")
        else: notice.write_bytes(data)
        print(f"{name}: {len(manifest['files'])} pinned source/license files {'verified' if args.check else 'imported'}.")

if __name__ == "__main__": main()
