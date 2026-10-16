"""Generate small deterministic KTX2 oracles with the official standalone ktx tool."""

import argparse
import base64
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import tempfile


def generate(tool, destination):
    fixtures = []
    with tempfile.TemporaryDirectory() as directory:
        root = Path(directory)
        profiles = [
            ("raw-rgba", "R8G8B8A8_UNORM", [], False),
            ("raw-rgba-zstd", "R8G8B8A8_UNORM", ["--zstd", "3"], False),
            ("raw-rgba-zlib", "R8G8B8A8_UNORM", ["--zlib", "3"], False),
            ("basis-lz", "R8G8B8A8_UNORM", ["--encode", "basis-lz"], False),
            ("uastc", "R8G8B8A8_UNORM", ["--encode", "uastc"], False),
            ("uastc-zstd", "R8G8B8A8_UNORM", ["--encode", "uastc", "--zstd", "3"], False),
            ("raw-hdr", "R16G16B16_SFLOAT", [], True),
            ("hdr-4x4", "R16G16B16_SFLOAT", ["--encode", "uastc-hdr-4x4"], True),
            ("hdr-6x6i", "R16G16B16_SFLOAT", ["--encode", "uastc-hdr-6x6i"], True),
        ]
        for name, pixel_format, options, hdr in profiles:
            inputs = []
            for level, (width, height) in enumerate(((7, 5), (3, 2), (1, 1))):
                path = root / f"{name}-{level}.raw"
                if hdr:
                    data = b"".join(struct.pack("<eee", 0.5 + x * 0.15, 1.5 + y * 0.2, 3.5 + level) for y in range(height) for x in range(width))
                else:
                    data = bytes(value for y in range(height) for x in range(width)
                                 for value in (32 + x * 20, 50 + y * 25, 170, (x + y) * 19 + 30))
                path.write_bytes(data)
                inputs.append(str(path))
            source = root / (name + ".ktx2")
            command = [str(tool), "create", "--testrun", "--format", pixel_format, "--raw", "--width", "7", "--height", "5", "--levels", "3", *options, *inputs, str(source)]
            subprocess.run(command, check=True, stdout=subprocess.DEVNULL)
            decoded = source
            if "--encode" in options:
                decoded = root / (name + "-decoded.ktx2")
                subprocess.run([str(tool), "transcode", "--testrun", "--target", "rgb16f" if hdr else "rgba8", str(source), str(decoded)], check=True, stdout=subprocess.DEVNULL)
            pixels = bytearray()
            for level in range(3):
                raw = root / (name + f"-output-{level}.raw")
                subprocess.run([str(tool), "extract", "--raw", "--level", str(level), str(decoded), str(raw)], check=True, stdout=subprocess.DEVNULL)
                pixels.extend(raw.read_bytes())
            fixtures.append(dict(name=name, width=7, height=5, mipmaps=True, format="Rgbh" if hdr else "Rgba8",
                                 encoded=base64.b64encode(source.read_bytes()).decode(), pixels=base64.b64encode(pixels).decode()))
            print(name, len(source.read_bytes()), len(pixels), flush=True)
    version = subprocess.check_output([str(tool), "--version"], text=True).strip()
    invalid = bytearray(base64.b64decode(next(f for f in fixtures if f["name"] == "basis-lz")["encoded"]))
    global_offset = struct.unpack_from("<Q", invalid, 64)[0]
    endpoint_bytes, selector_bytes, table_bytes = struct.unpack_from("<III", invalid, global_offset + 4)
    levels = struct.unpack_from("<I", invalid, 40)[0]
    table = global_offset + 20 + 20 * levels + endpoint_bytes + selector_bytes
    invalid[table + 2] ^= 185
    invalid[table + table_bytes - 2] ^= 189
    descriptor = bytearray(base64.b64decode(fixtures[0]["encoded"]))
    dfd = struct.unpack_from("<I", descriptor, 48)[0]
    descriptor[dfd + 30] = 148
    failures = [dict(name="invalid-basis-codebook", encoded=base64.b64encode(invalid).decode()),
                dict(name="invalid-dfd-sample-length", encoded=base64.b64encode(descriptor).decode())]
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(dict(generator=version, tool_sha256=hashlib.sha256(tool.read_bytes()).hexdigest(),
                                          fixtures=fixtures, failures=failures), indent=2) + "\n")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("tool", type=Path)
    parser.add_argument("destination", type=Path)
    args = parser.parse_args()
    generate(args.tool.resolve(), args.destination.resolve())
