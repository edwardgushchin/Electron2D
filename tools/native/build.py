"""Build the single Electron2D native module against the selected SDL core."""
import argparse
from pathlib import Path
import platform
import shutil
import subprocess
import build_tls
from build_webrtc import copy_header
import package


def tls_options(tls):
    source = tls / ("openssl-" + build_tls.VERSION)
    include = tls / "include"
    for directory in (source / "include", tls / "build/include"):
        shutil.copytree(directory, include, dirs_exist_ok=True, copy_function=copy_header)
    windows = platform.system() == "Windows"
    linux = platform.system() == "Linux"
    crypto = tls / "build/libcrypto.lib" if windows else tls / ("libcrypto.so.4" if linux else "libElectron2DCrypto.4.dylib")
    ssl = tls / "build/libssl.lib" if windows else tls / ("libssl.so.4" if linux else "libElectron2DSSL.4.dylib")
    for library in (crypto, ssl):
        if not library.is_file():
            raise FileNotFoundError(library)
    return ["-DOPENSSL_USE_STATIC_LIBS=FALSE", "-DOPENSSL_INCLUDE_DIR=" + str(include),
            "-DOPENSSL_CRYPTO_LIBRARY=" + str(crypto), "-DOPENSSL_SSL_LIBRARY=" + str(ssl)]


def build(rid, sdl, output, tls=None):
    if not sdl.is_file():
        raise ValueError("The selected target SDL core is required")
    expected = {"Linux": "linux-", "Darwin": "osx-", "Windows": "win-"}[platform.system()]
    if not rid.startswith(expected):
        raise ValueError("The native producer requires the selected target host")
    if tls is None:
        tls = output / "tls"
        build_tls.build(rid, tls)
    options = tls_options(tls)
    if rid.startswith("win-"):
        options += ["-DOPENSSL_ROOT_DIR=" + str(tls / "build"), "-DCMAKE_C_COMPILER=cl", "-DCMAKE_CXX_COMPILER=cl",
                    "-DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded"]
    subprocess.run(["cmake", "-S", str(package.ROOT / "tools/native"), "-B", str(output),
                    "-G", "Ninja", "-DCMAKE_BUILD_TYPE=Release", "-DCMAKE_POLICY_VERSION_MINIMUM=3.5",
                    "-DSDL3_LIBRARIES=" + str(sdl), *options], check=True)
    subprocess.run(["cmake", "--build", str(output), "--target", "Electron2DNative", "--parallel", "2"], check=True)
    if rid.startswith("osx-"):
        library = output / "libElectron2D.dylib"
        imports = subprocess.check_output(["otool", "-L", str(library)], text=True).splitlines()[1:]
        for line in imports:
            dependency = line.strip().split(" (compatibility version", 1)[0]
            if Path(dependency).name in build_tls.LIBRARIES.values():
                subprocess.run(["install_name_tool", "-change", dependency,
                                "@loader_path/" + Path(dependency).name, str(library)], check=True)
        subprocess.run(["codesign", "--force", "--sign", "-", str(library)], check=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("rid", choices=tuple(build_tls.TARGETS))
    parser.add_argument("sdl", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--tls", type=Path)
    args = parser.parse_args()
    build(args.rid, args.sdl.resolve(), args.output.resolve(), args.tls.resolve() if args.tls else None)
