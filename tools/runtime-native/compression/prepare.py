"""Prepare an owned native-runtime component; installed SDK files are read-only inputs."""
import difflib
import hashlib
import json
from pathlib import Path
import sys
import tarfile

ARCHIVE = "4c0b3aa0fda67c304762a572d6172ca5b2d29bb62191cf7c0fcef40a15223092"
PREFIXES = ("src/native/libs/System.IO.Compression.Native/", "src/native/libs/Common/", "src/native/external/brotli/", "src/native/external/zlib-ng/", "src/native/minipal/")


def prepare(archive, output):
    with archive.open("rb") as stream:
        if hashlib.file_digest(stream, "sha256").hexdigest() != ARCHIVE:
            raise ValueError("Unexpected runtime archive")
    if output.exists():
        record = json.loads((output / 'source-inputs.json').read_text(encoding='utf-8'))
        if record['archive'] != ARCHIVE:
            raise ValueError('Unexpected cached source archive')
        files = {p.relative_to(output / 'runtime-10.0.1').as_posix() for p in (output / 'runtime-10.0.1').rglob('*') if p.is_file()}
        if files != set(record['prepared']):
            raise ValueError('Changed prepared input set')
        for name, digest in record['prepared'].items():
            file = output / 'runtime-10.0.1' / name
            if file.is_symlink() or hashlib.sha256(file.read_bytes()).hexdigest() != digest:
                raise ValueError('Changed prepared input: ' + name)
        return
    with tarfile.open(archive) as source:
        members = [m for m in source.getmembers() if m.isfile() and m.name.startswith("runtime-10.0.1/") and (
            m.name.removeprefix("runtime-10.0.1/").startswith(PREFIXES) or m.name in (
                "runtime-10.0.1/LICENSE.TXT", "runtime-10.0.1/src/native/external/brotli.cmake", "runtime-10.0.1/src/native/external/zlib-ng.cmake"))]
        source.extractall(output, members=members, filter="data")
    root = output / "runtime-10.0.1"
    patches = []
    originals = {}
    def patch(name, transform):
        file = root / name
        before = file.read_text(encoding="utf-8")
        after = transform(before)
        originals[name] = hashlib.sha256(file.read_bytes()).hexdigest()
        patches.extend(difflib.unified_diff(before.splitlines(True), after.splitlines(True), "a/" + name, "b/" + name))
        file.write_text(after, encoding="utf-8")
    def replace(text, old, new):
        if text.count(old) != 1: raise ValueError("Unexpected source anchor: " + old[:80])
        return text.replace(old, new)
    def zlib(text):
        text = replace(text, '#include "pal_zlib.h"', '#include "pal_zlib.h"\n#include "compression_memory.h"')
        start = text.index('    z_stream* zStream = (z_stream*)calloc(1, sizeof(z_stream));')
        end = text.index('\n}', start)
        text = text[:start] + '''    z_stream* zStream = (z_stream*)e2d_compression_zero(1, sizeof(z_stream));
    stream->internalState = zStream;
    if (zStream == NULL) return PAL_Z_MEMERROR;
    zStream->zalloc = e2d_compression_zalloc;
    zStream->zfree = e2d_compression_zfree;
    return PAL_Z_OK;''' + text[end:]
        text = replace(text, '        free(zStream);', '        e2d_compression_release(zStream);')
        for codec in ('deflate', 'inflate'):
            anchor = f'        result = {codec}Init2(zStream, ' + ('level, method, windowBits, memLevel, strategy);' if codec == 'deflate' else 'windowBits);') + '\n        TransferStateToPalZStream(zStream, stream);'
            changed = anchor + f'''
        if (result != PAL_Z_OK)
        {{
            if (zStream->state != NULL) {codec}End(zStream);
            End(stream);
        }}'''
            text = replace(text, anchor, changed)
        return text
    patch('src/native/libs/System.IO.Compression.Native/pal_zlib.c', zlib)
    def brotli(text):
        text = replace(text, '#include <stdlib.h>', '#include <stdlib.h>\n#include "compression_memory.h"')
        text = replace(text, 'return malloc(size);', 'return e2d_compression_allocate(size);')
        return replace(text, 'free(address);', 'e2d_compression_release(address);')
    patch('src/native/external/brotli/c/common/platform.c', brotli)
    def bootstrap(text):
        text = '#include "compression_memory.h"\n' + text
        return replace(text, 'return malloc(size);', 'return e2d_compression_allocate(size);')
    patch('src/native/external/brotli/c/enc/memory.c', bootstrap)
    def decoder(text):
        text = '#include "compression_memory.h"\n' + text
        text = replace(text, '(BrotliDecoderState*)malloc(sizeof(BrotliDecoderState))', '(BrotliDecoderState*)e2d_compression_allocate(sizeof(BrotliDecoderState))')
        return replace(text, 'free(state);', 'e2d_compression_release(state);')
    patch('src/native/external/brotli/c/dec/decode.c', decoder)
    def dictionary(text):
        text = '#include "compression_memory.h"\n' + text
        return replace(text, '(BrotliSharedDictionary*)malloc(sizeof(BrotliSharedDictionary))', '(BrotliSharedDictionary*)e2d_compression_allocate(sizeof(BrotliSharedDictionary))')
    patch('src/native/external/brotli/c/common/shared_dictionary.c', dictionary)
    (output / 'allocator.patch').write_text(''.join(patches), encoding='utf-8')
    prepared = {p.relative_to(root).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in root.rglob('*') if p.is_file()}
    (output / 'source-inputs.json').write_text(json.dumps({'archive': ARCHIVE, 'originals': originals, 'prepared': prepared}, indent=2))


if __name__ == '__main__':
    prepare(*map(Path, sys.argv[1:]))
