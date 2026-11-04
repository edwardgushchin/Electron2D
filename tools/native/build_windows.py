"""Produce private Windows libraries using the selected MSVC target and pinned SDL DLL."""

import argparse
import os
from pathlib import Path
import platform
import shutil
import subprocess

import package
import windows


def build(rid, sdl, artifacts):
    if platform.system() != "Windows" or os.environ.get("VSCMD_ARG_TGT_ARCH") != rid.removeprefix("win-"):
        raise RuntimeError("Windows native production requires the selected MSVC target environment")
    root = package.ROOT
    output = root / "obj/native-windows" / package.configuration()["version"] / rid
    windows.sdl_import_library(sdl, rid, output / "sdl")
    from build import build as build_engine
    build_engine(rid, output / "sdl/SDL3.lib", output / "engine")
    destination = artifacts / "runtimes" / rid / "native"
    destination.mkdir(parents=True, exist_ok=True)
    shutil.copy2(output / "engine/libElectron2D.dll", destination / "libElectron2D.dll")
    for name in ("libcrypto-4-Electron2D.dll", "libssl-4-Electron2D.dll"):
        shutil.copy2(output / "engine/tls" / name, destination / name)
    for source, target, name, options in (
            ("tools/audio-native", "FAudio-shared", "FAudio.dll", ["-DBUILD_SDL3=ON", "-DXNASONG=OFF",
                "-DELECTRON2D_SDL_VERSION=" + package.configuration()["sdlVersion"],
                "-DSDL3_LIBRARIES=" + str(output / "sdl/SDL3.lib")]),
            ("tools/shaping-native", "harfbuzz", "libHarfBuzzSharp.dll", ["-DELECTRON2D_MEMORY_LIBRARY=" + str(output / "engine/Electron2D.lib")]),
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
        destination = artifacts / "runtimes" / rid / "native"
        destination.mkdir(parents=True, exist_ok=True)
        shutil.copy2(directory / name, destination / name)
    package.stage(rid, artifacts)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("rid", choices=windows.MACHINES)
    parser.add_argument("sdl", type=Path)
    parser.add_argument("artifacts", type=Path)
    args = parser.parse_args()
    build(args.rid, args.sdl.resolve(), args.artifacts.resolve())
