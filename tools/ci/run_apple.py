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
    devices = [item for item in runtime.get("supportedDeviceTypes", []) if item["name"].startswith(family)]
    if not devices:
        raise RuntimeError(f"No compatible {platform} simulator device type for {runtime['identifier']}.")
    return runtime["identifier"], devices[0]["identifier"]


def check_bundle(app):
    notices = Path(__file__).resolve().parents[2] / "licence"
    for source in notices.iterdir():
        target = app / "licence" / source.name
        if source.is_file() and source.name != "ReferenceData-LICENSE.txt":
            if not target.is_file() or target.read_bytes() != source.read_bytes():
                raise RuntimeError(f"Missing or changed Apple bundle notice: {source.name}")
    if (app / "licence/ReferenceData-LICENSE.txt").exists():
        raise RuntimeError("Source-only reference-data notice must not be in an Apple bundle.")
    print("Apple bundle notices passed.")


def run(app, platform):
    def command(*args, timeout=180, console=False):
        try:
            process = subprocess.run(["xcrun", "simctl", *args], check=True, capture_output=True, text=True, timeout=timeout)
        except subprocess.CalledProcessError as error:
            raise RuntimeError(f"simctl {' '.join(args)} failed: {error.stdout}{error.stderr}") from error
        return process.stdout + process.stderr if console else process.stdout

    runtime, device = select(json.loads(command("list", "--json")), platform)
    udid = command("create", "Electron2D contract tests", device, runtime).strip()
    try:
        command("boot", udid)
        command("bootstatus", udid, "-b", timeout=300)
        command("install", udid, str(app.resolve()))
        output = command("launch", "--console", "--terminate-running-process", udid, "org.electron2d.tests", console=True)
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
    parser.add_argument("--bundle-only", action="store_true", help="Check notices without launching a physical-device app.")
    args = parser.parse_args()
    check_bundle(args.app)
    if not args.bundle_only:
        run(args.app, args.platform)
