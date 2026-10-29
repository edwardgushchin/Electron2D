"""Pinned source preparation, cache corruption and closed-failure checks."""
from pathlib import Path
import sys
import tempfile
from prepare import prepare


def rejected(action, text):
    try:
        action()
    except ValueError as error:
        if text not in str(error):
            raise
    else:
        raise RuntimeError('Invalid source input was accepted: ' + text)


with tempfile.TemporaryDirectory(prefix='e2d-compression-prepare-') as directory:
    root = Path(directory)
    archive = Path(sys.argv[1])
    output = root / 'prepared'
    prepare(archive, output)
    prepare(archive, output)
    bad_archive = root / 'bad.tar.gz'
    bad_archive.write_bytes(b'invalid')
    rejected(lambda: prepare(bad_archive, root / 'bad'), 'Unexpected runtime archive')
    file = output / 'runtime-10.0.1/src/native/libs/System.IO.Compression.Native/pal_zlib.c'
    before = file.read_bytes()
    file.write_bytes(before + b'\n/* changed */\n')
    rejected(lambda: prepare(archive, output), 'Changed prepared input:')
    file.write_bytes(before)
    extra = file.with_name('unexpected.c')
    extra.write_bytes(b'/* unexpected */')
    rejected(lambda: prepare(archive, output), 'Changed prepared input set')
    extra.unlink()
    file.unlink()
    file.symlink_to(root / 'outside')
    (root / 'outside').write_bytes(before)
    rejected(lambda: prepare(archive, output), 'Changed prepared input:')
    file.unlink()
    file.write_bytes(before)
    prepare(archive, output)
print('Source preparation and four corrupted-input controls passed.')
