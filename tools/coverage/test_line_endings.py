"""Regress checkout conversion without relaxing recipe or snapshot identity."""
import hashlib
import itertools
from pathlib import Path
import sys
import tempfile
from unittest.mock import patch

from compare_snapshot import compare

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'shaders'))
import build_toolchain


with tempfile.TemporaryDirectory(prefix='e2d-line-endings-') as temporary:
    root = Path(temporary)
    script = root / 'build_toolchain.py'
    lock = root / 'toolchain.lock.json'
    script_bytes = Path(build_toolchain.__file__).read_bytes().replace(b'\r\n', b'\n')
    lock_bytes = (build_toolchain.root / lock.name).read_bytes().replace(b'\r\n', b'\n')
    recipe = hashlib.sha256(lock_bytes + script_bytes).hexdigest()
    output = root / f'obj/toolchain/{build_toolchain.host}'
    (output / 'bin').mkdir(parents=True)
    tools = [output / 'bin' / (name + build_toolchain.suffix)
             for name in ('glslangValidator', 'spirv-val', 'spirv-dis')]
    for tool in tools:
        tool.write_bytes(b'fixture')
    receipt = output / 'recipe.sha256'
    receipt.write_text(recipe)
    stamp = receipt.stat().st_mtime_ns
    with patch.object(build_toolchain, 'root', root), patch.object(build_toolchain, '__file__', str(script)):
        for script_eol, lock_eol in itertools.product((b'\n', b'\r\n'), repeat=2):
            script.write_bytes(script_bytes.replace(b'\n', script_eol))
            lock.write_bytes(lock_bytes.replace(b'\n', lock_eol))
            with patch.object(build_toolchain, 'build_sources') as build:
                assert build_toolchain.build() == output
                build.assert_not_called()
            assert receipt.stat().st_mtime_ns == stamp, 'Checkout conversion rewrote receipt'
        for path in (script, lock):
            original = path.read_bytes()
            path.write_bytes(original + b' ')
            with patch.object(build_toolchain, 'build_sources') as build:
                build_toolchain.build()
                build.assert_called_once()
            assert receipt.read_text() != recipe, 'Content edit retained recipe'
            path.write_bytes(original)
            receipt.write_text(recipe)
        for tool in tools:
            tool.unlink()
            with patch.object(build_toolchain, 'build_sources') as build:
                build_toolchain.build()
                build.assert_called_once()
            tool.write_bytes(b'fixture')
        receipt.write_text('corrupt')
        with patch.object(build_toolchain, 'build_sources') as build:
            build_toolchain.build()
            build.assert_called_once()
        # A damaged downloaded archive must still fail before extraction/build.
        work = root / f'obj/toolchain-build/{build_toolchain.host}'
        work.mkdir(parents=True, exist_ok=True)
        (work / 'fixture.tar.gz').write_bytes(b'corrupt')
        try:
            build_toolchain.build_sources(b'[{"name":"fixture","sha256":"invalid"}]', output)
            raise AssertionError('Corrupt archive accepted')
        except SystemExit as error:
            assert 'Archive checksum mismatch' in str(error)
        # A pre-existing CRLF source stamp must reuse the extracted source tree.
        source = work / 'source'
        source.mkdir()
        (source / 'CMakeLists.txt').write_text('fixture')
        sources = b'[\n{"name":"fixture","directory":".","commit":"pinned"},\n{"name":"SPIRV-Tools","directory":".","commit":"pinned"}\n]\n'
        (work / 'sources.json').write_bytes(sources.replace(b'\n', b'\r\n'))
        with patch.object(build_toolchain.subprocess, 'run') as run, patch.object(build_toolchain.shutil, 'copy2'):
            build_toolchain.build_sources(sources, output)
            assert run.call_count == 3, 'Existing source stamp was not reused'
    expected, actual = root / 'expected.json', root / 'actual.json'
    expected.write_bytes(b'{\n  "api": 1\n}\n')
    actual.write_bytes(expected.read_bytes().replace(b'\n', b'\r\n'))
    assert compare(expected, actual), 'Equivalent snapshots differ'
    for data in (b'{\n  "api": 2\n}\n', b'{\n "api": 1\n}\n',
                 b'{\n  "api": 1\r}\n', b'\xff', b'{\n  "api": 1\n}'):
        actual.write_bytes(data)
        assert not compare(expected, actual), 'Changed or corrupt snapshot accepted'
print('LF/CRLF recipe, cache invalidation, archive checksum and exact API comparison passed')
