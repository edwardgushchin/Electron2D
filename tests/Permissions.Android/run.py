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
BULK_CASES = ["public-bulk-single", "public-single-bulk", "public-bulk-partial",
              "public-bulk-granted", "public-bulk-finish"]
LOCATION_CASES = ["public-location-allow", "public-location-deny", "public-location-background",
                  "public-location-upgrade", "public-location-overlap-deny"]
SENSOR_CASES = ["public-sensors-allow", "public-sensors-deny", "public-sensors-background"]
UNORDERED_DIALOGS = {"batch", "public-bulk-single"}
EXPECTED_DIALOGS = {
    "grant": [("audio", True)],
    "deny": [("audio", False)],
    "batch": [("audio", True), ("camera", False)],
    "throw": [("audio", True)],
    "public-order": [("audio", True), ("camera", False)],
    "public-retry": [("audio", False), ("audio", True)],
    "public-finish": [],
    "public-bulk-single": [("audio", True), ("camera", False)],
    "public-single-bulk": [("audio", True), ("camera", False)],
    "public-bulk-partial": [("camera", False)],
    "public-bulk-granted": [],
    "public-bulk-finish": [],
    "public-bulk-empty": [],
    "public-location-allow": [("location-foreground", True), ("location-background", False)],
    "public-location-deny": [("location-foreground", False)],
    "public-location-background": [("location-background", False)],
    "public-location-upgrade": [("location-upgrade", True), ("location-background", False)],
    "public-location-overlap-deny": [
        ("location-coarse", False), ("location-foreground", True), ("location-background", False),
    ],
    "public-sensors-allow": [("sensors-foreground", True), ("sensors-background", False)],
    "public-sensors-deny": [("sensors-foreground", False)],
    "public-sensors-background": [("sensors-background", False)],
}


def find_button(dialog, *suffixes):
    return next((node for suffix in suffixes for node in dialog
                 if node.get("resource-id", "").endswith("/" + suffix)), None)


def is_background_settings(dialog):
    labels = {node.get("text", "").lower() for node in dialog}
    return bool({"allow all the time", "all the time"} & labels) and bool(
        {"allow only while using the app", "allow while using the app", "while using the app"} & labels
    )


def identify_dialog(dialog, message):
    message = message.lower()
    if "location" in message or "sensor" in message:
        group = "location" if "location" in message else "sensors"
        keep_foreground = find_button(dialog, "permission_no_upgrade_button",
                                      "permission_no_upgrade_and_dont_ask_again_button")
        if is_background_settings(dialog) or keep_foreground is not None:
            return f"{group}-background"
        if "change to precise location" in message:
            return "location-upgrade"
        if group == "location" and "approximate location" in message and not any(
            node.get("text") == "Precise" for node in dialog
        ):
            return "location-coarse"
        return f"{group}-foreground"
    audio = "record audio" in message or "microphone" in message
    camera = "take pictures" in message or "camera" in message
    assert audio != camera, message
    return "audio" if audio else "camera"


def run(apk, serial, output, modes):
    if not serial.startswith("emulator-"):
        raise ValueError("This test controls only an explicitly selected emulator.")
    output.mkdir(parents=True, exist_ok=False)
    adb = ["adb", "-s", serial]

    def command(*args, check=True, binary=False, timeout=30):
        result = subprocess.run(adb + list(args), capture_output=True, text=not binary, timeout=timeout)
        if check and result.returncode:
            raise RuntimeError(f"adb {args}: {result.stdout!r} {result.stderr!r}")
        return result

    def tap(node):
        coords = [int(value) for value in re.findall(r"\d+", node.get("bounds", ""))]
        assert len(coords) == 4
        command("shell", "input", "tap", str((coords[0] + coords[2]) // 2), str((coords[1] + coords[3]) // 2))

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
            installed = True
            command("install", str(apk), timeout=120)
            if mode in {"public-bulk-partial", "public-bulk-granted"}:
                command("shell", "pm", "grant", PACKAGE, "android.permission.RECORD_AUDIO")
            if mode == "public-bulk-granted":
                command("shell", "pm", "grant", PACKAGE, "android.permission.CAMERA")
            if mode in {"public-location-background", "public-location-upgrade"}:
                command("shell", "pm", "grant", PACKAGE, "android.permission.ACCESS_COARSE_LOCATION")
            if mode == "public-location-background":
                command("shell", "pm", "grant", PACKAGE, "android.permission.ACCESS_FINE_LOCATION")
            if mode == "public-sensors-background":
                command("shell", "pm", "grant", PACKAGE, "android.permission.BODY_SENSORS")
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
                # Some Android versions return zero on a failed dump and leave the previous XML in place.
                if dump.returncode == 0 and "dumped to:" in dump.stdout:
                    xml = command("shell", "cat", "/data/local/tmp/e2d-permission-ui.xml").stdout
                    nodes = list(ET.fromstring(xml).iter("node"))
                    dialog = [node for node in nodes if node.get("package", "").endswith("permissioncontroller")]
                    if any(node.get("text") for node in dialog):
                        message = " ".join(node.get("text", "") for node in dialog)
                        assert "permission result contract" in message.lower(), message
                        kind = identify_dialog(dialog, message)
                        if last_tap is None or time.monotonic() - last_tap > 1:
                            assert len(clicked) < len(expected), f"{mode}: unexpected dialog {message}"
                            if mode in UNORDERED_DIALOGS:
                                assert kind not in clicked, (mode, kind, message)
                                allow = kind == "audio"
                            else:
                                expected_kind, allow = expected[len(clicked)]
                                assert kind == expected_kind, (mode, expected_kind, message)
                            background = kind.endswith("-background")
                            settings = background and is_background_settings(dialog)
                            if not settings:
                                if background:
                                    button = find_button(dialog, "permission_no_upgrade_button",
                                                         "permission_no_upgrade_and_dont_ask_again_button")
                                elif kind == "location-upgrade":
                                    button = next((node for node in dialog
                                                   if node.get("text") == "Change to precise location"), None)
                                elif allow:
                                    button = find_button(dialog, "permission_allow_foreground_only_button",
                                                         "permission_allow_button")
                                else:
                                    button = find_button(dialog, "permission_deny_button")
                                assert button is not None, message
                            if kind == "location-foreground" and allow:
                                precise = next((node for node in dialog if node.get("text") == "Precise"), None)
                                assert precise is not None, message
                                tap(precise)
                            capture = f"{len(clicked) + 1}-{kind}" if mode.startswith("public-") else kind
                            (case / (capture + ".xml")).write_text(xml, encoding="utf-8")
                            (case / (capture + ".png")).write_bytes(command("exec-out", "screencap", "-p", binary=True).stdout)
                            if settings:
                                assert not allow, message
                                command("shell", "input", "keyevent", "KEYCODE_BACK")
                            else:
                                tap(button)
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
            if mode in UNORDERED_DIALOGS:
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
    profile = parser.add_mutually_exclusive_group()
    profile.add_argument("--mode", choices=EXPECTED_DIALOGS)
    profile.add_argument("--public", action="store_true", help="Run the original public OS API scenarios.")
    profile.add_argument("--bulk", action="store_true", help="Run batch OS API scenarios with the standard manifest.")
    profile.add_argument("--location", action="store_true", help="Run batch location scenarios with the location manifest.")
    profile.add_argument("--sensors", action="store_true", help="Run body sensor scenarios with the sensor manifest.")
    args = parser.parse_args()
    modes = SDK_CASES
    if args.mode:
        modes = [args.mode]
    elif args.bulk:
        modes = BULK_CASES
    elif args.location:
        modes = LOCATION_CASES
    elif args.sensors:
        modes = SENSOR_CASES
    elif args.public:
        modes = PUBLIC_CASES
    run(args.apk.resolve(), args.serial, args.output.resolve(), modes)
