"""Pin the generated allocator and reject changed input before producing output."""

import argparse
import hashlib
from pathlib import Path
import tempfile
import prepare_memory


def check(source):
    original = source.read_bytes()
    with tempfile.TemporaryDirectory() as directory:
        root = Path(directory)
        output = root / "SDL_malloc.c"
        prepare_memory.generate(source, output)
        result = output.read_text()
        assert result.startswith(prepare_memory.NOTICE)
        result = result.removeprefix(prepare_memory.NOTICE)
        if source.name == "SDL_properties.c":
            assert result == original.decode() + prepare_memory.IO_HELPERS
        elif source.name == "SDL_iostream.c":
            expected = original.decode()
            for old, new in prepare_memory.IO_EDITS:
                expected = expected.replace(old, new)
            assert result == expected
        else:
            environment = source.name == "SDL_getenv.c"
            replacement = prepare_memory.ENV_REPLACEMENT if environment else prepare_memory.REPLACEMENT
            original_text = prepare_memory.ENV_ORIGINAL if environment else prepare_memory.ORIGINAL
            assert result.count(replacement) == (2 if environment else 1)
            assert original_text not in result
        assert source.read_bytes() == original
        (root / "input").mkdir()
        changed = root / "input" / source.name
        for content in (original + b"\n", original.replace(b"SDL_", b"BAD_", 1)):
            changed.write_bytes(content)
            output.unlink(missing_ok=True)
            try:
                prepare_memory.generate(changed, output)
            except ValueError:
                pass
            else:
                raise AssertionError("Changed SDL input was accepted")
            assert not output.exists()
    print("SDL allocation source SHA256:", hashlib.sha256(original).hexdigest(), "; generation and corruption controls passed")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    check(parser.parse_args().source)
