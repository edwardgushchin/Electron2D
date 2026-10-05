"""Build and audit Android, Apple device/simulator and browser native payloads."""

import argparse
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess

import build_tls
import package

ANDROID_ABIS = {"android-arm": "armeabi-v7a", "android-arm64": "arm64-v8a",
                "android-x86": "x86", "android-x64": "x86_64"}
NDK_VERSION = "28.2.13676358"


def profile(rid, ndk):
    environment = dict(os.environ)
    options, flags = [], []
    if rid in ANDROID_ABIS:
        if ndk is None or "Pkg.Revision = " + NDK_VERSION not in (ndk / "source.properties").read_text():
            raise ValueError("Android native production requires pinned NDK " + NDK_VERSION)
        host = {"Linux": "linux-x86_64", "Darwin": "darwin-x86_64"}[platform.system()]
        compiler = ndk / "toolchains/llvm/prebuilt" / host / "bin"
        environment.update(ANDROID_NDK_ROOT=str(ndk), PATH=str(compiler) + os.pathsep + environment["PATH"])
        options = ["-DCMAKE_TOOLCHAIN_FILE=" + str(ndk / "build/cmake/android.toolchain.cmake"),
                   "-DANDROID_ABI=" + ANDROID_ABIS[rid], "-DANDROID_PLATFORM=android-21", "-DANDROID_STL=c++_static",
                   "-DCMAKE_SHARED_LINKER_FLAGS=-Wl,-z,max-page-size=16384"]
    elif rid == "browser-wasm":
        paths = json.loads(subprocess.check_output(["dotnet", "msbuild", str(package.ROOT / "tests/Electron2D.BrowserTests/Electron2D.BrowserTests.csproj"),
                           "-getProperty:EmscriptenSdkToolsPath,EmscriptenNodeToolsPath"], text=True))["Properties"]
        sdk = Path(paths["EmscriptenSdkToolsPath"]).resolve()
        node = Path(paths["EmscriptenNodeToolsPath"]).resolve() / "bin/node"
        environment.update(DOTNET_EMSCRIPTEN_LLVM_ROOT=str(sdk / "bin"), DOTNET_EMSCRIPTEN_BINARYEN_ROOT=str(sdk),
                           DOTNET_EMSCRIPTEN_NODE_JS=str(node), FROZEN_CACHE="", EM_CACHE=str(package.ROOT / "obj/native-wasm-cache"),
                           CC=str(sdk / "emscripten/emcc"), AR=str(sdk / "bin/llvm-ar"), RANLIB=str(sdk / "bin/llvm-ranlib"),
                           PATH=os.pathsep.join((str(sdk / "bin"), str(node.parent), environment["PATH"])))
        options = ["-DCMAKE_TOOLCHAIN_FILE=" + str(sdk / "emscripten/cmake/Modules/Platform/Emscripten.cmake")]
        flags = ["-fwasm-exceptions", "-sSUPPORT_LONGJMP=wasm"]
    else:
        if platform.system() != "Darwin":
            raise RuntimeError("Apple native production requires Xcode on macOS")
        television = rid.startswith("tvos")
        simulator = "simulator" in rid
        sdk_name = ("appletv" if television else "iphone") + ("simulator" if simulator else "os")
        sdk = subprocess.check_output(["xcrun", "--sdk", sdk_name, "--show-sdk-path"], text=True).strip()
        cpu = "x86_64" if rid.endswith("-x64") else "arm64"
        target = cpu + "-apple-" + ("tvos" if television else "ios") + "15.0" + ("-simulator" if simulator else "")
        options = ["-DCMAKE_SYSTEM_NAME=" + ("tvOS" if television else "iOS"), "-DCMAKE_OSX_SYSROOT=" + sdk,
                   "-DCMAKE_OSX_ARCHITECTURES=" + cpu, "-DCMAKE_OSX_DEPLOYMENT_TARGET=15.0"]
        environment.update(CC="clang", AR="ar", RANLIB="ranlib")
        flags = ["-target", target, "-isysroot", sdk]
    return environment, options, flags


def build(rid, artifacts, sdl=None, ndk=None):
    environment, options, flags = profile(rid, ndk)
    root = package.ROOT
    directory = root / "obj/native-cross" / rid
    font = next((root / "src/Scene/Theme/Fonts").glob("*.woff2"))
    if sdl is None and rid != "browser-wasm":
        packages = Path(os.environ.get("NUGET_PACKAGES", Path.home() / ".nuget/packages"))
        sdl = packages / ("sdl3-cs." + package.platform(rid).lower()) / package.configuration()["sdlVersion"] / "runtimes" / rid / "native" / ("libSDL3.so" if rid.startswith("android-") else "libSDL3.a")
    if rid != "browser-wasm" and (sdl is None or not sdl.is_file()):
        raise ValueError("The selected target's restored SDL core is required")
    subprocess.run(["cmake", "-S", str(root / "tools/native/cross"), "-B", str(directory), "-G", "Ninja",
                    "-DCMAKE_BUILD_TYPE=Release", "-DELECTRON2D_SDL_VERSION=" + package.configuration()["sdlVersion"],
                    "-DSDL3_LIBRARIES=" + str(sdl or ""), "-DELECTRON2D_CHECK_FONT=" + str(font), *options],
                   env=environment, check=True)
    audio_target = "FAudio-shared" if rid.startswith("android-") else "FAudio-static"
    subprocess.run(["cmake", "--build", str(directory), "--target", "Electron2DTextBreak", audio_target,
                    "Electron2DENet", "freetype", "harfbuzz", "--parallel", "2"], env=environment, check=True)
    if rid == "browser-wasm":
        subprocess.run(["cmake", "--build", str(directory), "--target", "Electron2DFreeTypeCheck", "Electron2DWasmJumpCheck", "--parallel", "2"], env=environment, check=True)
        subprocess.run([environment["DOTNET_EMSCRIPTEN_NODE_JS"], str(directory / "Electron2DWasmJumpCheck.js")], env=environment, check=True)
        subprocess.run([environment["DOTNET_EMSCRIPTEN_NODE_JS"], str(directory / "Electron2DFreeTypeCheck.js"), "/font.woff2"],
                       env=environment, check=True)
    build_tls.cross(rid, directory / "tls", environment, flags)
    destination = artifacts / "runtimes" / rid / "native"
    destination.mkdir(parents=True, exist_ok=True)
    dependencies = {"libElectron2DZlib.a": "libz.a", "libElectron2DPNG.a": "libpng16.a",
                    "libElectron2DBrotliDec.a": "libbrotlidec.a", "libElectron2DBrotliCommon.a": "libbrotlicommon.a",
                    "libElectron2DZstd.a": "libzstd.a"}
    for name in package.LIBRARIES[package.platform(rid)]:
        candidates = list(directory.rglob(dependencies.get(name, name)))
        if len(candidates) != 1:
            raise ValueError(f"Expected exactly one built artifact for {name}: {candidates}")
        shutil.copy2(candidates[0], destination / name)
    os.environ.update(environment)
    package.stage(rid, artifacts)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("rid", choices=[rid for rid in package.RIDS if package.platform(rid) in ("Android", "iOS", "tvOS", "Web")])
    parser.add_argument("artifacts", type=Path)
    parser.add_argument("--sdl", type=Path)
    parser.add_argument("--ndk", type=Path)
    args = parser.parse_args()
    build(args.rid, args.artifacts.resolve(), args.sdl.resolve() if args.sdl else None, args.ndk.resolve() if args.ndk else None)
