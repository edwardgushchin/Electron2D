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
from check_native_publish import TEXT_EXPORTS, WEBRTC_EXPORTS, WEBP_EXPORTS, check_private_text, check_private_webrtc, check_private_webp

PLATFORMS = {"Linux": ("linux-x64", "linux-arm64"), "MacOS": ("osx-x64", "osx-arm64"),
             "Windows": ("win-x86", "win-x64", "win-arm64"),
             "Android": ("android-arm", "android-arm64", "android-x86", "android-x64"),
             "iOS": ("ios-arm64", "iossimulator-arm64", "iossimulator-x64"),
             "tvOS": ("tvos-arm64", "tvossimulator-arm64", "tvossimulator-x64"), "Web": ("browser-wasm",)}
RIDS = tuple(rid for values in PLATFORMS.values() for rid in values)
LIBRARIES = {"Linux": ("libElectron2DTextBreak.so", "libFAudio.so.0", "libElectron2DENet.so", "libElectron2DWebRTC.so", "libElectron2DMesh.so", "libElectron2DRender.so", "libElectron2DWebP.so", "libElectron2DOGV.so", "libElectron2DKTX.so"),
             "MacOS": ("libElectron2DTextBreak.dylib", "libFAudio.0.dylib", "libElectron2DENet.dylib", "libElectron2DWebRTC.dylib", "libElectron2DMesh.dylib", "libElectron2DRender.dylib", "libElectron2DWebP.dylib", "libElectron2DOGV.dylib", "libElectron2DKTX.dylib",
                       "libElectron2DCrypto.3.dylib", "libElectron2DSSL.3.dylib", "libElectron2DFreeType.dylib"),
             "Windows": ("Electron2DTextBreak.dll", "FAudio.dll", "Electron2DENet.dll", "Electron2DWebRTC.dll", "Electron2DMesh.dll", "Electron2DRender.dll", "Electron2DWebP.dll", "Electron2DOGV.dll", "Electron2DKTX.dll",
                         "libcrypto-3-Electron2D.dll", "libssl-3-Electron2D.dll", "Electron2DFreeType.dll")}
LIBRARIES["Android"] = ("libElectron2DTextBreak.so", "libFAudio.so", "libElectron2DENet.so", "libElectron2DMesh.so", "libElectron2DRender.so", "libElectron2DWebP.so", "libElectron2DOGV.so", "libElectron2DKTX.so",
                        "libElectron2DCrypto.so", "libElectron2DSSL.so", "libElectron2DFreeType.so", "libElectron2DHarfBuzz.so")
ARCHIVES = ("libElectron2DTextBreak.a", "libFAudio.a", "libElectron2DENet.a", "libElectron2DMesh.a", "libElectron2DRender.a", "libElectron2DWebP.a", "libElectron2DOGV.a", "libElectron2DKTX.a", "libwebp.a", "libsharpyuv.a", "libElectron2DCrypto.a",
            "libElectron2DSSL.a", "libElectron2DFreeType.a", "libElectron2DHarfBuzz.a", "libwebpdemux.a", "libElectron2DZlib.a",
            "libElectron2DPNG.a", "libElectron2DBrotliDec.a", "libElectron2DBrotliCommon.a", "libElectron2DZstd.a")
LIBRARIES.update(iOS=ARCHIVES, tvOS=ARCHIVES, Web=(*ARCHIVES, "libSDL3.a", "libElectron2DWasmCompat.a",
                 "libSDL3_image.a", "libwebpmux.a"))


def required_exports(name):
    if "Electron2DWebRTC" in name:
        return WEBRTC_EXPORTS
    if "Electron2DKTX" in name:
        return {"e2d_ktx_create", "e2d_ktx_level", "e2d_ktx_destroy", "e2d_basis_create", "e2d_block_decode", "e2d_image_encode", "e2d_image_encoded_free"}
    if "Electron2DOGV" in name:
        return {"e2d_ogv_create", "e2d_ogv_frame", "e2d_ogv_finish", "e2d_ogv_destroy",
                "e2d_video_create", "e2d_video_frame", "e2d_video_audio", "e2d_video_seek_audio", "e2d_video_destroy"}
    if "Electron2DWebP" in name:
        return WEBP_EXPORTS
    if "FreeType" in name:
        return {"FT_Init_FreeType", "FT_New_Memory_Face", "FT_Load_Glyph"}
    if "Crypto" in name or "crypto" in name:
        return {"BIO_s_dgram_pair", "BIO_new_bio_dgram_pair", "ERR_get_error"}
    if "SSL" in name or "ssl" in name:
        return {"TLS_method", "DTLS_method", "SSL_CTX_new"}
    if "TextBreak" in name:
        return TEXT_EXPORTS
    if "FAudio" in name:
        return {"e2d_audio_select_output", "e2d_audio_output_latency", "e2d_audio_create_offline_master", "e2d_audio_mix_offline"}
    if "Electron2DRender" in name:
        return {"e2d_render_raster_support", "e2d_render_raster_pipeline", "e2d_render_line_support", "e2d_render_line_pipeline", "e2d_render_empty_support", "e2d_render_empty_create", "e2d_render_empty_begin", "e2d_render_empty_release", "e2d_render_graphics_storage_support", "e2d_render_storage_barrier", "e2d_render_shader16_support", "e2d_render_memory", "e2d_render_sampler", "e2d_render_sampler_view", "e2d_render_limits", "e2d_render_filter_support", "e2d_render_texture_format"}
    if "Electron2DMesh" in name:
        return {"e2d_mesh_scale", "e2d_mesh_simplify"}
    if "ENet" in name:
        return {"e2d_enet_" + item for item in ("callbacks", "create", "destroy", "connect", "service", "flush",
                                             "send", "packet", "release", "peer", "stat", "host", "compress")} | {"e2d_file_encode", "e2d_file_decode"}
    if "HarfBuzz" in name:
        return {"hb_shape", "hb_buffer_create", "hb_font_create"}
    if name == "libSDL3.a":
        return {"SDL_Init", "SDL_OpenAudioDeviceStream", "SDL_PutAudioStreamData"}
    if name == "libSDL3_image.a":
        return {"IMG_LoadBMP_IO", "IMG_LoadJPG_IO", "IMG_LoadSizedSVG_IO", "IMG_LoadTGA_IO", "IMG_LoadWEBP_IO"}
    if name == "libwebp.a":
        return {"WebPDecodeRGBAInto", "WebPEncode", "WebPPictureImportRGBA", "WebPPictureFree", "WebPConfigInitInternal", "WebPPictureInitInternal", "WebPValidateConfig"}
    if name == "libwebpdemux.a":
        return {"WebPDemuxInternal", "WebPAnimDecoderNewInternal", "WebPAnimDecoderGetNext", "WebPAnimDecoderDelete"}
    if name == "libwebpmux.a":
        return {"WebPNewInternal"}
    if name == "libsharpyuv.a":
        return {"SharpYuvConvert"}
    if "WasmCompat" in name:
        return {"__wasm_setjmp", "__wasm_setjmp_test"}
    return set()


def archive_exports(path, rid):
    if path.read_bytes()[:8] != b"!<arch>\n":
        raise ValueError(f"Expected a static native archive: {path}")
    if rid == "browser-wasm":
        headers = subprocess.check_output(["llvm-readobj", "--file-headers", str(path)], text=True)
        architectures = re.findall(r"^Arch: (.+)$", headers, re.MULTILINE)
        if not architectures or set(architectures) != {"wasm32"} or headers.count("Format: WASM") != len(architectures):
            raise ValueError(f"Foreign or non-Wasm object in archive: {path}")
        symbols = subprocess.check_output(["llvm-nm", "--extern-only", "--defined-only", "--format=posix", str(path)], text=True, stderr=subprocess.PIPE)
        return {line.split()[0] for line in symbols.splitlines() if len(line.split()) == 4}
    headers = subprocess.check_output(["otool", "-hv", str(path)], text=True)
    processors = re.findall(r"MH_MAGIC_64\s+(\S+)", headers)
    expected_cpu = "X86_64" if rid.endswith("-x64") else "ARM64"
    commands = subprocess.check_output(["otool", "-l", str(path)], text=True)
    platforms = re.findall(r"cmd LC_BUILD_VERSION\s+cmdsize \d+\s+platform (\d+)", commands)
    expected_platform = {"ios-arm64": "2", "iossimulator-arm64": "7", "iossimulator-x64": "7",
                         "tvos-arm64": "3", "tvossimulator-arm64": "8", "tvossimulator-x64": "8"}[rid]
    if not processors or set(processors) != {expected_cpu} or len(platforms) != len(processors) or set(platforms) != {expected_platform}:
        raise ValueError(f"Foreign CPU or device/simulator platform in archive: {path}")
    symbols = subprocess.check_output(["nm", "-gU", str(path)], text=True)
    return {line.split()[-1].removeprefix("_") for line in symbols.splitlines() if len(line.split()) == 3}


def windows_exports(path, rid, name):
    from windows import inspect
    exports, imports = inspect(path, rid, name)
    system = {"kernel32.dll", "ntdll.dll", "advapi32.dll", "bcrypt.dll", "crypt32.dll", "user32.dll",
              "ws2_32.dll", "gdi32.dll", "shell32.dll", "ole32.dll", "winmm.dll"}
    private = {library.lower() for library in LIBRARIES["Windows"]} | {"sdl3.dll"}
    if any(dependency not in system | private and not dependency.startswith("api-ms-win-") for dependency in imports):
        raise ValueError(f"Unbundled private DLL dependency: {path}: {imports - system - private}")
    if name == "FAudio.dll" and "sdl3.dll" not in imports:
        raise ValueError("FAudio must share the packaged SDL3 core")
    if name == "libssl-3-Electron2D.dll" and "libcrypto-3-electron2d.dll" not in imports:
        raise ValueError("Private OpenSSL must resolve its own bundled crypto library")
    if "FreeType" in name and imports & (private - {name.lower()}):
        raise ValueError("Private FreeType codecs and auto-hinting dependencies must be statically linked")
    return exports


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
    if "FreeType" in name and any(dependency in imports.lower() for dependency in
                                  ("libpng", "libz.", "libbrotli", "libharfbuzz")):
        raise ValueError("Private FreeType codecs and auto-hinting dependencies must be statically linked")
    symbols = subprocess.check_output(["nm", "-gU", str(path)], text=True)
    return {line.split()[-1].removeprefix("_") for line in symbols.splitlines() if line.strip()}


def configuration():
    props = ET.parse(ROOT / "tools/native-package.props")
    return {"version": props.findtext(".//Electron2DVersion"),
            "minimumEngineVersion": props.findtext(".//Electron2DVersion"),
            "sdlVersion": props.findtext(".//Electron2DSDLVersion")}


def source_hash():
    files = set()
    for directory in ("src/Vendor/ICU", "src/Vendor/MSDFGen", "src/Vendor/PCRE2", "src/Vendor/FAudio", "src/Vendor/ENet",
                      "src/Vendor/FastLZ", "src/Vendor/libdatachannel", "src/Vendor/meshoptimizer", "src/Servers/Text/Native",
                      "tools/audio-native", "tools/enet-native", "tools/webrtc-native", "tools/mesh-native", "tools/render-native", "tools/webp-native", "tools/ogv-native", "tools/ktx-native", "tools/font-native", "tools/native"):
        files.update(p for p in (ROOT / directory).rglob("*") if p.is_file()
                     and not {"bin", "obj", "__pycache__"} & set(p.relative_to(ROOT).parts))
    files.update((ROOT / "tools").glob("*-native.targets"))
    files.update((ROOT / "tools").glob("native-package.*"))
    files.add(ROOT / ".github/workflows/native.yml")
    files.add(ROOT / "tools/check_native_publish.py")
    digest = hashlib.sha256()
    for path in sorted(files, key=lambda item: item.relative_to(ROOT).as_posix()):
        digest.update(path.relative_to(ROOT).as_posix().encode() + b"\0")
        digest.update(path.read_bytes())
    return digest.hexdigest()


def elf_exports(path):
    symbols = subprocess.check_output(["nm", "-D", "--defined-only", str(path)], text=True)
    return {line.split()[-1].split("@@", 1)[0] for line in symbols.splitlines() if line.strip()}


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
        if name.endswith(".a"):
            exports = archive_exports(path, rid)
            if not required_exports(name) <= exports:
                raise ValueError(f"Missing engine or dependency ABI in archive: {path}")
            if "Electron2DKTX" in name and any(symbol.startswith(("ZSTD", "XXH", "POOL_", "FSE_", "HUF_", "HIST_", "ZDICT_", "COVER_", "FASTCOVER_", "ERR_")) for symbol in exports):
                raise ValueError(f"KTX static archive contains global codec symbols: {path}")
            if "TextBreak" in name and any(symbol.startswith(("pcre2_", "_pcre2_")) for symbol in exports):
                raise ValueError(f"Private text archive contains application-visible PCRE2 symbols: {path}")
            checksums[name] = hashlib.sha256(data).hexdigest()
            continue
        if rid.startswith(("osx-", "win-")):
            exports = windows_exports(path, rid, name) if rid.startswith("win-") else macos_exports(path, rid, name)
            required = required_exports(name)
            if not required <= exports or ("TextBreak" in name or "ENet" in name or "Electron2DWebRTC" in name or "Electron2DMesh" in name or "Electron2DRender" in name or "Electron2DOGV" in name or "Electron2DKTX" in name) and exports != required:
                raise ValueError(f"Missing or foreign engine ABI exports: {path}")
            checksums[name] = hashlib.sha256(data).hexdigest()
            continue
        machines = {"linux-x64": (2, 62), "linux-arm64": (2, 183), "android-arm": (1, 40),
                    "android-arm64": (2, 183), "android-x86": (1, 3), "android-x64": (2, 62)}
        elf_class, machine = machines[rid]
        if data[:6] != b"\x7fELF" + bytes((elf_class, 1)) or int.from_bytes(data[18:20], "little") != machine:
            raise ValueError(f"Wrong native architecture: {path}")
        dynamic = subprocess.check_output(["readelf", "--wide", "--dynamic", str(path)], text=True)
        if re.findall(r"\(SONAME\).*\[([^]]+)\]", dynamic) != [name]:
            raise ValueError(f"Wrong SONAME: {path}")
        if re.search(r"\((RPATH|RUNPATH)\)", dynamic):
            raise ValueError(f"Native package must not retain a build-machine search path: {path}")
        if name == "libFAudio.so.0" and "[libSDL3.so.0]" not in dynamic:
            raise ValueError("FAudio must share the packaged SDL3 core")
        exports = elf_exports(path)
        required = required_exports(name)
        if ("Electron2DWebRTC" in name or "Electron2DMesh" in name or "Electron2DRender" in name or "Electron2DOGV" in name or "Electron2DKTX" in name) and exports != required:
            raise ValueError(f"Private codec/mesh must export only its engine bridge: {path}")
        if name != "libElectron2DTextBreak.so" and not required <= exports:
            raise ValueError(f"Missing engine bridge exports: {path}: {required - exports}")
        if rid.startswith("android-"):
            segments = subprocess.check_output(["readelf", "--wide", "--program-headers", str(path)], text=True)
            alignments = [int(line.split()[-1], 16) for line in segments.splitlines() if line.strip().startswith("LOAD ")]
            if not alignments or min(alignments) < 16384:
                raise ValueError(f"Android native ELF must support 16 KB pages: {path}")
            dependencies = set(re.findall(r"\(NEEDED\).*\[([^]]+)\]", dynamic))
            allowed = set(libraries) | {"libSDL3.so", "libc.so", "libm.so", "libdl.so", "liblog.so"}
            if not dependencies <= allowed:
                raise ValueError(f"Unbundled Android dependency: {path}: {dependencies - allowed}")
            if name == "libFAudio.so" and "libSDL3.so" not in dependencies:
                raise ValueError("FAudio must share the packaged Android SDL core")
            if name == "libElectron2DSSL.so" and "libElectron2DCrypto.so" not in dependencies:
                raise ValueError("Android OpenSSL must use its private crypto library")
            if name == "libElectron2DTextBreak.so" and exports != TEXT_EXPORTS:
                raise ValueError("Private Android ICU must export only its engine bridge")
        checksums[name] = hashlib.sha256(data).hexdigest()
    if rid.startswith("linux-"):
        check_private_text(rid, directory / libraries[0])
        check_private_webrtc(rid, directory / "libElectron2DWebRTC.so")
        check_private_webp(rid, directory / "libElectron2DWebP.so")
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
    sub.add_parser("fingerprint")
    staging = sub.add_parser("stage")
    staging.add_argument("rid", choices=RIDS)
    staging.add_argument("artifacts", type=Path)
    checking = sub.add_parser("check")
    checking.add_argument("artifacts", type=Path)
    checking.add_argument("--rids", default=",".join(PLATFORMS["Linux"]))
    checking.add_argument("--platform", choices=PLATFORMS, default="Linux")
    args = parser.parse_args()
    if args.command == "fingerprint":
        print(source_hash())
    elif args.command == "stage":
        stage(args.rid, args.artifacts)
    else:
        check(args.artifacts, args.rids.split(","), args.platform)
