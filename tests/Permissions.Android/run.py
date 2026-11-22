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
SDK_CASES = ["grant", "deny", "batch", "throw"]
PUBLIC_CASES = ["public-order", "public-retry", "public-finish"]
EXPECTED_DIALOGS = {
    "grant": [("audio", True)],
    "deny": [("audio", False)],
    "batch": [("audio", True), ("camera", False)],
    "throw": [("audio", True)],
    "public-order": [("audio", True), ("camera", False)],
    "public-retry": [("audio", False), ("audio", True)],
    "public-finish": [],
}


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
            clicked = []
            expected = EXPECTED_DIALOGS[mode]
            last_tap = None
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
                        if last_tap is None or time.monotonic() - last_tap > 1:
                            assert len(clicked) < len(expected), f"{mode}: unexpected dialog {message}"
                            if mode == "batch":
                                assert kind not in clicked, (mode, kind, message)
                                allow = kind == "audio"
                            else:
                                expected_kind, allow = expected[len(clicked)]
                                assert kind == expected_kind, (mode, expected_kind, message)
                            suffixes = ["permission_allow_foreground_only_button", "permission_allow_button"] if allow else ["permission_deny_button"]
                            button = next((node for suffix in suffixes for node in dialog if node.get("resource-id", "").endswith("/" + suffix)), None)
                            assert button is not None, message
                            coords = [int(value) for value in re.findall(r"\d+", button.get("bounds", ""))]
                            assert len(coords) == 4
                            capture = f"{len(clicked) + 1}-{kind}" if mode in PUBLIC_CASES else kind
                            (case / (capture + ".xml")).write_text(xml, encoding="utf-8")
                            (case / (capture + ".png")).write_bytes(command("exec-out", "screencap", "-p", binary=True).stdout)
                            command("shell", "input", "tap", str((coords[0] + coords[2]) // 2), str((coords[1] + coords[3]) // 2))
                            clicked.append(kind)
                            last_tap = time.monotonic()
                time.sleep(.2)
            else:
                raise TimeoutError(f"{mode}: native result deadline")
            log = command("logcat", "-d", "--pid=" + pid, "-v", "threadtime", "PermissionResult:I", "*:S").stdout
            (case / "logcat.txt").write_text(log)
            if mode in SDK_CASES:
                assert f"READY {mode} warm=0/0" in log
            expected_kinds = [kind for kind, _ in expected]
            if mode == "batch":
                assert sorted(clicked) == sorted(expected_kinds), (mode, clicked)
            else:
                assert clicked == expected_kinds, (mode, clicked)
            results.append({"mode": mode, "result": receipt, "dialogs": clicked})
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
    parser.add_argument("--mode", choices=SDK_CASES + PUBLIC_CASES)
    parser.add_argument("--public", action="store_true", help="Run only the public OS API scenarios.")
    args = parser.parse_args()
    modes = [args.mode] if args.mode else PUBLIC_CASES if args.public else SDK_CASES
    run(args.apk.resolve(), args.serial, args.output.resolve(), modes)
