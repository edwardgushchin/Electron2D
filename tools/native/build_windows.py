"""Produce private Windows libraries using the selected MSVC target and pinned SDL DLL."""

import argparse
import os
from pathlib import Path
import platform
import shutil
import subprocess

import build_tls
import build_webrtc
import package
import windows


def build(rid, sdl, artifacts):
    if platform.system() != "Windows" or os.environ.get("VSCMD_ARG_TGT_ARCH") != rid.removeprefix("win-"):
        raise RuntimeError("Windows native production requires the selected MSVC target environment")
    root = package.ROOT
    output = root / "obj/native-windows" / package.configuration()["version"] / rid
    windows.sdl_import_library(sdl, rid, output / "sdl")
    for source, target, name, options in (
            ("src/Servers/Text/Native", "Electron2DTextBreak", "Electron2DTextBreak.dll", []),
            ("tools/audio-native", "FAudio-shared", "FAudio.dll", ["-DBUILD_SDL3=ON", "-DXNASONG=OFF",
                "-DELECTRON2D_SDL_VERSION=" + package.configuration()["sdlVersion"],
                "-DSDL3_LIBRARIES=" + str(output / "sdl/SDL3.lib")]),
            ("tools/enet-native", "Electron2DENet", "Electron2DENet.dll", []),
            ("tools/ktx-native", "Electron2DKTX", "Electron2DKTX.dll", []),
            ("tools/ogv-native", "Electron2DOGV", "Electron2DOGV.dll", []),
            ("tools/webp-native", "Electron2DWebP", "Electron2DWebP.dll", []),
            ("tools/mesh-native", "Electron2DMesh", "Electron2DMesh.dll", []),
            ("tools/render-native", "Electron2DRender", "Electron2DRender.dll", ["-DSDL3_LIBRARIES=" + str(output / "sdl/SDL3.lib")]),
            ("tools/font-native", "freetype", "Electron2DFreeType.dll", [])):
        directory = output / target
        subprocess.run(["cmake", "-S", str(root / source), "-B", str(directory), "-G", "Ninja",
                        "-DCMAKE_BUILD_TYPE=Release", "-DCMAKE_POLICY_DEFAULT_CMP0091=NEW",
                        "-DCMAKE_POLICY_VERSION_MINIMUM=3.5", "-DCMAKE_C_COMPILER=cl", "-DCMAKE_CXX_COMPILER=cl",
                        "-DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded", *options], check=True)
        subprocess.run(["cmake", "--build", str(directory), "--target", target, "--parallel", "2"], check=True)
        if target == "freetype":
            subprocess.run(["cmake", "--build", str(directory), "--target", "Electron2DFreeTypeCheck", "Electron2DHarfBuzzMemoryCheck", "--parallel", "2"], check=True)
            font = next((root / "src/Scene/Theme/Fonts").glob("*.woff2"))
            subprocess.run([str(directory / "Electron2DFreeTypeCheck.exe"), str(font)], check=True)
            subprocess.run([str(directory / "Electron2DHarfBuzzMemoryCheck.exe"), str(font)], check=True)
        if target == "Electron2DOGV":
            subprocess.run(["cmake", "--build", str(directory), "--target", "Electron2DOGVCheck", "Electron2DVorbisResidueCheck", "--parallel", "2"], check=True)
            checks = directory / "checks"
            checks.mkdir(parents=True, exist_ok=True)
            subprocess.run([str(directory / "Electron2DOGVCheck.exe"), str(checks)], check=True)
            subprocess.run([str(directory / "Electron2DVorbisResidueCheck.exe")], check=True)
        destination = artifacts / "runtimes" / rid / "native"
        destination.mkdir(parents=True, exist_ok=True)
        shutil.copy2(directory / name, destination / name)
    build_tls.build(rid, output / "tls")
    for name in ("libcrypto-3-Electron2D.dll", "libssl-3-Electron2D.dll"):
        shutil.copy2(output / "tls" / name, destination / name)
    build_webrtc.build(rid, output / "webrtc", output / "tls")
    shutil.copy2(output / "webrtc/Electron2DWebRTC.dll", destination / "Electron2DWebRTC.dll")
    package.stage(rid, artifacts)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("rid", choices=windows.MACHINES)
    parser.add_argument("sdl", type=Path)
    parser.add_argument("artifacts", type=Path)
    args = parser.parse_args()
    build(args.rid, args.sdl.resolve(), args.artifacts.resolve())
