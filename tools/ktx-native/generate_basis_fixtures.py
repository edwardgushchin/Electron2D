"""Create legacy Basis pixel oracles using upstream basisu and the OpenEXR oracle."""

import argparse
import base64
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import tempfile
import zlib


def chunk(kind, data):
    return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))


def read_png(path):
    source = path.read_bytes()
    assert source[:8] == b"\x89PNG\r\n\x1a\n"
    offset, compressed = 8, bytearray()
    while offset < len(source):
        size = struct.unpack_from(">I", source, offset)[0]
        kind, data = source[offset + 4:offset + 8], source[offset + 8:offset + 8 + size]
        assert zlib.crc32(kind + data) == struct.unpack_from(">I", source, offset + 8 + size)[0]
        if kind == b"IHDR":
            width, height, depth, color, compression, filtering, interlace = struct.unpack(">IIBBBBB", data)
            assert depth == 8 and color in (0, 2, 6) and not (compression or filtering or interlace)
            channels = {0: 1, 2: 3, 6: 4}[color]
        if kind == b"IDAT":
            compressed.extend(data)
        offset += size + 12
    scan = zlib.decompress(compressed)
    previous, pixels, offset = bytearray(width * channels), bytearray(), 0
    for _ in range(height):
        filter_type = scan[offset]
        row = bytearray(scan[offset + 1:offset + 1 + width * channels])
        offset += 1 + width * channels
        for i in range(len(row)):
            left = row[i - channels] if i >= channels else 0
            up, upper_left = previous[i], previous[i - channels] if i >= channels else 0
            if filter_type == 4:
                prediction = left + up - upper_left
                distances = (abs(prediction - left), abs(prediction - up), abs(prediction - upper_left))
                add = (left, up, upper_left)[distances.index(min(distances))]
            else:
                assert 0 <= filter_type <= 3
                add = (0, left, up, (left + up) // 2)[filter_type]
            row[i] = (row[i] + add) & 255
        pixels.extend(row)
        previous = row
    assert offset == len(scan)
    return width, height, channels, pixels


def generate(tool, exr_oracle, destination):
    fixtures = []
    with tempfile.TemporaryDirectory(prefix="electron2d-basis-oracle-") as directory:
        root = Path(directory)
        rgba = bytes(value for y in range(5) for x in range(7) for value in (32 + x * 20, 50 + y * 25, 170, (x + y) * 19 + 30))
        (root / "source.png").write_bytes(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", 7, 5, 8, 6, 0, 0, 0)) +
                                         chunk(b"IDAT", zlib.compress(b"".join(b"\0" + rgba[y * 28:(y + 1) * 28] for y in range(5)))) + chunk(b"IEND", b""))
        (root / "source.hdr").write_bytes(b"#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y 5 +X 7\n" + bytes([48, 112, 224, 131]) * 35)
        for name, options, hdr in (("etc1s", [], False), ("uastc", ["-uastc"], False), ("hdr", ["-hdr"], True)):
            encoded = root / (name + ".basis")
            source = root / ("source.hdr" if hdr else "source.png")
            subprocess.run([str(tool), "-basis", "-file", str(source), "-mipmap", "-max_threads", "2", *options, "-output_file", str(encoded)], check=True, stdout=subprocess.DEVNULL)
            folder = root / name
            folder.mkdir()
            subprocess.run([str(tool), "-unpack", "-file", str(encoded), "-no_ktx"], cwd=folder, check=True, stdout=subprocess.DEVNULL)
            pixels = bytearray()
            for level, dimensions in enumerate(((7, 5), (3, 2), (1, 1))):
                if hdr:
                    exr = folder / f"{name}_hdr_unpacked_rgb_RGB_HALF_{level}_0000.exr"
                    raw = exr.with_suffix(".raw")
                    subprocess.run([str(exr_oracle), "decode", str(exr), str(raw)], check=True)
                    data = raw.read_bytes()
                    width, height, count, precision = struct.unpack_from("<iiii", data)
                    assert (width, height) == dimensions and count == 3 and precision in (2, 4)
                    values = struct.unpack("<" + ("e" if precision == 2 else "f") * (width * height * 3), data[16:])
                    pixels.extend(struct.pack("<" + "e" * len(values), *values))
                else:
                    width, height, count, rgb = read_png(folder / f"{name}_unpacked_rgb_RGBA32_{level}_0000.png")
                    alpha_width, alpha_height, alpha_count, alpha = read_png(folder / f"{name}_unpacked_a_RGBA32_{level}_0000.png")
                    assert (width, height) == dimensions == (alpha_width, alpha_height) and count == 3 and alpha_count == 1
                    pixels.extend(value for index in range(width * height) for value in (*rgb[index * 3:index * 3 + 3], alpha[index]))
            fixtures.append(dict(name=name, container="basis", width=7, height=5, mipmaps=True, format="Rgbh" if hdr else "Rgba8",
                                 encoded=base64.b64encode(encoded.read_bytes()).decode(), pixels=base64.b64encode(pixels).decode()))
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(dict(tool_sha256=hashlib.sha256(tool.read_bytes()).hexdigest(),
                                          exr_oracle_sha256=hashlib.sha256(exr_oracle.read_bytes()).hexdigest(), fixtures=fixtures), indent=2) + "\n")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("tool", type=Path)
    parser.add_argument("exr_oracle", type=Path)
    parser.add_argument("destination", type=Path)
    args = parser.parse_args()
    generate(args.tool.resolve(), args.exr_oracle.resolve(), args.destination.resolve())
