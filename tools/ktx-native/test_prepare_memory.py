"""Check the complete pinned C allocation profile and its lexical boundary."""

import hashlib
import json
from pathlib import Path
import sys
import tarfile
import tempfile

from prepare_allocations import PINS as OWNERSHIP, prepare as prepare_ownership
from prepare_memory import main, namespace


def check(archive):
    example = '''#include <malloc.h>
// free(malloc(1));
const char *a = "realloc calloc";
const char *b = R"tag("free" malloc())tag";
__attribute__((malloc)) void *owner();
#if __has_attribute(malloc)
#endif
auto p = std::malloc(4); auto release = &free;
posix_memalign(&p, 64, 32); _aligned_free(p);
auto name = strdup("free"); auto windows = _strdup("malloc");
'''
    expected = example.replace("std::malloc(4)", "e2d_ktx_malloc(4)").replace("&free", "&e2d_ktx_free")
    expected = expected.replace("posix_memalign(&p", "e2d_ktx_posix_memalign(&p").replace("_aligned_free(p)", "e2d_ktx_aligned_free(p)")
    expected = expected.replace("strdup(\"free\")", "e2d_ktx_strdup(\"free\")").replace("_strdup(\"malloc\")", "e2d_ktx_strdup(\"malloc\")")
    assert namespace(example) == expected
    assert namespace(expected) == expected
    assert hashlib.sha256(archive.read_bytes()).hexdigest() == "bce6794bca57d86de2f00683ba8eaa6b3e8875d5abe45ac1136e206eb68aa476"
    pins = json.loads(Path(__file__).with_name("memory-sources.json").read_text())
    originals = {}
    with tarfile.open(archive) as source:
        for member in source.getmembers():
            if not member.isfile():
                continue
            name = member.name.split("/", 1)[1]
            if name in pins:
                data = source.extractfile(member).read()
                assert hashlib.sha256(data).hexdigest() == pins[name]["original"], name
                originals[name] = data
    assert originals.keys() == pins.keys()
    with tempfile.TemporaryDirectory(prefix="e2d-ktx-memory-source-") as temporary:
        root = Path(temporary)

        def restore():
            for name, data in originals.items():
                path = root / name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(data)

        restore()
        main(root)
        prepared = {name: (root / name).read_bytes() for name in pins}
        main(root)
        assert prepared == {name: (root / name).read_bytes() for name in pins}
        restore()
        for name in OWNERSHIP:
            (root / name).write_bytes(prepare_ownership(name, originals[name].decode("utf-8")).encode("utf-8"))
        main(root)
        assert prepared == {name: (root / name).read_bytes() for name in pins}
        for name in pins:
            restore()
            (root / name).write_bytes(originals[name] + b"\n/* drift */\n")
            before = {entry: (root / entry).read_bytes() for entry in pins}
            try:
                main(root)
            except ValueError as error:
                assert str(error) == "Pinned C allocation input changed: " + name
            else:
                raise AssertionError("Accepted source drift: " + name)
            assert before == {entry: (root / entry).read_bytes() for entry in pins}
        restore()
        added = root / "lib/src/unregistered.c"
        added.write_text("void *unregistered() { return malloc(4); }\n")
        try:
            main(root)
        except ValueError as error:
            assert str(error) == "Unknown C allocation source: lib/src/unregistered.c"
        else:
            raise AssertionError("Accepted an unregistered allocation source")
        assert originals == {name: (root / name).read_bytes() for name in pins}
    print(f"KTX C allocation profile: {len(pins)} pinned pairs, lexical guards, prior-profile upgrade, idempotence and atomic drift refusal passed.")


if __name__ == "__main__":
    check(Path(sys.argv[1]))
