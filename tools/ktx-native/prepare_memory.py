"""Prepare the pinned codec's C allocation domain before any native use."""

import hashlib
import json
from pathlib import Path
import re
import sys

from prepare_allocations import PINS as OWNERSHIP, prepare as prepare_ownership
from prepare_cpp import prepare as prepare_cpp

NOTICE = "/* Generated Electron2D C allocation profile; the downloaded archive is unchanged. */\n#include \"allocator.h\"\n"
NAMES = ("malloc", "calloc", "realloc", "free", "posix_memalign", "_aligned_malloc", "_aligned_free", "strdup", "_strdup")
TOKENS = re.compile(
    r'^[ \t]*#[ \t]*include[ \t]*<[^>\n]+>|//[^\n]*|/\*[\s\S]*?\*/|'
    r'(?:u8|u|U|L)?R"(?P<delimiter>[^ ()\\\t\r\n]{0,16})\([\s\S]*?\)(?P=delimiter)"|'
    r'"(?:\\[\s\S]|[^"\\])*"|\'(?:\\[\s\S]|[^\'\\])*\'|'
    r'__attribute__\s*\(\(\s*malloc\s*\)\)|'
    r'(?:__has_attribute|XXH_HAS_ATTRIBUTE)\s*\(\s*malloc\s*\)|'
    r'(?P<name>\b(?:std\s*::\s*)?(?:' + "|".join(NAMES) + r')\b)', re.MULTILINE)

FOLDERS = ("lib/src", "external/dfdutils", "external/basis_universal/transcoder",
    "external/basis_universal/encoder", "external/astc-encoder/Source", "external/basis_universal/zstd")
EXTENSIONS = (".c", ".cpp", ".h", ".hpp", ".inl", ".inc", ".hh", ".cc", ".cxx")


def namespace(text):
    def replace(match):
        name = match.group("name")
        if name is None:
            return match.group()
        name = re.sub(r"^std\s*::\s*", "", name)
        return "e2d_ktx_" + name.lstrip("_")
    return TOKENS.sub(replace, text)


def prepare(name, contents):
    if name in OWNERSHIP:
        contents = prepare_ownership(name, contents.decode("utf-8")).encode("utf-8")
    # Preserve vendor comment encodings byte-for-byte; allocator identifiers are ASCII.
    return NOTICE.encode("ascii") + prepare_cpp(name, namespace(contents.decode("latin-1"))).encode("latin-1")


def main(root):
    manifest = json.loads(Path(__file__).with_name("memory-sources.json").read_text())
    for folder in FOLDERS:
        for path in (root / folder).rglob("*"):
            if path.suffix not in EXTENSIONS:
                continue
            text = path.read_bytes().decode("latin-1")
            if namespace(text) != text and path.relative_to(root).as_posix() not in manifest:
                raise ValueError("Unknown C allocation source: " + path.relative_to(root).as_posix())
    pending = []
    for name, pins in manifest.items():
        path = root / name
        contents = path.read_bytes()
        digest = hashlib.sha256(contents).hexdigest()
        if digest == pins["prepared"]:
            continue
        if digest == pins["original"]:
            result = prepare(name, contents)
        elif name in OWNERSHIP and digest == OWNERSHIP[name][1]:
            result = NOTICE.encode("ascii") + prepare_cpp(name, namespace(contents.decode("latin-1"))).encode("latin-1")
        else:
            raise ValueError("Pinned C allocation input changed: " + name)
        if hashlib.sha256(result).hexdigest() != pins["prepared"]:
            raise ValueError("Pinned C allocation output changed: " + name)
        pending.append((path, result))
    for path, contents in pending:
        path.write_bytes(contents)


if __name__ == "__main__":
    main(Path(sys.argv[1]))
