"""Exercise actual permission dialogs on an explicitly selected Android emulator."""

import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import time
import xml.etree.ElementTree as ET

PACKAGE = "org.electron2d.permissions"


def run(apk, serial, output, modes):
    if not serial.startswith("emulator-"):
        raise ValueError("This test controls only an explicitly selected emulator.")
    output.mkdir(parents=True, exist_ok=False)
    adb = ["adb", "-s", serial]

    def command(*args, check=True, binary=False):
        result = subprocess.run(adb + list(args), capture_output=True, text=not binary, timeout=30)
        if check and result.returncode:
            raise RuntimeError(f"adb {args}: {result.stdout!r} {result.stderr!r}")
        return result

    assert command("shell", "getprop", "ro.kernel.qemu").stdout.strip() == "1"
    assert not command("shell", "pm", "path", PACKAGE, check=False).stdout.strip(), "Test package must initially be absent"
    (output / "device.json").write_text(json.dumps({"serial": serial, "avd": command("emu", "avd", "name").stdout,
        "sdk": command("shell", "getprop", "ro.build.version.sdk").stdout.strip(), "apkSHA256": hashlib.sha256(apk.read_bytes()).hexdigest()}, indent=2) + "\n")
    installed = False
    results = []
    try:
        for mode in modes:
            case = output / mode
            case.mkdir()
            command("install", str(apk))
            installed = True
            (case / "mode").write_text(mode + "\n")
            command("push", str(case / "mode"), "/data/local/tmp/e2d-permission-mode")
            command("shell", "run-as", PACKAGE, "mkdir", "-p", "files")
            command("shell", "run-as", PACKAGE, "cp", "/data/local/tmp/e2d-permission-mode", "files/mode")
            launch = command("shell", "am", "start", "-W", "-n", PACKAGE + "/org.electron2d.GameActivity").stdout
            (case / "launch.txt").write_text(launch)
            assert "Error:" not in launch and "Status: ok" in launch, launch
            pid = command("shell", "pidof", PACKAGE, check=False).stdout.strip()
            assert pid.isdigit(), "The native application must start before permission interaction"
            (case / "pid.txt").write_text(pid + "\n")
            deadline = time.monotonic() + 150
            clicked = set()
            while time.monotonic() < deadline:
                receipt = command("shell", "run-as", PACKAGE, "cat", "files/result", check=False).stdout.strip()
                if receipt:
                    (case / "result.txt").write_text(receipt + "\n")
                    assert receipt.startswith(f"RESULT {mode} PASS"), receipt
                    break
                assert command("shell", "pidof", PACKAGE, check=False).stdout.strip() == pid, "Native host exited before its result"
                dump = command("shell", "uiautomator", "dump", "/data/local/tmp/e2d-permission-ui.xml", check=False)
                if dump.returncode == 0:
                    xml = command("shell", "cat", "/data/local/tmp/e2d-permission-ui.xml").stdout
                    nodes = list(ET.fromstring(xml).iter("node"))
                    dialog = [node for node in nodes if node.get("package", "").endswith("permissioncontroller")]
                    if dialog:
                        message = " ".join(node.get("text", "") for node in dialog)
                        assert "permission result contract" in message.lower(), message
                        audio = "record audio" in message.lower() or "microphone" in message.lower()
                        camera = "take pictures" in message.lower() or "camera" in message.lower()
                        assert audio != camera, message
                        kind = "audio" if audio else "camera"
                        if kind not in clicked:
                            allow = mode != "deny" and audio
                            suffixes = ["permission_allow_foreground_only_button", "permission_allow_button"] if allow else ["permission_deny_button"]
                            button = next((node for suffix in suffixes for node in dialog if node.get("resource-id", "").endswith("/" + suffix)), None)
                            assert button is not None, message
                            coords = [int(value) for value in re.findall(r"\d+", button.get("bounds", ""))]
                            assert len(coords) == 4
                            (case / (kind + ".xml")).write_text(xml)
                            (case / (kind + ".png")).write_bytes(command("exec-out", "screencap", "-p", binary=True).stdout)
                            command("shell", "input", "tap", str((coords[0] + coords[2]) // 2), str((coords[1] + coords[3]) // 2))
                            clicked.add(kind)
                time.sleep(.2)
            else:
                raise TimeoutError(f"{mode}: native result deadline")
            log = command("logcat", "-d", "--pid=" + pid, "-v", "threadtime", "PermissionResult:I", "*:S").stdout
            (case / "logcat.txt").write_text(log)
            assert f"READY {mode} warm=0/0" in log
            assert clicked == ({"audio", "camera"} if mode == "batch" else {"audio"})
            results.append({"mode": mode, "result": receipt, "dialogs": sorted(clicked)})
            print(receipt, flush=True)
            command("shell", "am", "force-stop", PACKAGE)
            command("uninstall", PACKAGE)
            installed = False
        (output / "results.json").write_text(json.dumps(results, indent=2) + "\n")
    finally:
        if installed:
            (output / "failure-logcat.txt").write_text(command("logcat", "-d", "-v", "threadtime", check=False).stdout)
            command("shell", "am", "force-stop", PACKAGE, check=False)
            command("uninstall", PACKAGE, check=False)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("apk", type=Path)
    parser.add_argument("serial")
    parser.add_argument("output", type=Path)
    parser.add_argument("--mode", choices=["grant", "deny", "batch", "throw"])
    args = parser.parse_args()
    run(args.apk.resolve(), args.serial, args.output.resolve(), [args.mode] if args.mode else ["grant", "deny", "batch", "throw"])
