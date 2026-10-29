"""Run every native allocation failure in fresh PAL consumers, with independent codecs."""
import ctypes
import ctypes.util
import hashlib
import json
import os
from pathlib import Path
import resource
import subprocess
import sys
import tempfile
import zlib


def require(value, message):
    if not value:
        raise RuntimeError(message)


def run(build, report):
    build = build.resolve()
    resource.setrlimit(resource.RLIMIT_CORE, (0, 0))
    encoder = ctypes.CDLL(ctypes.util.find_library('brotlienc'))
    decoder = ctypes.CDLL(ctypes.util.find_library('brotlidec'))
    encoder.BrotliEncoderCompress.argtypes = [ctypes.c_int, ctypes.c_int, ctypes.c_int, ctypes.c_size_t, ctypes.c_void_p, ctypes.POINTER(ctypes.c_size_t), ctypes.c_void_p]
    decoder.BrotliDecoderDecompress.argtypes = [ctypes.c_size_t, ctypes.c_void_p, ctypes.POINTER(ctypes.c_size_t), ctypes.c_void_p]
    buffer = ctypes.create_string_buffer(131072)
    env = os.environ | {'LD_LIBRARY_PATH': str(build / 'fault') + ':' + str(build), 'ASAN_OPTIONS': 'detect_leaks=1:halt_on_error=1', 'UBSAN_OPTIONS': 'halt_on_error=1:print_stacktrace=1'}
    results = []
    for pattern, quality in [('repeat', 5), ('random', 0), ('random', 11)]:
        source = bytes((i * 113 + i // 7) % 256 for i in range(65536))
        if pattern == 'random':
            state, data = 1, bytearray()
            for _ in source:
                state ^= (state << 13) & 0xffffffff
                state ^= state >> 17
                state ^= (state << 5) & 0xffffffff
                data.append(state & 255)
            source = bytes(data)
        size = ctypes.c_size_t(len(buffer))
        require(encoder.BrotliEncoderCompress(quality, 22, 0, len(source), source, ctypes.byref(size), buffer), 'Independent Brotli encoding failed')
        fixtures = []
        for window in (-15, 15, 31):
            codec = zlib.compressobj(6, zlib.DEFLATED, window)
            fixtures.append(codec.compress(source) + codec.flush())
        fixtures.append(buffer.raw[:size.value])
        case_env = env | {'E2D_COMPRESSION_QUALITY': str(quality)}
        if pattern == 'random':
            case_env['E2D_COMPRESSION_RANDOM'] = '1'
        with tempfile.TemporaryDirectory(prefix='e2d-compression-vectors-') as directory:
            temporary = Path(directory)
            for mode in range(10):
                fixture = temporary / 'input'
                fixture.write_bytes(fixtures[mode % 3 if mode < 6 else 3])
                output = temporary / 'encoded'
                position, maximum = 0, 0
                while position <= maximum:
                    command = [str(build / 'Electron2DCompressionMemoryCheck'), str(mode), str(position), str(fixture), str(output)]
                    result = subprocess.run(command, env=case_env, text=True, capture_output=True)
                    require(result.returncode == 0, f'{command} ({pattern}, quality={quality}): {result.returncode}\n{result.stdout}\n{result.stderr}')
                    row = json.loads(result.stdout) | {'pattern': pattern, 'quality': quality}
                    if position == 0:
                        maximum = row['attempts']
                    else:
                        require(row['attempts'] >= position, 'Failure position was not reached')
                    if mode < 3:
                        require(zlib.decompress(output.read_bytes(), (-15, 15, 31)[mode]) == source, 'Independent zlib decode changed bytes')
                    elif mode in (6, 8):
                        encoded = output.read_bytes()
                        size = ctypes.c_size_t(len(buffer))
                        require(decoder.BrotliDecoderDecompress(len(encoded), encoded, ctypes.byref(size), buffer) == 1 and buffer.raw[:size.value] == source, 'Independent Brotli decode changed bytes')
                    results.append(row)
                    position += 1
    pal = build / 'libSystem.IO.Compression.Native.so'
    report.write_text(json.dumps({'palSHA256': hashlib.sha256(pal.read_bytes()).hexdigest(), 'faultPositions': sum(r['failAt'] != 0 for r in results), 'cases': results}, indent=2) + '\n', encoding='utf-8')
    print(f"Passed {len(results)} cases, {sum(r['failAt'] != 0 for r in results)} allocation failures; independent zlib/Brotli bytes, retry and full native drain.")


if __name__ == '__main__':
    run(*map(Path, sys.argv[1:]))
