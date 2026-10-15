#!/usr/bin/env python3
"""Independent ordinary PCK encryption/interoperability oracle (Python + OpenSSL)."""
import argparse
import hashlib
from pathlib import Path
import struct
import subprocess

MAGIC = 0x43504447


def crypt(data, key, iv, decrypt=False):
    args = ["openssl", "enc", "-aes-256-cfb", "-nopad", "-K", key.hex(), "-iv", iv.hex()]
    if decrypt:
        args.append("-d")
    return subprocess.run(args, input=data, capture_output=True, check=True).stdout


def encrypt(data, key, serial):
    iv = bytes([serial]) * 16
    return hashlib.md5(data).digest() + struct.pack("<Q", len(data)) + iv + crypt(data + bytes(-len(data) % 16), key, iv)


def decrypt(data, key):
    length, = struct.unpack_from("<Q", data, 16)
    if length > 64 * 1024 * 1024:
        raise ValueError("Oracle byte budget exceeded")
    padded = (length + 15) // 16 * 16
    result = crypt(data[40:40 + padded], key, data[24:40], True)[:length]
    assert len(result) == length and hashlib.md5(result).digest() == data[:16]
    return result


def create(folder):
    key = bytes(range(32))
    data = bytearray(128)
    entries = []
    for i, length in enumerate([0, 1, 15, 16, 17, 31, 32, 33]):
        payload = bytes(range(length))
        name = f"oracle/entry-{length}".encode()
        entries.append((name, len(data) - 128, payload))
        data += encrypt(payload, key, i + 1)
        data += bytes(-len(data) % 32)
    directory_offset = len(data)
    directory = bytearray()
    for name, offset, payload in entries:
        padded = (len(name) + 3) // 4 * 4
        directory += struct.pack("<I", padded) + name + bytes(padded - len(name))
        directory += struct.pack("<QQ", offset, len(payload)) + hashlib.md5(payload).digest() + struct.pack("<I", 1)
    data += struct.pack("<I", len(entries)) + encrypt(directory, key, 91)
    struct.pack_into("<6IQQ", data, 0, MAGIC, 4, 4, 6, 0, 3, 128, directory_offset)
    path = folder / "oracle.pck"
    path.write_bytes(data)
    print(f"Independent encrypted fixture: {len(entries)} entries; SHA256 {hashlib.sha256(data).hexdigest()}")


def check(path):
    data = path.read_bytes()
    magic, version, major, minor, patch, flags, base, directory = struct.unpack_from("<6IQQ", data)
    assert magic == MAGIC and version == 4 and flags == 3 and base % 32 == 0
    count, = struct.unpack_from("<I", data, directory)
    contents = decrypt(data[directory + 4:], bytes(32))
    pos = 0
    names = set()
    for _ in range(count):
        length, = struct.unpack_from("<I", contents, pos)
        pos += 4
        name = contents[pos:pos + length].rstrip(b"\0").decode()
        pos += length
        offset, size = struct.unpack_from("<QQ", contents, pos)
        md5 = contents[pos + 16:pos + 32]
        entry_flags, = struct.unpack_from("<I", contents, pos + 32)
        pos += 36
        assert entry_flags == 1 and (base + offset) % 32 == 0
        payload = decrypt(data[base + offset:], bytes(32))
        assert len(payload) == size and hashlib.md5(payload).digest() == md5
        names.add(name)
        if name.endswith(".png"):
            assert payload.startswith(b"\x89PNG\r\n\x1a\n")
        if name.endswith(".e2dscene"):
            assert payload.startswith(b"E2DASSET")
    assert pos == len(contents) and names == {"game/pixel.png", "game/scene.e2dscene"}
    print(f"Independent OpenSSL decode: {count} encrypted entries + encrypted directory; SHA256 {hashlib.sha256(data).hexdigest()}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=["create", "check"])
    parser.add_argument("path", type=Path)
    args = parser.parse_args()
    if args.mode == "create":
        args.path.mkdir(parents=True, exist_ok=True)
        create(args.path)
    else:
        check(args.path)
