"""Execute the contract app in a temporary iOS or tvOS simulator."""

import argparse
import json
import os
from pathlib import Path
import subprocess
import time
import uuid

from check_rid import check_notices, check_result as result


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
    check_notices(lambda name: (app / "licence" / name).read_bytes())
    print("Apple bundle notices passed.")


def run(app, platform, timeout=120):
    def command(*args, timeout=180, env=None):
        try:
            process = subprocess.run(["xcrun", "simctl", *args], check=True, capture_output=True, text=True, timeout=timeout, env=env)
        except subprocess.CalledProcessError as error:
            raise RuntimeError(f"simctl {' '.join(args)} failed: {error.stdout}{error.stderr}") from error
        return process.stdout

    runtime, device = select(json.loads(command("list", "--json")), platform)
    udid = command("create", "Electron2D contract tests", device, runtime).strip()
    try:
        command("boot", udid)
        command("bootstatus", udid, "-b", timeout=300)
        command("install", udid, str(app.resolve()))
        container = Path(command("get_app_container", udid, "org.electron2d.tests", "data").strip())
        token = uuid.uuid4().hex
        report = container / "tmp" / ("e2d-result-" + token + ".txt")
        env = dict(os.environ, SIMCTL_CHILD_ELECTRON2D_RESULT_PATH=str(report), SIMCTL_CHILD_ELECTRON2D_RUN_TOKEN=token)
        command("launch", "--terminate-running-process", udid, "org.electron2d.tests", env=env)
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            if report.is_file():
                text = report.read_text()
                if text.endswith("\n") and result(text, token):
                    print(f"{platform}: simulator contract checks passed.")
                    return
            time.sleep(.2)
        output = command("spawn", udid, "log", "show", "--last", "2m", "--style", "compact",
                         "--predicate", 'process CONTAINS "Electron2D" OR eventMessage CONTAINS "org.electron2d.tests"')
        started = report.with_name(report.name + ".started")
        output += "\nStartup: " + (started.read_text() if started.is_file() else "managed entry point was not reached")
        if os.environ.get("GITHUB_ACTIONS") == "true":
            crashes = sorted((Path.home() / "Library/Logs/DiagnosticReports").glob("Electron2D.AppleTests*.ips"), key=lambda path: path.stat().st_mtime)
            if crashes:
                output += "\nCrash: " + crashes[-1].read_text()[:4000]
        raise TimeoutError("Apple app did not report completion within the deadline:\n" + output[-8000:])
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
