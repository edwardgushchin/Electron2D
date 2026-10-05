"""Execute the contract app in a temporary iOS or tvOS simulator."""

import argparse
import json
from pathlib import Path
import subprocess


def select(profiles, platform):
    runtimes = [item for item in profiles["runtimes"] if item.get("isAvailable") and item["name"].startswith(platform + " ")]
    if not runtimes:
        raise RuntimeError(f"No available {platform} simulator runtime; install it with Xcode.")
    runtime = max(runtimes, key=lambda item: tuple(int(part) for part in item["version"].split(".")))
    family = "iPhone" if platform == "iOS" else "Apple TV"
    devices = [item for item in profiles["devicetypes"] if item["name"].startswith(family)]
    if not devices:
        raise RuntimeError(f"No {platform} simulator device type.")
    return runtime["identifier"], devices[-1]["identifier"]


def run(app, platform):
    def command(*args, timeout=180):
        return subprocess.run(["xcrun", "simctl", *args], check=True, capture_output=True, text=True, timeout=timeout).stdout

    runtime, device = select(json.loads(command("list", "--json")), platform)
    udid = command("create", "Electron2D contract tests", device, runtime).strip()
    try:
        command("boot", udid)
        command("bootstatus", udid, "-b", timeout=300)
        command("install", udid, str(app.resolve()))
        output = command("launch", "--console", "--terminate-running-process", udid, "org.electron2d.tests")
        if "ELECTRON2D_RESULT PASS" not in output or "ELECTRON2D_RESULT FAIL" in output:
            raise RuntimeError("Apple app did not report success: " + output)
        print(f"{platform}: simulator contract checks passed.")
    finally:
        subprocess.run(["xcrun", "simctl", "shutdown", udid], capture_output=True, timeout=60)
        command("delete", udid)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("app", type=Path)
    parser.add_argument("platform", choices=("iOS", "tvOS"))
    args = parser.parse_args()
    run(args.app, args.platform)
