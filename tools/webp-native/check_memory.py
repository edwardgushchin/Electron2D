"""Exercise actual shared WebP ownership failures with an independent decoder."""
import ctypes
import ctypes.util
import hashlib
import json
from pathlib import Path
import resource
import subprocess
import sys
import tempfile


def require(value, message):
    if not value:
        raise RuntimeError(message)


def run(build, report):
    build = build.resolve()
    resource.setrlimit(resource.RLIMIT_CORE, (0, 0))
    oracle = ctypes.CDLL(ctypes.util.find_library('webp'))
    oracle.WebPDecodeRGBAInto.argtypes = [ctypes.c_void_p, ctypes.c_size_t, ctypes.c_void_p, ctypes.c_size_t, ctypes.c_int]
    oracle.WebPDecodeRGBAInto.restype = ctypes.c_void_p
    rows = []
    with tempfile.TemporaryDirectory(prefix='e2d-webp-vectors-') as directory:
        pixels = Path(directory) / 'pixels'
        for mode in range(29):
            position, maximum = 0, 0
            while position <= maximum:
                result = subprocess.run([str(build / 'Electron2DWebPMemoryCheck'), str(mode), str(position), str(pixels)], capture_output=True, text=True)
                require(result.returncode == 0, f'mode={mode}, failAt={position}: {result.returncode}\n{result.stdout}\n{result.stderr}')
                row = json.loads(result.stdout)
                if position == 0:
                    maximum = row['attempts']
                else:
                    require(row['attempts'] >= position, 'Failure position was not reached')
                wire = pixels.with_suffix('.webp').read_bytes()
                width, height = (4, 4) if mode == 28 else (47, 33)
                # The independent static decoder cannot composite animation canvases.
                if mode != 28:
                    output = ctypes.create_string_buffer(width * height * 4)
                    require(oracle.WebPDecodeRGBAInto(wire, len(wire), output, len(output), width * 4) and output.raw == pixels.read_bytes(), 'Independent pixels changed')
                rows.append(row)
                position += 1
    pal = build / 'libElectron2DWebP.so'
    report.write_text(json.dumps({'librarySHA256': hashlib.sha256(pal.read_bytes()).hexdigest(), 'faultPositions': sum(r['failAt'] != 0 for r in rows), 'cases': rows}, indent=2) + '\n', encoding='utf-8')
    print(f"Passed {len(rows)} cases, {sum(r['failAt'] != 0 for r in rows)} failed requests; full native drain, retry, independent pixels and animation canvas.")


if __name__ == '__main__':
    run(*map(Path, sys.argv[1:]))
