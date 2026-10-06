"""Install and execute one APK on an explicitly selected Android test device."""

import argparse
from pathlib import Path
import subprocess
import struct
import time
import uuid
from zipfile import ZipFile

from check_rid import check_notices, check_result as result, native_package


ABIS = {"android-x64": "x86_64", "android-x86": "x86", "android-arm64": "arm64-v8a", "android-arm": "armeabi-v7a"}
PACKAGE = "org.electron2d.tests"


def system_logcat(adb):
    try:
        process = subprocess.run(adb + ["logcat", "-d", "-b", "all", "-v", "threadtime"],
                                 capture_output=True, text=True, timeout=15)
        return f"Android system logcat exit {process.returncode}:\n" + process.stdout + process.stderr
    except subprocess.TimeoutExpired as error:
        output = "\n".join(value.decode("utf-8", "replace") if isinstance(value, bytes) else value or ""
                           for value in (error.stdout, error.stderr))
        return "Android system logcat timed out after 15 seconds:\n" + output


def backtrace(adb):
    try:
        process = subprocess.run(adb + ["shell", "pidof", PACKAGE], capture_output=True, text=True, timeout=15)
        pid = process.stdout.strip()
        if process.returncode or len(pid) > 10 or not pid.isascii() or not pid.isdecimal() or not 0 < int(pid) <= 2147483647:
            return "Android native backtrace unavailable: no single live test process.\n" + process.stderr
        process = subprocess.run(adb + ["shell", "debuggerd", "-b", pid], capture_output=True, text=True, timeout=15)
        return f"Android native backtrace exit {process.returncode}:\n" + process.stdout + process.stderr
    except subprocess.TimeoutExpired as error:
        output = "\n".join(value.decode("utf-8", "replace") if isinstance(value, bytes) else value or ""
                           for value in (error.stdout, error.stderr))
        return "Android native backtrace timed out after 15 seconds:\n" + output


def check_apk(apk, rid):
    with ZipFile(apk) as archive:
        check_notices(lambda name: archive.read("assets/licence/" + name))
        libraries = {*native_package.LIBRARIES["Android"], "libSDL3.so"}
        expected = {f"lib/{ABIS[rid]}/{name}" for name in libraries}
        found = [name for name in archive.namelist() if Path(name).name in libraries]
        if set(found) != expected or len(found) != len(expected):
            raise RuntimeError("Android APK has missing, duplicate or foreign native payloads.")
        elf_class, machine = {"android-arm": (1, 40), "android-arm64": (2, 183),
                              "android-x86": (1, 3), "android-x64": (2, 62)}[rid]
        for name in found:
            with archive.open(name) as library:
                header = library.read(20)
            if len(header) != 20 or header[:6] != b"\x7fELF" + bytes((elf_class, 1)) or struct.unpack_from("<H", header, 18)[0] != machine:
                raise RuntimeError("Android APK has the wrong native architecture: " + name)
    print("Android APK notices and complete native RID payload passed.")


def run(apk, rid, serial, timeout=120):
    check_apk(apk, rid)
    adb = ["adb", "-s", serial]

    def command(*args):
        process = subprocess.run(adb + list(args), capture_output=True, text=True, timeout=timeout)
        if process.returncode:
            raise RuntimeError("adb command failed: " + process.stdout + process.stderr)
        return process.stdout

    abis = command("shell", "getprop", "ro.product.cpu.abilist").strip().split(",")
    if ABIS[rid] not in abis:
        raise RuntimeError(f"{serial} cannot execute {rid}; device ABIs: {abis}")
    token = uuid.uuid4().hex
    command("install", "-r", "--abi", ABIS[rid], str(apk.resolve()))
    try:
        command("shell", "am", "force-stop", PACKAGE)
        command("shell", "am", "start", "-n", PACKAGE + "/" + PACKAGE + ".MainActivity",
                "--es", "run", token, "--es", "rid", rid)
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            log = command("logcat", "-d", "-v", "brief", "Electron2DTests:I", "*:S")
            if result(log, token):
                print(f"{rid}: Android contract and native checks passed on {serial} ({ABIS[rid]}).")
                return
            time.sleep(1)
        diagnostics = command("logcat", "-d", "-v", "brief", "AndroidRuntime:E", "DEBUG:E", "libc:F", "mono-rt:E", "Electron2DTests:I", "*:S")
        trace = backtrace(adb)
        directory = Path("bin/ci") / rid / "android-diagnostics"
        directory.mkdir(parents=True, exist_ok=True)
        (directory / "logcat.txt").write_text(diagnostics)
        (directory / "native-backtrace.txt").write_text(trace)
        (directory / "system-logcat.txt").write_text(system_logcat(adb))
        raise TimeoutError("Android app did not report completion within the deadline.\n" + diagnostics[-6000:] + "\n" + trace[-6000:])
    finally:
        command("shell", "am", "force-stop", PACKAGE)
        command("uninstall", PACKAGE)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("apk", type=Path)
    parser.add_argument("rid", choices=ABIS)
    parser.add_argument("--serial", required=True)
    args = parser.parse_args()
    run(args.apk, args.rid, args.serial)
