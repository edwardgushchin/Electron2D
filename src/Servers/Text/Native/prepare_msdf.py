"""Build the pinned core with private allocators and exception-safe ownership."""
import hashlib
import json
from pathlib import Path
import re
import sys


def require(value, message):
    if not value:
        raise ValueError(message)


def prepare(source, output):
    source, output = source.resolve(), output.resolve()
    require(source != output and source not in output.parents and output not in source.parents, "MSDF output must be outside the pinned source tree")
    manifest = json.loads((source / "manifest.json").read_text(encoding="utf-8"))
    require(manifest["commit"] == "1874bcf7d9624ccc85b4bc9a85d78116f690f35b", "Unexpected MSDF revision")
    require({p.relative_to(source).as_posix() for p in source.rglob("*") if p.is_file()} == set(manifest["files"]) | {"manifest.json"}, "Unexpected MSDF source closure")
    for name, digest in manifest["files"].items():
        require(hashlib.sha256((source / name).read_bytes()).hexdigest() == digest, name)
    for name in manifest["files"]:
        if Path(name).suffix not in (".h", ".hpp", ".cpp"):
            continue
        text = (source / name).read_text(encoding="utf-8")
        text = text.replace("std::vector", "e2d_msdf::vector")
        text = text.replace("std::queue", "e2d_msdf::queue")
        if name == "core/edge-segments.h":
            require(text.count("class EdgeSegment {") == 1, name)
            text = text.replace("class EdgeSegment {", "class EdgeSegment : public e2d_msdf::EdgeMemory {")
        if name in ("core/Shape.cpp", "core/edge-coloring.cpp"):
            text, count = re.subn(r"EdgeSegment \*parts\[(3|7)\] = \{ \};", r"e2d_msdf::Parts<EdgeSegment, \1> parts;", text)
            require(count == (1 if name == "core/Shape.cpp" else 3), name)
            text = re.sub(r"EdgeHolder\(parts\[([^]]+)\]\)", r"EdgeHolder(parts.take(\1))", text)
        if name == "core/EdgeHolder.cpp":
            before = "delete edgeSegment;\n        edgeSegment = orig.edgeSegment ? orig.edgeSegment->clone() : NULL;"
            require(text.count(before) == 1, name)
            text = text.replace(before, "EdgeHolder replacement(orig);\n        swap(*this, replacement);")
        if name == "core/Bitmap.hpp":
            # Construct the replacement before retiring the old buffer on allocation failure.
            for kind in ("BitmapConstRef<T, N>", "BitmapConstSection<T, N>", "Bitmap<T, N>"):
                signature = f"Bitmap<T, N> &Bitmap<T, N>::operator=(const {kind} &orig) {{"
                start = text.index(signature) + len(signature)
                end, depth = start, 1
                while depth:
                    depth += (text[end] == "{") - (text[end] == "}")
                    end += 1
                guard = "pixels != orig.pixels" if kind.startswith("BitmapConstRef") else "this != &orig" if kind == "Bitmap<T, N>" else "true"
                text = text[:start] + f"\n    if ({guard}) {{\n        Bitmap<T, N> replacement(orig);\n        *this = static_cast<Bitmap<T, N> &&>(replacement);\n    }}\n    return *this;\n" + text[end - 1:]
            require(text.count("new T[N*w*h]") == 4, name)
            text = text.replace("new T[N*w*h]", "e2d_msdf::array<T>(static_cast<size_t>(N)*w*h)")
            require(text.count("delete[] pixels;") == 2, name)
            text = text.replace("delete[] pixels;", "e2d_msdf::release(pixels);")
        target = output / name
        target.parent.mkdir(parents=True, exist_ok=True)
        content = '#include "MSDFMemory.h"\n' + text
        if not target.exists() or target.read_text(encoding="utf-8") != content:
            with target.open("w", encoding="utf-8", newline="\n") as stream:
                stream.write(content)


if __name__ == "__main__":
    prepare(*map(Path, sys.argv[1:]))
