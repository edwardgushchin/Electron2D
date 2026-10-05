"""Regression checks for CI matrix drift and target artifact rejection."""

from contextlib import redirect_stdout
from io import StringIO
import json
from pathlib import Path
import struct
import subprocess
import shutil
import tempfile
import unittest
from unittest.mock import Mock, patch
from zipfile import ZipFile

import check_rid
import check_status
import rids
import run_android
import run_apple
import run_browser


class Checks(unittest.TestCase):
    def test_aggregate_status_requires_every_expected_dependency_to_pass(self):
        for group in ("build", "tests"):
            names = ("matrix", "native", group)
            passed = {name: {"result": "success"} for name in names}
            check_status.check(passed, group)
            for name in names:
                for result in ("failure", "cancelled", "skipped", "timed_out", None, "unknown"):
                    failed = dict(passed, **{name: {"result": result}})
                    with self.subTest(group=group, dependency=name, result=result), self.assertRaises(RuntimeError):
                        check_status.check(failed, group)
            for invalid in ({}, None, {group: {"result": "success"}}, dict(passed, extra={"result": "success"})):
                with self.assertRaises(ValueError):
                    check_status.check(invalid, group)

    def test_browser_cleanup_stops_only_its_process_group(self):
        for timeout in (False, True):
            process = Mock(pid=12345)
            process.wait.side_effect = [subprocess.TimeoutExpired("fixture", 10), 0] if timeout else [0, 0]
            with self.subTest(timeout=timeout), patch.object(run_browser.os, "killpg") as kill:
                run_browser.stop(process)
                self.assertEqual([call.args for call in kill.call_args_list],
                                 [(12345, run_browser.signal.SIGTERM), (12345, run_browser.signal.SIGKILL)])
                process.terminate.assert_not_called()

    def test_android_apk_notice_paths(self):
        notices = Path(__file__).resolve().parents[2] / "licence"
        with tempfile.TemporaryDirectory() as directory, redirect_stdout(StringIO()):
            apk = Path(directory) / "fixture.apk"
            with ZipFile(apk, "w"):
                pass
            with self.assertRaises(RuntimeError):
                run_android.check_apk(apk)
            with ZipFile(apk, "w") as archive:
                for source in notices.iterdir():
                    if source.is_file() and source.name != "ReferenceData-LICENSE.txt":
                        archive.write(source, "assets/licence/" + source.name)
            run_android.check_apk(apk)

    def test_apple_bundle_requires_notices_and_excludes_source_data(self):
        notices = Path(__file__).resolve().parents[2] / "licence"
        with tempfile.TemporaryDirectory() as directory, redirect_stdout(StringIO()):
            app = Path(directory)
            (app / "licence").mkdir()
            with self.assertRaises(RuntimeError):
                run_apple.check_bundle(app)
            for source in notices.iterdir():
                if source.is_file() and source.name != "ReferenceData-LICENSE.txt":
                    shutil.copyfile(source, app / "licence" / source.name)
            run_apple.check_bundle(app)
            (app / "licence/Electron2D-LICENSE.txt").write_text("changed")
            with self.assertRaises(RuntimeError):
                run_apple.check_bundle(app)
            shutil.copyfile(notices / "Electron2D-LICENSE.txt", app / "licence/Electron2D-LICENSE.txt")
            shutil.copyfile(notices / "ReferenceData-LICENSE.txt", app / "licence/ReferenceData-LICENSE.txt")
            with self.assertRaises(RuntimeError):
                run_apple.check_bundle(app)

    def test_browser_completion_rejects_stale_or_invalid_reports(self):
        for value in (None, {"run": "old", "status": "passed"}, {"run": "current", "status": "unknown"}, {"run": "current", "status": []},
                      {"run": "current", "status": "failed", "error": 7}):
            with self.assertRaises(ValueError):
                run_browser.validate_result(value, "current")
        self.assertEqual(run_browser.validate_result({"run": "current", "status": "passed"}, "current")["status"], "passed")
        self.assertEqual(run_browser.validate_result({"run": "current", "status": "failed", "error": "fixture"}, "current")["status"], "failed")

    def test_mobile_driver_rejects_missing_stale_and_failed_results(self):
        self.assertFalse(run_android.result("RESULT old PASS", "current"))
        self.assertFalse(run_android.result("app crashed", "current"))
        self.assertTrue(run_android.result("I/Electron2DTests: RESULT current PASS", "current"))
        with self.assertRaises(RuntimeError):
            run_android.result("E/Electron2DTests: RESULT current FAIL exception", "current")
        with self.assertRaises(RuntimeError):
            run_apple.select({"runtimes": [], "devicetypes": []}, "iOS")
        profiles = {"runtimes": [{"name": "iOS 26", "version": "26.0", "identifier": "older", "isAvailable": True},
                                  {"name": "iOS 26.1", "version": "26.1", "identifier": "newer", "isAvailable": True,
                                   "supportedDeviceTypes": [{"name": "iPhone 17", "identifier": "phone"}]},
                                  {"name": "iOS 27", "version": "27.0", "identifier": "missing", "isAvailable": False}],
                    "devicetypes": [{"name": "iPhone 17", "identifier": "phone"}, {"name": "iPhone 6s Plus", "identifier": "unsupported"}]}
        self.assertEqual(run_apple.select(profiles, "iOS"), ("newer", "phone"))
        profiles["runtimes"][1]["supportedDeviceTypes"] = []
        with self.assertRaises(RuntimeError):
            run_apple.select(profiles, "iOS")

    def test_apple_report_checks_completion_and_cleans_up(self):
        profiles = {"runtimes": [{"name": "iOS 26", "version": "26.0", "identifier": "runtime", "isAvailable": True,
                                  "supportedDeviceTypes": [{"name": "iPhone 17", "identifier": "phone"}]}],
                    "devicetypes": [{"name": "iPhone 17", "identifier": "phone"}]}
        for status in ("PASS", "FAIL fixture", "missing", "stale"):
            with self.subTest(status=status), tempfile.TemporaryDirectory() as directory, redirect_stdout(StringIO()):
                container = Path(directory)
                (container / "tmp").mkdir()

                def command(args, **kwargs):
                    output = json.dumps(profiles) if args[2] == "list" else "fixture" if args[2] == "create" else directory if args[2] == "get_app_container" else ""
                    if args[2] == "launch" and status != "missing":
                        env = kwargs["env"]
                        token = "old" if status == "stale" else env["SIMCTL_CHILD_ELECTRON2D_RUN_TOKEN"]
                        Path(env["SIMCTL_CHILD_ELECTRON2D_RESULT_PATH"]).write_text("RESULT " + token + " " + status + "\n")
                    if args[2] == "launch":
                        stderr = next(arg.split("=", 1)[1] for arg in args if arg.startswith("--stderr="))
                        Path(stderr).write_text("native launch exception fixture")
                        self.assertTrue(any(arg.startswith("--stdout=") for arg in args))
                    return subprocess.CompletedProcess(args, 0, output, "")

                with patch.object(run_apple.subprocess, "run", side_effect=command) as process:
                    if status == "PASS":
                        run_apple.run(Path("fixture.app"), "iOS")
                    else:
                        with self.assertRaises((RuntimeError, TimeoutError)) as error:
                            run_apple.run(Path("fixture.app"), "iOS", timeout=.01)
                        if status in ("missing", "stale"):
                            self.assertIn("native launch exception fixture", str(error.exception))
                self.assertEqual(process.call_args_list[-2].args[0], ["xcrun", "simctl", "shutdown", "fixture"])
                self.assertEqual(process.call_args_list[-1].args[0], ["xcrun", "simctl", "delete", "fixture"])

    def test_matrix_rejects_missing_duplicate_and_wrong_platform(self):
        rows = rids.matrix()
        for changed in (rows[:-1], rows + [rows[0]], [dict(rows[0], platform="Linux")] + rows[1:]):
            with self.subTest(rows=changed), patch.object(rids.json, "loads", return_value=changed):
                with self.assertRaises(ValueError):
                    rids.matrix()

    def test_artifacts_and_rejections(self):
        for rid in ("linux-x64", "linux-arm64", "osx-x64", "osx-arm64", "win-x86", "win-x64", "win-arm64", "android-arm64", "ios-arm64", "browser-wasm"):
            row = next(item for item in rids.matrix() if item["rid"] == rid)
            with self.subTest(rid=rid), tempfile.TemporaryDirectory() as directory, patch.object(check_rid.native_package, "windows_exports") as windows:
                output = Path(directory)
                platforms = {"Windows", "Linux", "MacOS"} if row["platform"] in {"Windows", "Linux", "MacOS"} else {row["platform"]}
                packages = [{"Identity": f"SDL3-CS.{platform}{suffix}"} for platform in platforms for suffix in ("", ".Image", ".Shadercross")] if row["platform"] != "Web" else []
                if row["platform"] in {"Windows", "Linux", "MacOS"}:
                    packages.append({"Identity": "Electron2D.Native.Linux"})
                    packages.append({"Identity": "Electron2D.Native.MacOS"})
                    packages.append({"Identity": "Electron2D.Native.Windows"})
                profile = {"Properties": {"RuntimeIdentifier": rid, "TargetFramework": row["framework"], "Electron2DNativePlatform": row["platform"]}, "Items": {"PackageReference": packages}}
                profile_file = output / "profile.json"
                profile_file.write_text(json.dumps(profile))
                (output / "Electron2D.dll").write_bytes(b"MZ")
                (output / "Electron2D.xml").write_text("<doc><assembly><name>Electron2D</name></assembly></doc>")
                if row["platform"] == "Windows":
                    native = output / "runtimes" / rid / "native"
                    native.mkdir(parents=True)
                    for name in check_rid.native_package.LIBRARIES["Windows"]:
                        (native / name).write_bytes(b"native PE fixture; parsing covered by test_windows.py")
                if row["platform"] == "Linux":
                    native = output / "runtimes" / rid / "native"
                    native.mkdir(parents=True)
                    header = bytearray(20)
                    header[:6] = b"\x7fELF\x02\x01"
                    struct.pack_into("<H", header, 18, 62 if rid == "linux-x64" else 183)
                    for name in ("libElectron2DTextBreak.so", "libFAudio.so.0", "libElectron2DENet.so"):
                        (native / name).write_bytes(header)
                if row["platform"] == "MacOS":
                    native = output / "runtimes" / rid / "native"
                    native.mkdir(parents=True)
                    header = struct.pack("<II", 0xfeedfacf, 0x01000007 if rid == "osx-x64" else 0x0100000c) + bytes(12)
                    for name in ("libElectron2DTextBreak.dylib", "libFAudio.0.dylib", "libElectron2DENet.dylib", "libElectron2DCrypto.3.dylib", "libElectron2DSSL.3.dylib", "libElectron2DFreeType.dylib"):
                        (native / name).write_bytes(header)
                with redirect_stdout(StringIO()):
                    check_rid.check(rid, output)
                if row["platform"] == "Windows":
                    self.assertEqual(windows.call_count, 6)
                    windows.side_effect = ValueError("Wrong native PE architecture")
                    with self.assertRaises(ValueError):
                        check_rid.check(rid, output)
                    windows.side_effect = None
                profile["Properties"]["RuntimeIdentifier"] = "wrong-rid"
                profile_file.write_text(json.dumps(profile))
                with self.assertRaises(ValueError):
                    check_rid.check(rid, output)
                profile["Properties"]["RuntimeIdentifier"] = rid
                profile["Items"]["PackageReference"].append({"Identity": "SDL3-CS.Wrong"})
                profile_file.write_text(json.dumps(profile))
                with self.assertRaises(ValueError):
                    check_rid.check(rid, output)
                profile["Items"]["PackageReference"].pop()
                profile_file.write_text(json.dumps(profile))
                (output / "libFAudio.so.0").write_bytes(b"foreign or flattened payload")
                with self.assertRaises(ValueError):
                    check_rid.check(rid, output)
                (output / "libFAudio.so.0").unlink()
                if row["platform"] == "Linux":
                    (native / "libFAudio.so.0").write_bytes(b"wrong ABI")
                    with self.assertRaises(ValueError):
                        check_rid.check(rid, output)
                if row["platform"] == "MacOS":
                    (native / "libFAudio.0.dylib").write_bytes(b"wrong ABI")
                    with self.assertRaises(ValueError):
                        check_rid.check(rid, output)


if __name__ == "__main__":
    unittest.main()
