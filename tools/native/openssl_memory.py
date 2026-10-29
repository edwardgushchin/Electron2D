"""Prepare the checked Linux allocator profile without changing the pinned source."""
import hashlib
from pathlib import Path
import re
import shutil

SOURCES = {
    "crypto/mem.c": "3052c09ab94e14469463d97b471e64d68708b1c1f76e205e26e1aefa66b31238",
    "crypto/mem_sec.c": "d8762f7f02709c28986b671ea26bfadfc52816a9c024ec5acec98b226db3c197",
    "crypto/LPdir_unix.c": "5b08afaef95f9fae98635c98d767c3180fb237aa70da612df8fe76dae127d895",
    "crypto/conf/conf_lib.c": "6f611ee20431c8bed508d8bf24b218c4033aaece913a23a5a96f78f2742a9cd6",
    "crypto/conf/conf_sap.c": "ebe805a9630a55a92d82c7a346e7675d854630b559152af90dbb4b3e48f7e68b",
}
CALLS = {
    "crypto/mem.c": {"malloc": 1, "realloc": 1, "free": 1, "posix_memalign": 1},
    "crypto/mem_sec.c": {"mmap": 2, "munmap": 1},
    "crypto/LPdir_unix.c": {"malloc": 1, "free": 2},
    "crypto/conf/conf_lib.c": {"malloc": 1, "strdup": 2, "free": 5},
    "crypto/conf/conf_sap.c": {"strdup": 1, "free": 1},
}
REPLACEMENTS = dict(malloc="e2d_memory_malloc", realloc="e2d_memory_realloc", free="e2d_memory_free",
                    posix_memalign="e2d_ssl_memalign", strdup="e2d_ssl_strdup", mmap="e2d_ssl_mmap", munmap="e2d_ssl_munmap")


def transform(name, text, header):
    # Match statements, not comments or names of caller-owned callbacks.
    code = re.sub(r"/\*.*?\*/|//[^\n]*", lambda match: " " * len(match[0]), text, flags=re.S)
    counts, edits = {}, []
    for call in CALLS[name]:
        sites = list(re.finditer(r"\b" + call + r"(?=\s*\()", code))
        counts[call] = len(sites)
        edits.extend((site.start(), site.end(), REPLACEMENTS[call]) for site in sites)
    if counts != CALLS[name]:
        raise ValueError("The pinned allocation sites changed: " + name + " " + str(counts))
    for start, end, replacement in sorted(edits, reverse=True):
        text = text[:start] + replacement + text[end:]
    if name == "crypto/mem_sec.c":
        # These three allocations already have a complete failure/cleanup path.
        for field in ("freelist", "bittable", "bitmalloc"):
            statement = "    OPENSSL_assert(sh." + field + " != NULL);\n"
            if text.count(statement) != 1:
                raise ValueError("The pinned secure-heap failure boundary changed")
            text = text.replace(statement, "")
    marker = '#include "internal/e_os.h"' if name in ("crypto/mem.c", "crypto/mem_sec.c", "crypto/conf/conf_lib.c") else '#include <stdlib.h>' if name == "crypto/LPdir_unix.c" else '#include "internal/cryptlib.h"'
    if text.count(marker) != 1:
        raise ValueError("The pinned source include boundary changed: " + name)
    return text.replace(marker, marker + '\n#include "' + str(header).replace('\\', '/') + '"')


def prepare(source, destination):
    header = Path(__file__).resolve().parents[1] / "tls-native/memory.h"
    generated = {}
    for name, sha in SOURCES.items():
        raw = (source / name).read_bytes()
        if hashlib.sha256(raw).hexdigest() != sha:
            raise ValueError("The pinned TLS allocator source changed: " + name)
        generated[name] = transform(name, raw.decode(), header)
    if not destination.exists():
        shutil.copytree(source, destination)
    for name, text in generated.items():
        target = destination / name
        if target.read_text() != text:
            target.write_text(text)
    return destination
