#!/usr/bin/env python3
"""Compare the private file ABI against separately loaded mature codec sources."""
import argparse
import ctypes as c
from pathlib import Path
import random
import subprocess
import tempfile


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('bridge', type=Path)
    args = parser.parse_args()
    bridge = c.CDLL(str(args.bridge.resolve()))
    libc = c.CDLL(None)
    bridge.e2d_enet_memory.argtypes = [c.c_void_p, c.c_void_p]
    assert bridge.e2d_enet_memory(c.cast(libc.malloc, c.c_void_p), c.cast(libc.free, c.c_void_p)) == 0
    signature = [c.c_int, c.c_void_p, c.c_int, c.c_void_p, c.c_int]
    for name in ['e2d_file_encode', 'e2d_file_decode']:
        function = getattr(bridge, name)
        function.argtypes = signature
        function.restype = c.c_int
    with tempfile.TemporaryDirectory() as directory:
        oracle = Path(directory) / 'fastlz.so'
        source = Path(__file__).resolve().parents[2] / 'src/Vendor/FastLZ/fastlz.c'
        subprocess.run(['cc', '-shared', '-fPIC', '-O2', str(source), '-o', str(oracle)], check=True)
        fast = c.CDLL(str(oracle))
        fast.fastlz_compress.argtypes = [c.c_void_p, c.c_int, c.c_void_p]
        fast.fastlz_decompress.argtypes = [c.c_void_p, c.c_int, c.c_void_p, c.c_int]
        zstd = c.CDLL('libzstd.so.1')
        zstd.ZSTD_compress.argtypes = [c.c_void_p, c.c_size_t, c.c_void_p, c.c_size_t, c.c_int]
        zstd.ZSTD_compress.restype = c.c_size_t
        zstd.ZSTD_decompress.argtypes = [c.c_void_p, c.c_size_t, c.c_void_p, c.c_size_t]
        zstd.ZSTD_decompress.restype = c.c_size_t
        rng = random.Random(79)
        cases = 0
        for mode in [0, 2]:
            for size in [1, 2, 7, 15, 16, 17, 65535, 65536]:
                for data in [rng.randbytes(size), bytes([37]) * size]:
                    native_data = data.ljust(16, b'\0') if mode == 0 else data
                    source_buffer = c.create_string_buffer(native_data)
                    bound = bridge.e2d_file_encode(mode, None, size, None, 0)
                    assert 0 < bound <= 131136
                    encoded = c.create_string_buffer(bound)
                    count = bridge.e2d_file_encode(mode, source_buffer, size, encoded, bound)
                    assert 0 < count <= bound
                    decoded = c.create_string_buffer(len(native_data))
                    actual = fast.fastlz_decompress(encoded, count, decoded, len(native_data)) if mode == 0 else zstd.ZSTD_decompress(decoded, len(native_data), encoded, count)
                    assert actual == len(native_data) and decoded.raw == native_data
                    count = fast.fastlz_compress(source_buffer, len(native_data), encoded) if mode == 0 else zstd.ZSTD_compress(encoded, bound, source_buffer, len(native_data), 3)
                    actual = bridge.e2d_file_decode(mode, encoded, count, decoded, len(native_data))
                    assert actual == len(native_data) and decoded.raw == native_data
                    assert bridge.e2d_file_encode(mode, source_buffer, size, encoded, bound - 1) == -1
                    cases += 1
        assert bridge.e2d_file_encode(99, None, 1, None, 0) == -1
        assert bridge.e2d_file_decode(0, None, 0, None, 0) == -1
        print(f'{cases} independent bidirectional FastLZ/Zstandard block comparisons passed.')


if __name__ == '__main__':
    main()
