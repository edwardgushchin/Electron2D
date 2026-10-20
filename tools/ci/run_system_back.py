#!/usr/bin/env python3
"""Exercise the production Android activity through actual emulator back input."""
import argparse
import hashlib
import json
from pathlib import Path
import shlex
import struct
import subprocess
import time


def producer_entries(data, sdk):
    end = data.index(b"*end\n") + 5
    header = data[:end].decode()
    assert "data-file-overflow=false" in header and "clock=dual" in header
    binary = data[end:]
    magic, version, offset, _, size = struct.unpack_from("<4sHHQH", binary)
    assert (magic, version, offset, size) == (b"SLOW", 3, 32, 14)
    assert (len(binary) - offset) % size == 0
    owner, method = (("org.electron2d.GameActivity_BackCallback", "onBackInvoked") if sdk >= 33
                     else ("org.electron2d.GameActivity", "dispatchKeyEvent"))
    ids = {int(parts[0], 16) for line in header.splitlines()
           if len(parts := line.split("\t")) >= 3 and parts[1:3] == [owner, method]}
    return sum(struct.unpack_from("<I", binary, at + 2)[0] in ids
               for at in range(offset, len(binary), size))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--adb", required=True)
    parser.add_argument("--serial", required=True)
    parser.add_argument("--sdk", required=True, type=int)
    parser.add_argument("--apk", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    assert args.serial.startswith("emulator-"), "Use an explicitly selected disposable emulator."
    package = "org.electron2d.systemback"
    command = [args.adb, "-s", args.serial]
    args.output.mkdir(parents=True, exist_ok=True)
    transcript = []

    def adb(*flags, check=True):
        result = subprocess.run(command + list(flags), capture_output=True, text=True, timeout=120)
        transcript.append(dict(args=flags, exit=result.returncode, stdout=result.stdout, stderr=result.stderr))
        if check:
            result.check_returncode()
        return result.stdout

    def wait_for(marker, path):
        until = time.monotonic() + 90
        while time.monotonic() < until:
            log = adb("logcat", "-d", "-v", "brief")
            path.write_text(log)
            assert "RESULT " + mode + " FAIL" not in log and "FATAL EXCEPTION" not in log, log[-12000:]
            if marker in log:
                return log
            time.sleep(.25)
        raise TimeoutError(marker + "\n" + log[-12000:])

    try:
        assert adb("shell", "getprop", "ro.kernel.qemu").strip() == "1"
        assert int(adb("shell", "getprop", "ro.build.version.sdk")) == args.sdk
        assert adb("shell", "getprop", "sys.boot_completed").strip() == "1"
        digest = hashlib.sha256(args.apk.read_bytes()).hexdigest()
        adb("install", "--no-incremental", "-r", str(args.apk.resolve()))
        results = []
        for mode in ("default", "cancel", "blocked", "explicit", "failure"):
            output = args.output / mode
            output.mkdir(exist_ok=True)
            adb("shell", "am", "force-stop", package)
            adb("shell", shlex.join(["run-as", package, "sh", "-c",
                "mkdir -p files; printf %s " + mode + " > files/mode; rm -f files/result"]))
            adb("logcat", "-c")
            adb("shell", "am", "start", "-n", package + "/org.electron2d.GameActivity")
            log = wait_for("READY " + mode + " first", output / "startup.log")
            assert "warm=0/0" in log
            trace = "/data/local/tmp/electron2d-system-back.trace"
            adb("shell", "am", "profile", "start", package, trace)
            requests = 2 if mode in ("cancel", "blocked") else 1
            for index in range(requests):
                if args.sdk < 33 and mode == "default":
                    adb("shell", "input", "keyevent", "--longpress", "4")
                else:
                    adb("shell", "input", "keyevent", "4")
                if index + 1 < requests:
                    wait_for("READY " + mode + " second", output / "first-request.log")
            wait_for("RESULT " + mode + " PASS", output / "result.log")
            adb("shell", "am", "profile", "stop", package)
            # ActivityManager returns before ART finishes writing the trace.
            until = time.monotonic() + 30
            while True:
                adb("pull", trace, str(output / "producer.trace"))
                if b"*end\n" in (output / "producer.trace").read_bytes():
                    break
                assert time.monotonic() < until, "ART did not finish its method trace."
                time.sleep(.25)
            entries = producer_entries((output / "producer.trace").read_bytes(), args.sdk)
            expected_entries = requests if args.sdk >= 33 else (3 if mode == "default" else requests * 2)
            assert entries == expected_entries, (mode, entries, expected_entries)
            result = adb("shell", shlex.join(["run-as", package, "cat", "files/result"])).strip()
            assert result.startswith("RESULT " + mode + " PASS ")
            results.append(dict(mode=mode, result=result, producerEntries=entries))
            print(result, "nativeProducerEntries=" + str(entries), flush=True)
        assert hashlib.sha256(args.apk.read_bytes()).hexdigest() == digest
        (args.output / "check.json").write_text(json.dumps(dict(sdk=args.sdk, serial=args.serial,
            apk=str(args.apk.resolve()), apkSHA256=digest, results=results), indent=2) + "\n")
    finally:
        (args.output / "adb.json").write_text(json.dumps(transcript, indent=2) + "\n")


if __name__ == "__main__":
    main()
