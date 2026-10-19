"""Build the desktop WebRTC bridge against the producer's pinned private OpenSSL."""
import argparse
from pathlib import Path
import shutil
import subprocess
import build_tls
import package


def build(rid, output, tls):
    if rid not in build_tls.TARGETS:
        raise ValueError("Private OpenSSL WebRTC production requires a desktop target")
    source, generated = tls / ("openssl-" + build_tls.VERSION), tls / "build"
    include = output / "openssl-include"
    for directory in (source / "include", generated / "include"):
        shutil.copytree(directory, include, dirs_exist_ok=True)
    windows = rid.startswith("win-")
    crypto = generated / "libcrypto.lib" if windows else tls / "libElectron2DCrypto.3.dylib"
    ssl = generated / "libssl.lib" if windows else tls / "libElectron2DSSL.3.dylib"
    for path in (include / "openssl/ssl.h", include / "openssl/opensslv.h", crypto, ssl):
        if not path.is_file():
            raise ValueError("Build the pinned target OpenSSL before WebRTC: " + str(path))
    options = ["-DCMAKE_C_COMPILER=cl", "-DCMAKE_CXX_COMPILER=cl", "-DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded"] if windows else []
    subprocess.run(["cmake", "-S", str(package.ROOT / "tools/webrtc-native"), "-B", str(output), "-G", "Ninja", "-DCMAKE_BUILD_TYPE=Release", "-DOPENSSL_INCLUDE_DIR=" + str(include), "-DOPENSSL_CRYPTO_LIBRARY=" + str(crypto), "-DOPENSSL_SSL_LIBRARY=" + str(ssl), *options], check=True)
    subprocess.run(["cmake", "--build", str(output), "--target", "Electron2DWebRTC", "--parallel", "2"], check=True)
    if not windows:
        library = output / "libElectron2DWebRTC.dylib"
        for name in (crypto.name, ssl.name):
            subprocess.run(["install_name_tool", "-change", "@rpath/" + name, "@loader_path/" + name, str(library)], check=True)
        subprocess.run(["codesign", "--force", "--sign", "-", str(library)], check=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("rid", choices=build_tls.TARGETS)
    parser.add_argument("output", type=Path)
    parser.add_argument("tls", type=Path)
    args = parser.parse_args()
    build(args.rid, args.output.resolve(), args.tls.resolve())
