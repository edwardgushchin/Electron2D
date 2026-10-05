"""Install and execute one APK on an explicitly selected Android test device."""

import argparse
from pathlib import Path
import subprocess
import time
import uuid


ABIS = {"android-x64": "x86_64", "android-x86": "x86", "android-arm64": "arm64-v8a", "android-arm": "armeabi-v7a"}
PACKAGE = "org.electron2d.tests"


def result(log, token):
    marker = "RESULT " + token + " "
    for line in log.splitlines():
        if marker in line:
            status = line.split(marker, 1)[1]
            if status == "PASS":
                return True
            raise RuntimeError("Android contract checks failed: " + status)
    return False


def run(apk, rid, serial, timeout=120):
    adb = ["adb", "-s", serial]

    def command(*args):
        return subprocess.run(adb + list(args), check=True, capture_output=True, text=True, timeout=timeout).stdout

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
                print(f"{rid}: Android contract checks passed on {serial} ({ABIS[rid]}).")
                return
            time.sleep(1)
        raise TimeoutError("Android app did not report completion within the deadline.")
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
