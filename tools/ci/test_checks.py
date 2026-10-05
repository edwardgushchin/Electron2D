"""Regression checks for CI matrix drift and target artifact rejection."""

from contextlib import redirect_stdout
from io import StringIO
import json
from pathlib import Path
import struct
import tempfile
import unittest
from unittest.mock import patch

import check_rid
import rids
import run_android
import run_apple
import run_browser


class Checks(unittest.TestCase):
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
                                  {"name": "iOS 26.1", "version": "26.1", "identifier": "newer", "isAvailable": True},
                                  {"name": "iOS 27", "version": "27.0", "identifier": "missing", "isAvailable": False}],
                    "devicetypes": [{"name": "iPhone 17", "identifier": "phone"}]}
        self.assertEqual(run_apple.select(profiles, "iOS"), ("newer", "phone"))

    def test_matrix_rejects_missing_duplicate_and_wrong_platform(self):
        rows = rids.matrix()
        for changed in (rows[:-1], rows + [rows[0]], [dict(rows[0], platform="Linux")] + rows[1:]):
            with self.subTest(rows=changed), patch.object(rids.json, "loads", return_value=changed):
                with self.assertRaises(ValueError):
                    rids.matrix()

    def test_artifacts_and_rejections(self):
        for rid in ("linux-x64", "linux-arm64", "win-x86", "android-arm64", "ios-arm64", "browser-wasm"):
            row = next(item for item in rids.matrix() if item["rid"] == rid)
            with self.subTest(rid=rid), tempfile.TemporaryDirectory() as directory:
                output = Path(directory)
                platforms = {"Windows", "Linux", "MacOS"} if row["platform"] in {"Windows", "Linux", "MacOS"} else {row["platform"]}
                packages = [{"Identity": f"SDL3-CS.{platform}{suffix}"} for platform in platforms for suffix in ("", ".Image", ".Shadercross")] if row["platform"] != "Web" else []
                profile = {"Properties": {"RuntimeIdentifier": rid, "TargetFramework": row["framework"], "Electron2DNativePlatform": row["platform"]}, "Items": {"PackageReference": packages}}
                profile_file = output / "profile.json"
                profile_file.write_text(json.dumps(profile))
                (output / "Electron2D.dll").write_bytes(b"MZ")
                (output / "Electron2D.xml").write_text("<doc><assembly><name>Electron2D</name></assembly></doc>")
                if row["platform"] == "Linux":
                    native = output / "runtimes" / rid / "native"
                    native.mkdir(parents=True)
                    header = bytearray(20)
                    header[:6] = b"\x7fELF\x02\x01"
                    struct.pack_into("<H", header, 18, 62 if rid == "linux-x64" else 183)
                    for name in ("libElectron2DTextBreak.so", "libFAudio.so.0", "libElectron2DENet.so"):
                        (native / name).write_bytes(header)
                with redirect_stdout(StringIO()):
                    check_rid.check(rid, output)
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


if __name__ == "__main__":
    unittest.main()
