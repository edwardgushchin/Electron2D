"""Validate target-native binaries and their source receipts before NuGet packing."""

import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
from check_native_publish import TEXT_EXPORTS, check_private_text

PLATFORMS = {"Linux": ("linux-x64", "linux-arm64"), "MacOS": ("osx-x64", "osx-arm64")}
RIDS = tuple(rid for values in PLATFORMS.values() for rid in values)
LIBRARIES = {"Linux": ("libElectron2DTextBreak.so", "libFAudio.so.0", "libElectron2DENet.so"),
             "MacOS": ("libElectron2DTextBreak.dylib", "libFAudio.0.dylib", "libElectron2DENet.dylib",
                       "libElectron2DCrypto.3.dylib", "libElectron2DSSL.3.dylib")}


def platform(rid):
    return next(name for name, values in PLATFORMS.items() if rid in values)


def macos_exports(path, rid, name):
    data = path.read_bytes()
    cpu = {"osx-x64": 0x01000007, "osx-arm64": 0x0100000c}[rid]
    if data[:4] != b"\xcf\xfa\xed\xfe" or int.from_bytes(data[4:8], "little") != cpu:
        raise ValueError(f"Wrong Mach-O architecture: {path}")
    identity = subprocess.check_output(["otool", "-D", str(path)], text=True).splitlines()[1:]
    if identity != ["@rpath/" + name]:
        raise ValueError(f"Wrong Mach-O identity: {path}: {identity}")
    commands = subprocess.check_output(["otool", "-l", str(path)], text=True)
    if "LC_RPATH" in commands:
        raise ValueError(f"Native package must not retain a build-machine search path: {path}")
    imports = subprocess.check_output(["otool", "-L", str(path)], text=True)
    if "libFAudio" in name and "@rpath/libSDL3.0.dylib" not in imports:
        raise ValueError("FAudio must share the packaged SDL3 core")
    if "Electron2DSSL" in name and "@loader_path/libElectron2DCrypto.3.dylib" not in imports:
        raise ValueError("Private OpenSSL must resolve its own bundled crypto library")
    if "libicu" in imports.lower():
        raise ValueError("Private text must not depend on global ICU")
    symbols = subprocess.check_output(["nm", "-gU", str(path)], text=True)
    return {line.split()[-1].removeprefix("_") for line in symbols.splitlines() if line.strip()}


def configuration():
    props = ET.parse(ROOT / "tools/native-package.props")
    return {"version": props.findtext(".//Electron2DNativePackageVersion"),
            "sdlVersion": props.findtext(".//Electron2DSDLVersion")}


def source_hash():
    files = set()
    for directory in ("src/Vendor/ICU", "src/Vendor/FAudio", "src/Vendor/ENet",
                      "src/Vendor/FastLZ", "src/Servers/Text/Native",
                      "tools/audio-native", "tools/enet-native", "tools/native"):
        files.update(p for p in (ROOT / directory).rglob("*") if p.is_file()
                     and not {"bin", "obj", "__pycache__"} & set(p.relative_to(ROOT).parts))
    files.update((ROOT / "tools").glob("*-native.targets"))
    files.update((ROOT / "tools").glob("native-package.*"))
    digest = hashlib.sha256()
    for path in sorted(files):
        digest.update(path.relative_to(ROOT).as_posix().encode() + b"\0")
        digest.update(path.read_bytes())
    return digest.hexdigest()


def inspect(rid, directory):
    if rid not in RIDS:
        raise ValueError(f"Unsupported native RID: {rid}")
    libraries = LIBRARIES[platform(rid)]
    if {p.name for p in directory.iterdir()} != set(libraries):
        raise ValueError(f"Expected exactly the selected target's private native libraries: {directory}")
    checksums = {}
    for name in libraries:
        path = directory / name
        data = path.read_bytes()
        if rid.startswith("osx-"):
            exports = macos_exports(path, rid, name)
            required = {"BIO_s_dgram_pair", "BIO_new_bio_dgram_pair", "ERR_get_error"} if "Crypto" in name else {"TLS_method", "DTLS_method", "SSL_CTX_new"} if "SSL" in name else TEXT_EXPORTS if "TextBreak" in name else {"e2d_audio_select_output", "e2d_audio_output_latency"} if "FAudio" in name else {
                "e2d_enet_" + item for item in ("callbacks", "create", "destroy", "connect", "service", "flush", "send", "packet", "release", "peer", "stat", "host", "compress")}
            if not required <= exports or ("TextBreak" in name or "ENet" in name) and exports != required:
                raise ValueError(f"Missing or foreign engine ABI exports: {path}")
            checksums[name] = hashlib.sha256(data).hexdigest()
            continue
        if data[:6] != b"\x7fELF\x02\x01" or int.from_bytes(data[18:20], "little") != {"linux-x64": 62, "linux-arm64": 183}[rid]:
            raise ValueError(f"Wrong native architecture: {path}")
        dynamic = subprocess.check_output(["readelf", "--wide", "--dynamic", str(path)], text=True)
        if re.findall(r"\(SONAME\).*\[([^]]+)\]", dynamic) != [name]:
            raise ValueError(f"Wrong SONAME: {path}")
        if re.search(r"\((RPATH|RUNPATH)\)", dynamic):
            raise ValueError(f"Native package must not retain a build-machine search path: {path}")
        if name == "libFAudio.so.0" and "[libSDL3.so.0]" not in dynamic:
            raise ValueError("FAudio must share the packaged SDL3 core")
        symbols = subprocess.check_output(["nm", "-D", "--defined-only", str(path)], text=True)
        exports = {line.split()[-1] for line in symbols.splitlines()}
        required = {"e2d_audio_select_output", "e2d_audio_output_latency"} if name == "libFAudio.so.0" else {
            "e2d_enet_" + item for item in ("callbacks", "create", "destroy", "connect", "service", "flush", "send", "packet", "release", "peer", "stat", "host", "compress")}
        if name != "libElectron2DTextBreak.so" and not required <= exports:
            raise ValueError(f"Missing engine bridge exports: {path}")
        checksums[name] = hashlib.sha256(data).hexdigest()
    if rid.startswith("linux-"):
        check_private_text(rid, directory / libraries[0])
    return checksums


def stage(rid, artifacts):
    receipt = dict(configuration(), sourceSHA256=source_hash(),
                   files=inspect(rid, artifacts / "runtimes" / rid / "native"))
    destination = artifacts / "receipts" / f"{rid}.json"
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(receipt, indent=2) + "\n")
    print(f"{rid}: native closure, architecture, loader identity, bridges and SHA-256 verified")


def check(artifacts, rids, selected_platform="Linux"):
    expected = set(rids)
    if not expected or not expected <= set(PLATFORMS[selected_platform]) or len(expected) != len(rids):
        raise ValueError(f"Invalid package RID selection: {rids}")
    if {p.name for p in (artifacts / "runtimes").iterdir()} != expected:
        raise ValueError("Missing or unexpected native package RID")
    receipts = {}
    config, fingerprint = configuration(), source_hash()
    for rid in sorted(expected):
        receipt = json.loads((artifacts / "receipts" / f"{rid}.json").read_text())
        actual = dict(config, sourceSHA256=fingerprint,
                      files=inspect(rid, artifacts / "runtimes" / rid / "native"))
        if receipt != actual:
            raise ValueError(f"Stale, mismatched or modified native artifact: {rid}")
        receipts[rid] = receipt
    (artifacts / "native-manifest.json").write_text(json.dumps(receipts, indent=2) + "\n")
    print(f"Native package verified: {', '.join(sorted(expected))}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)
    staging = sub.add_parser("stage")
    staging.add_argument("rid", choices=RIDS)
    staging.add_argument("artifacts", type=Path)
    checking = sub.add_parser("check")
    checking.add_argument("artifacts", type=Path)
    checking.add_argument("--rids", default=",".join(PLATFORMS["Linux"]))
    checking.add_argument("--platform", choices=PLATFORMS, default="Linux")
    args = parser.parse_args()
    if args.command == "stage":
        stage(args.rid, args.artifacts)
    else:
        check(args.artifacts, args.rids.split(","), args.platform)
