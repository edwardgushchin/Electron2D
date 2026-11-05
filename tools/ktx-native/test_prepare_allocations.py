"""Check immutable archive inputs, idempotence and atomic refusal of source drift."""

import hashlib
from pathlib import Path
import sys
import tarfile
import tempfile

from prepare_allocations import PINS, main


def check(archive):
    assert hashlib.sha256(archive.read_bytes()).hexdigest() == "bce6794bca57d86de2f00683ba8eaa6b3e8875d5abe45ac1136e206eb68aa476"
    with tarfile.open(archive) as source:
        originals = {}
        for name, (digest, _) in PINS.items():
            matches = [member for member in source.getmembers() if member.name.endswith("/" + name)]
            assert len(matches) == 1, name
            data = source.extractfile(matches[0]).read()
            assert hashlib.sha256(data).hexdigest() == digest, name
            originals[name] = data
    with tempfile.TemporaryDirectory(prefix="e2d-ktx-source-") as directory:
        root = Path(directory)

        def restore():
            for name, data in originals.items():
                path = root / name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(data)

        restore()
        main(root)
        prepared = {name: (root / name).read_bytes() for name in PINS}
        main(root)
        assert prepared == {name: (root / name).read_bytes() for name in PINS}
        for name in PINS:
            restore()
            path = root / name
            path.write_bytes(path.read_bytes() + b"\n/* source drift */\n")
            before = {entry: (root / entry).read_bytes() for entry in PINS}
            try:
                main(root)
            except ValueError as error:
                assert "Pinned allocation input changed: " + name == str(error)
            else:
                raise AssertionError("Accepted source drift: " + name)
            assert before == {entry: (root / entry).read_bytes() for entry in PINS}
    print(f"KTX allocation source: {len(PINS)} pinned pairs, idempotence and atomic drift refusal passed.")


if __name__ == "__main__":
    check(Path(sys.argv[1]))
