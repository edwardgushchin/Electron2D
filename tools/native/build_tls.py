"""Build pinned private OpenSSL libraries with relocatable platform identities."""

import argparse
import hashlib
import os
import platform
from pathlib import Path
import shutil
import shlex
import subprocess
import tarfile
import urllib.request

VERSION = "3.6.4"
SHA256 = "9bffaa1ad1e07b354c21bd3324ec02fa15579f45a7d0494b3e74bc449b7333ef"
TARGETS = {"osx-x64": ("x86_64", "darwin64-x86_64-cc"),
           "osx-arm64": ("arm64", "darwin64-arm64-cc"),
           "win-x86": ("x86", "electron2d-win-x86"),
           "win-x64": ("x64", "electron2d-win-x64"),
           "win-arm64": ("arm64", "electron2d-win-arm64")}
LIBRARIES = {"libcrypto.3.dylib": "libElectron2DCrypto.3.dylib",
             "libssl.3.dylib": "libElectron2DSSL.3.dylib"}


def checked_archive(path):
    if hashlib.sha256(path.read_bytes()).hexdigest() != SHA256:
        raise ValueError("The OpenSSL source archive does not match its pinned SHA-256")


def build(rid, output):
    machine, target = TARGETS[rid]
    windows = rid.startswith("win-")
    if windows:
        if platform.system() != "Windows" or os.environ.get("VSCMD_ARG_TGT_ARCH") != machine:
            raise RuntimeError("OpenSSL native production requires the selected Windows compiler architecture")
    elif platform.system() != "Darwin" or platform.machine() != machine:
        raise RuntimeError("OpenSSL native production requires the matching macOS runner")
    source, directory = prepare(output)
    if windows:
        environment = dict(os.environ, OPENSSL_LOCAL_CONFIG_DIR=str(Path(__file__).resolve().parent))
        subprocess.run(["perl", str(source / "Configure"), target, "shared", "no-tests", "no-docs", "no-asm", "no-uplink", "enable-static-vcruntime",
                        "--prefix=" + str(output / "install")], cwd=directory, env=environment, check=True)
        subprocess.run(["nmake", "/nologo", "build_libs"], cwd=directory, check=True)
        for name in ("libcrypto-3-Electron2D.dll", "libssl-3-Electron2D.dll"):
            shutil.copy2(directory / name, output / name)
        print(f"{rid}: pinned OpenSSL {VERSION} built with private DLL identities")
        return
    subprocess.run(["perl", str(source / "Configure"), target, "shared", "no-tests", "no-docs",
                    "--prefix=/Electron2D", "--openssldir=/etc/ssl"], cwd=directory, check=True)
    subprocess.run(["make", "-j4", "build_libs"], cwd=directory, check=True)
    for original, name in LIBRARIES.items():
        path = output / name
        shutil.copy2(directory / original, path)
        subprocess.run(["install_name_tool", "-id", "@rpath/" + name, str(path)], check=True)
        imports = subprocess.check_output(["otool", "-L", str(path)], text=True).splitlines()[1:]
        for line in imports:
            dependency = line.strip().split(" (compatibility version", 1)[0]
            if Path(dependency).name in LIBRARIES:
                subprocess.run(["install_name_tool", "-change", dependency,
                                "@loader_path/" + LIBRARIES[Path(dependency).name], str(path)], check=True)
        subprocess.run(["codesign", "--force", "--sign", "-", str(path)], check=True)
    print(f"{rid}: pinned OpenSSL {VERSION}, private identities and relative crypto dependency built")


def prepare(output):
    output.mkdir(parents=True, exist_ok=True)
    archive = output / ("openssl-" + VERSION + ".tar.gz")
    if not archive.exists():
        temporary = archive.with_suffix(".part")
        urllib.request.urlretrieve("https://github.com/openssl/openssl/releases/download/openssl-" + VERSION + "/" + archive.name, temporary)
        checked_archive(temporary)
        temporary.replace(archive)
    checked_archive(archive)
    source = output / ("openssl-" + VERSION)
    if not source.exists():
        with tarfile.open(archive) as contents:
            contents.extractall(output, filter="data")
    directory = output / "build"
    directory.mkdir(exist_ok=True)
    return source, directory


def cross(rid, output, environment, flags):
    source, directory = prepare(output)
    android = rid.startswith("android-")
    target = {"android-arm": "android-arm", "android-arm64": "android-arm64",
              "android-x86": "android-x86", "android-x64": "android-x86_64"}.get(rid,
              "electron2d-wasm" if rid == "browser-wasm" else "electron2d-apple")
    environment = dict(environment, OPENSSL_LOCAL_CONFIG_DIR=str(Path(__file__).resolve().parent))
    if rid != "browser-wasm" and not android:
        flags = ["CFLAGS=" + " ".join(shlex.quote(flag) for flag in flags)]
    options = ["shared" if android else "no-shared", "no-tests", "no-docs", "no-asm", "no-dso"]
    if rid == "browser-wasm":
        options.extend(["no-threads", "no-uplink", "no-async", "no-afalgeng"])
    if android:
        options.extend(["-D__ANDROID_API__=21", "-Wl,-z,max-page-size=16384"])
    subprocess.run(["perl", str(source / "Configure"), target, *options, *flags],
                   cwd=directory, env=environment, check=True)
    subprocess.run(["make", "-j2", "build_libs"], cwd=directory, env=environment, check=True)
    for original, private in (("crypto", "Crypto"), ("ssl", "SSL")):
        name = "libElectron2D" + private + (".so" if android else ".a")
        shutil.copy2(directory / ("lib" + original + (".so.3" if android else ".a")), output / name)
        if android:
            subprocess.run(["patchelf", "--page-size", "16384", "--set-soname", name, str(output / name)], check=True)
            if original == "ssl":
                subprocess.run(["patchelf", "--page-size", "16384", "--replace-needed", "libcrypto.so.3", "libElectron2DCrypto.so", str(output / name)], check=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("rid", choices=TARGETS)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    build(args.rid, args.output.resolve())
