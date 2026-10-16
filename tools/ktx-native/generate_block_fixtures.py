"""Generate block pixel oracles with independent ETC code and the upstream ASTC CLI."""

import argparse
import base64
import hashlib
import json
from pathlib import Path
import random
import struct
import subprocess
import tempfile

from generate_basis_fixtures import chunk, read_png
import zlib


LEVELS = ((7, 5), (3, 2), (1, 1))
FORMATS = {0: "Etc", 1: "Etc2Rgb8", 2: "Etc2Rgba8", 3: "Etc2Rgb8A1",
           4: "Etc2R11", 5: "Etc2R11S", 6: "Etc2Rg11", 7: "Etc2Rg11S",
           8: "Astc4X4", 9: "Astc4X4Hdr", 10: "Astc8X8", 11: "Astc8X8Hdr"}


def etc_mode(block):
    if not block[3] & 2:
        return "individual"
    for index, name in enumerate(("T", "H", "planar")):
        value = block[index]
        base, delta = value >> 3, value & 7
        delta -= 8 if delta & 4 else 0
        if not 0 <= base + delta <= 31:
            return name
    return "differential"


def etc_block(rng, mode):
    while True:
        data = rng.randbytes(8)
        if etc_mode(data) == mode:
            return data


def signed_block(base, multiplier, phase):
    # Explicit EAC table-zero vectors: column-major selectors 0..7, both signs,
    # multiplier zero, saturation, and the reserved -128 base normalized to -127.
    selectors = [(index + phase) % 8 for index in range(16)]
    packed = sum(value << (45 - index * 3) for index, value in enumerate(selectors))
    source = bytes((base & 255, multiplier << 4)) + packed.to_bytes(6, "big")
    modifiers = (-3, -6, -9, -15, 2, 5, 8, 14)
    normalized_base = max(-127, base)
    pixels = [max(-1023, min(1023, normalized_base * 8 + modifiers[selectors[x * 4 + y]] * max(1, multiplier * 8))) / 1023
              for y in range(4) for x in range(4)]
    return source, pixels


def fixture(mode, encoded, pixels, **extra):
    return dict(name=FORMATS[mode], mode=mode, width=7, height=5, mipmaps=True,
                source_format=FORMATS[mode], format="Rgbaf" if mode in (5, 7) else "Rgbah" if mode in (9, 11) else "Rgba8",
                encoded=base64.b64encode(encoded).decode(), pixels=base64.b64encode(pixels).decode(), **extra)


def generate(etc_oracle, astc_tool, exr_oracle, destination):
    fixtures, rng = [], random.Random(658432)
    with tempfile.TemporaryDirectory(prefix="electron2d-block-oracle-") as directory:
        root = Path(directory)
        for mode in range(8):
            encoded, pixels, modes, ordinal = bytearray(), bytearray(), [], 0
            for width, height in LEVELS:
                blocks, signed = bytearray(), []
                for by in range((height + 3) // 4):
                    for bx in range((width + 3) // 4):
                        if mode in (5, 7):
                            parameters = ((-127, 0), (0, 1), (127, 15), (-128, 0), (12, 8), (-56, 2))
                            base, multiplier = parameters[ordinal]
                            first, red = signed_block(base, multiplier, ordinal)
                            second, green = signed_block(-base if base != -128 else 127, multiplier, ordinal + 3)
                            blocks.extend(first + (second if mode == 7 else b""))
                            signed.append((red, green))
                        elif mode in (4, 6):
                            blocks.extend(rng.randbytes(8 if mode == 4 else 16))
                        else:
                            names = ("individual", "differential") if mode == 0 else ("individual", "differential", "T", "H", "planar", "differential")
                            name = names[ordinal % len(names)]
                            # A1 has no individual mode: its bit is opacity, not differential.
                            if mode == 3 and name == "individual":
                                name = "differential"
                            rgb = bytearray(etc_block(rng, name))
                            if mode == 3 and ordinal % 2 == 0:
                                rgb[3] &= ~2
                            blocks.extend((rng.randbytes(8) if mode == 2 else b"") + rgb)
                            modes.append(name)
                        ordinal += 1
                encoded.extend(blocks)
                if mode in (5, 7):
                    for y in range(height):
                        for x in range(width):
                            red, green = signed[(y // 4) * ((width + 3) // 4) + x // 4]
                            at = (y % 4) * 4 + x % 4
                            pixels.extend(struct.pack("<ffff", red[at], green[at] if mode == 7 else 0, 0, 1))
                else:
                    source, output = root / "input.blocks", root / "pixels.rgba"
                    source.write_bytes(blocks)
                    subprocess.run(["dotnet", str(etc_oracle), str(mode), str(width), str(height), str(source), str(output)], check=True)
                    pixels.extend(output.read_bytes())
            fixtures.append(fixture(mode, encoded, pixels, modes=modes))
        ra = dict(fixtures[2], name="Etc2RaAsRg", source_format="Etc2RaAsRg")
        rgba = base64.b64decode(ra["pixels"])
        ra["pixels"] = base64.b64encode(bytes(value for i in range(0, len(rgba), 4) for value in (rgba[i], rgba[i + 3], 0, 255))).decode()
        fixtures.append(ra)
        for mode in range(8, 12):
            encoded, pixels = bytearray(), bytearray()
            hdr, size = mode % 2 == 1, 8 if mode >= 10 else 4
            for level, (width, height) in enumerate(LEVELS):
                source, compressed = root / ("source.hdr" if hdr else "source.png"), root / "output.astc"
                if hdr:
                    source.write_bytes(f"#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y {height} +X {width}\n".encode() +
                                       bytes(value for y in range(height) for x in range(width) for value in (32 + x * 17, 65 + y * 25, 190, 130 + level)))
                else:
                    rgba = bytes(value for y in range(height) for x in range(width) for value in ((x * 31 + level * 60) % 256, y * 49, 180, (x + y) * 21))
                    source.write_bytes(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)) +
                                       chunk(b"IDAT", zlib.compress(b"".join(b"\0" + rgba[y * width * 4:(y + 1) * width * 4] for y in range(height)))) + chunk(b"IEND", b""))
                subprocess.run([str(astc_tool), "-cH" if hdr else "-cl", str(source), str(compressed), f"{size}x{size}", "-fastest", "-j", "1", "-silent"], check=True)
                raw = compressed.read_bytes()
                assert raw[:4] == bytes.fromhex("13aba15c") and raw[4:7] == bytes((size, size, 1))
                encoded.extend(raw[16:])
                output = root / ("output.exr" if hdr else "output.png")
                subprocess.run([str(astc_tool), "-dH" if hdr else "-dl", str(compressed), str(output), "-silent"], check=True)
                if hdr:
                    decoded = root / "output.raw"
                    subprocess.run([str(exr_oracle), "decode", str(output), str(decoded)], check=True)
                    data = decoded.read_bytes()
                    w, h, count, precision = struct.unpack_from("<iiii", data)
                    assert (w, h) == (width, height) and count in (3, 4) and precision in (2, 4)
                    values = struct.unpack("<" + ("e" if precision == 2 else "f") * (width * height * count), data[16:])
                    for at in range(0, len(values), count):
                        pixels.extend(struct.pack("<eeee", *values[at:at + 3], values[at + 3] if count == 4 else 1))
                else:
                    w, h, count, data = read_png(output)
                    assert (w, h, count) == (width, height, 4)
                    pixels.extend(data)
            fixtures.append(fixture(mode, encoded, pixels))
    destination.parent.mkdir(parents=True, exist_ok=True)
    document = dict(etc_oracle_commit="13b4f6ee4906877402ab69fc5a0b51b83c15db44",
                    etc_oracle_sha256=hashlib.sha256(etc_oracle.read_bytes()).hexdigest(),
                    astc_tool_sha256=hashlib.sha256(astc_tool.read_bytes()).hexdigest(),
                    exr_oracle_sha256=hashlib.sha256(exr_oracle.read_bytes()).hexdigest(), fixtures=fixtures)
    destination.write_text(json.dumps(document, indent=2) + "\n")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("etc_oracle", "astc_tool", "exr_oracle", "destination"):
        parser.add_argument(name, type=Path)
    args = parser.parse_args()
    generate(*(getattr(args, name).resolve() for name in ("etc_oracle", "astc_tool", "exr_oracle", "destination")))
