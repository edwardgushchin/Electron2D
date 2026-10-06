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
import xml.etree.ElementTree as ET
from unittest.mock import Mock, patch
from zipfile import ZipFile

import check_rid
import check_status
import rids
import run_android
import run_apple
import run_browser


class Checks(unittest.TestCase):
    def test_runtime_svg_bytes_survive_crlf_checkout(self):
        root = Path(__file__).resolve().parents[2]
        paths = sorted((root / "src/Scene/Theme/Icons").glob("*.svg"))
        self.assertTrue(paths)
        with tempfile.TemporaryDirectory() as directory:
            subprocess.run(["git", "-c", "core.autocrlf=true", "checkout-index", "--prefix=" + directory + "/",
                            *[path.relative_to(root).as_posix() for path in paths]], cwd=root, check=True)
            for path in paths:
                self.assertEqual((Path(directory) / path.relative_to(root)).read_bytes(), path.read_bytes(), str(path))

    def test_native_publish_layout_does_not_relocate_managed_dependencies(self):
        with tempfile.TemporaryDirectory() as directory:
            project = ET.Element("Project")
            group = ET.SubElement(project, "ItemGroup")
            for package in ("Electron2D.Android", "SDL3-CS.Android", "HarfBuzzSharp.NativeAssets.Win32", "MonoGame.Library.FreeType"):
                for asset in ("native", "runtime"):
                    item = ET.SubElement(group, "_ResolvedCopyLocalPublishAssets", Include=f"{package}/{asset}.fixture")
                    ET.SubElement(item, "NuGetPackageId").text = package
                    ET.SubElement(item, "AssetType").text = asset
            ET.SubElement(project, "Import", Project=str(rids.ROOT / "tools/native/Electron2D.targets"))
            fixture = Path(directory) / "fixture.proj"
            ET.ElementTree(project).write(fixture, encoding="unicode")
            result = subprocess.run(["dotnet", "msbuild", str(fixture), "-nologo",
                                     "-p:RuntimeIdentifier=android-x64", "-t:Electron2DNativePublishLayout",
                                     "-getItem:_ResolvedCopyLocalPublishAssets"],
                                    check=True, capture_output=True, text=True)
            for item in json.loads(result.stdout)["Items"]["_ResolvedCopyLocalPublishAssets"]:
                with self.subTest(package=item["NuGetPackageId"], asset=item["AssetType"]):
                    expected = "runtimes/android-x64/native/" if item["AssetType"] == "native" else None
                    self.assertEqual(item.get("DestinationSubDirectory"), expected)

    def test_default_desktop_layout_keeps_universal_macos_native_assets(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            project = ET.Element("Project")
            properties = ET.SubElement(project, "PropertyGroup")
            ET.SubElement(properties, "TargetFramework").text = "net10.0"
            for kind in ("RuntimeTargetsCopyLocalItems", "_ResolvedCopyLocalPublishAssets"):
                group = ET.SubElement(project, "ItemGroup")
                for rid in ("osx", "osx-x64", "osx-arm64", "linux-x64", "win-x64"):
                    item = ET.SubElement(group, kind, Include=f"runtimes/{rid}/native/{rid}.fixture")
                    for name, value in (("RuntimeIdentifier", rid), ("AssetType", "native"),
                                        ("DestinationSubDirectory", f"runtimes/{rid}/native/")):
                        ET.SubElement(item, name).text = value
            ET.SubElement(project, "Import", Project=str(rids.ROOT / "tools/native/Electron2D.targets"))
            fixture = root / "fixture.proj"
            ET.ElementTree(project).write(fixture, encoding="unicode")
            for host in ("osx-x64", "osx-arm64", "linux-x64", "win-x64"):
                with self.subTest(host=host):
                    result = subprocess.run(["dotnet", "msbuild", str(fixture), "-nologo",
                                             "-p:NETCoreSdkRuntimeIdentifier=" + host,
                                             "-t:Electron2DNativeBuildLayout,Electron2DNativePublishLayout",
                                             "-getItem:RuntimeTargetsCopyLocalItems,_ResolvedCopyLocalPublishAssets"],
                                            check=True, capture_output=True, text=True)
                    items = json.loads(result.stdout)["Items"]
                    selected = items["RuntimeTargetsCopyLocalItems"]
                    expected = {host, "osx"} if host.startswith("osx-") else {host}
                    self.assertEqual({item["RuntimeIdentifier"] for item in selected}, expected)
                    self.assertEqual({item["DestinationSubDirectory"] for item in selected}, {f"runtimes/{host}/native/"})
                    universal = next(item for item in items["_ResolvedCopyLocalPublishAssets"] if item["RuntimeIdentifier"] == "osx")
                    self.assertEqual(universal["DestinationSubDirectory"], f"runtimes/{host}/native/" if host.startswith("osx-") else "runtimes/osx/native/")

    def test_browser_static_references_do_not_leak_into_other_rids(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            targets = root / "buildTransitive/Electron2D.Web.targets"
            targets.parent.mkdir()
            shutil.copyfile(rids.ROOT / "tools/native/Electron2D.targets", targets)
            native = root / "runtimes/browser-wasm/native"
            native.mkdir(parents=True)
            for name in check_rid.native_package.LIBRARIES["Web"]:
                (native / name).write_bytes(b"!<arch>\n")
            (root / "licence").mkdir()
            (root / "licence/fixture.txt").write_text("fixture")
            project = ET.Element("Project")
            ET.SubElement(project, "Import", Project=str(targets))
            fixture = root / "fixture.proj"
            ET.ElementTree(project).write(fixture, encoding="unicode")
            for rid in ("browser-wasm", "linux-x64", "ios-arm64"):
                with self.subTest(rid=rid):
                    result = subprocess.run(["dotnet", "msbuild", str(fixture), "-nologo",
                                             "-p:RuntimeIdentifier=" + rid, "-getItem:NativeFileReference,Content"],
                                            check=True, capture_output=True, text=True)
                    references = json.loads(result.stdout)["Items"]["NativeFileReference"]
                    expected = {native / name for name in check_rid.native_package.LIBRARIES["Web"]} if rid == "browser-wasm" else set()
                    self.assertEqual({Path(item["FullPath"]) for item in references}, expected)
                    self.assertEqual(len(references), len(expected))
                    content = json.loads(result.stdout)["Items"]["Content"]
                    self.assertEqual([item["Link"] for item in content], ["wwwroot/licence/fixture.txt"] if rid == "browser-wasm" else [])

    def test_apple_static_references_select_only_the_requested_rid(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            targets = root / "buildTransitive/Electron2D.targets"
            targets.parent.mkdir()
            shutil.copyfile(rids.ROOT / "tools/native/Electron2D.targets", targets)
            rows = [row for row in rids.matrix() if row["platform"] in {"iOS", "tvOS"}]
            for row in rows:
                native = root / "runtimes" / row["rid"] / "native"
                native.mkdir(parents=True)
                for name in check_rid.native_package.ARCHIVES:
                    (native / name).write_bytes(b"!<arch>\n")
            project = ET.Element("Project")
            ET.SubElement(project, "Import", Project=str(targets))
            sdl = root / "sdl-image"
            sdl.mkdir()
            group = ET.SubElement(project, "PropertyGroup")
            ET.SubElement(group, "_SDL3CSNativeImageAppleStaticLibDir").text = str(sdl) + "/"
            group = ET.SubElement(project, "ItemGroup", Condition="'$(TargetPlatformIdentifier)' == 'ios' or '$(TargetPlatformIdentifier)' == 'tvos'")
            for name in ("libSDL3_image.a", "libpng16.a", "libz.a"):
                (sdl / name).write_bytes(b"!<arch>\n")
                ET.SubElement(group, "NativeReference", Include=str(sdl / name))
            foreign = root / "unrelated/libz.a"
            ET.SubElement(group, "NativeReference", Include=str(foreign))
            fixture = root / "fixture.proj"
            ET.ElementTree(project).write(fixture, encoding="unicode")
            for row in rows + [dict(rows[0], platform="Linux")]:
                with self.subTest(rid=row["rid"], platform=row["platform"]):
                    result = subprocess.run(["dotnet", "msbuild", str(fixture), "-nologo",
                                             "-p:RuntimeIdentifier=" + row["rid"],
                                             "-p:TargetPlatformIdentifier=" + row["platform"].lower(),
                                             "-t:Electron2DAppleCodecIdentity", "-getItem:NativeReference,LinkerArgument"],
                                            check=True, capture_output=True, text=True)
                    items = json.loads(result.stdout)["Items"]
                    references = items["NativeReference"]
                    if row["platform"] == "Linux":
                        self.assertEqual(references, [])
                        self.assertEqual(items["LinkerArgument"], [])
                        continue
                    self.assertEqual({Path(item["FullPath"]) for item in references},
                                     {root / "runtimes" / row["rid"] / "native" / name for name in check_rid.native_package.ARCHIVES} | {sdl / "libSDL3_image.a", foreign})
                    self.assertEqual(len(references), len(check_rid.native_package.ARCHIVES) + 2)
                    for item in (item for item in references if Path(item["Identity"]).name in check_rid.native_package.ARCHIVES):
                        self.assertEqual(Path(item["FullPath"]).parent, root / "runtimes" / row["rid"] / "native")
                        self.assertEqual(item["Kind"], "Static")
                        for flag in ("ForceLoad", "SmartLink", "IsCxx"):
                            self.assertEqual(item[flag], "true")
                    self.assertEqual([item["Identity"] for item in items["LinkerArgument"]], ["-framework", "CoreBluetooth"])

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
                run_android.check_apk(apk, "android-arm64")
            with ZipFile(apk, "w") as archive:
                for source in notices.iterdir():
                    if source.is_file() and source.name != "ReferenceData-LICENSE.txt":
                        archive.write(source, "assets/licence/" + source.name)
            with self.assertRaises(RuntimeError):
                run_android.check_apk(apk, "android-arm64")
            for rid, abi in run_android.ABIS.items():
                elf_class, machine = {"android-arm": (1, 40), "android-arm64": (2, 183), "android-x86": (1, 3), "android-x64": (2, 62)}[rid]
                header = bytearray(20); header[:6] = b"\x7fELF" + bytes((elf_class, 1)); struct.pack_into("<H", header, 18, machine)
                native = Path(directory) / (rid + ".apk")
                shutil.copyfile(apk, native)
                with ZipFile(native, "a") as archive:
                    for name in (*run_android.native_package.LIBRARIES["Android"], "libSDL3.so"):
                        archive.writestr(f"lib/{abi}/{name}", header)
                run_android.check_apk(native, rid)
                with self.assertRaises(RuntimeError):
                    run_android.check_apk(native, "android-x86" if rid != "android-x86" else "android-x64")
                with self.assertWarns(UserWarning), ZipFile(native, "a") as archive:
                    archive.writestr(f"lib/{abi}/libSDL3.so", b"foreign duplicate")
                with self.assertRaises(RuntimeError):
                    run_android.check_apk(native, rid)

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

    def test_browser_rejects_missing_changed_and_source_only_notices_before_launch(self):
        notices = rids.ROOT / "licence"
        with tempfile.TemporaryDirectory() as directory, patch.object(run_browser.subprocess, "Popen") as launch:
            root = Path(directory)
            (root / "index.html").write_text("fixture")
            (root / "licence").mkdir()
            with self.assertRaisesRegex(RuntimeError, "Missing bundle notice"):
                run_browser.run(root, "fixture")
            for source in notices.iterdir():
                if source.is_file() and source.name != "ReferenceData-LICENSE.txt":
                    shutil.copyfile(source, root / "licence" / source.name)
            (root / "licence/Electron2D-LICENSE.txt").write_text("changed")
            with self.assertRaisesRegex(RuntimeError, "Changed bundle notice"):
                run_browser.run(root, "fixture")
            shutil.copyfile(notices / "Electron2D-LICENSE.txt", root / "licence/Electron2D-LICENSE.txt")
            shutil.copyfile(notices / "ReferenceData-LICENSE.txt", root / "licence/ReferenceData-LICENSE.txt")
            with self.assertRaisesRegex(RuntimeError, "Source-only"):
                run_browser.run(root, "fixture")
            launch.assert_not_called()

    def test_mobile_driver_rejects_missing_stale_and_failed_results(self):
        self.assertFalse(run_android.result("RESULT old PASS", "current"))
        self.assertFalse(run_android.result("app crashed", "current"))
        self.assertTrue(run_android.result("I/Electron2DTests: RESULT current PASS", "current"))
        with self.assertRaises(RuntimeError):
            run_android.result("E/Electron2DTests: RESULT current FAIL exception", "current")
        with self.assertRaisesRegex(RuntimeError, "resampling fixture"):
            run_android.result("RESULT old PASS\nE/Electron2DTests: RESULT current FAIL outer exception\n"
                               "E/Electron2DTests: ---> resampling fixture", "current")
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

    def test_android_backtrace_uses_only_the_owned_pid_and_bounded_commands(self):
        adb = ["adb", "-s", "emulator-fixture"]
        for pid in ("", "0", "99999999999", "12 34", "12;reboot", "\u0661\u0662"):
            with self.subTest(pid=pid), patch.object(run_android.subprocess, "run",
                    return_value=subprocess.CompletedProcess([], 0, pid, "")) as process:
                self.assertIn("unavailable", run_android.backtrace(adb))
                process.assert_called_once_with(adb + ["shell", "pidof", run_android.PACKAGE],
                                               capture_output=True, text=True, timeout=15)
        for outcome in ("success", "denied", "timeout"):
            with self.subTest(outcome=outcome):
                calls = [subprocess.CompletedProcess([], 0, "123\n", "")]
                calls.append(subprocess.TimeoutExpired("debuggerd", 15, b"partial native frame", "timeout fixture") if outcome == "timeout" else
                             subprocess.CompletedProcess([], int(outcome == "denied"), "native frame fixture", "permission fixture"))
                with patch.object(run_android.subprocess, "run", side_effect=calls) as process:
                    text = run_android.backtrace(adb)
                self.assertIn("timed out" if outcome == "timeout" else "native frame fixture", text)
                if outcome == "timeout":
                    self.assertIn("partial native frame", text)
                    self.assertIn("timeout fixture", text)
                if outcome == "denied":
                    self.assertIn("exit 1", text)
                    self.assertIn("permission fixture", text)
                self.assertEqual(process.call_args_list[-1].args[0], adb + ["shell", "debuggerd", "-b", "123"])
                self.assertEqual(process.call_args_list[-1].kwargs["timeout"], 15)

    def test_apple_report_checks_completion_and_cleans_up(self):
        profiles = {"runtimes": [{"name": "iOS 26", "version": "26.0", "identifier": "runtime", "isAvailable": True,
                                  "supportedDeviceTypes": [{"name": "iPhone 17", "identifier": "phone"}]}],
                    "devicetypes": [{"name": "iPhone 17", "identifier": "phone"}]}
        cases = [(status, False, False) for status in ("PASS", "FAIL fixture", "missing", "stale")]
        cases += [("PASS", True, False), ("FAIL fixture", True, False), ("PASS", True, True)]
        for status, shutdown_timeout, delete_failure in cases:
            with self.subTest(status=status, shutdown_timeout=shutdown_timeout, delete_failure=delete_failure), \
                    tempfile.TemporaryDirectory() as directory, redirect_stdout(StringIO()) as log:
                container = Path(directory)
                (container / "tmp").mkdir()

                def command(args, **kwargs):
                    if args[2] == "shutdown" and shutdown_timeout:
                        raise subprocess.TimeoutExpired(args, kwargs["timeout"])
                    if args[2] == "delete" and delete_failure:
                        raise subprocess.CalledProcessError(1, args, "", "deletion fixture")
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
                    if status == "PASS" and not delete_failure:
                        run_apple.run(Path("fixture.app"), "iOS")
                    else:
                        with self.assertRaises((RuntimeError, TimeoutError)) as error:
                            run_apple.run(Path("fixture.app"), "iOS", timeout=.01)
                        if status in ("missing", "stale"):
                            self.assertIn("native launch exception fixture", str(error.exception))
                        elif delete_failure:
                            self.assertIn("deletion fixture", str(error.exception))
                        else:
                            self.assertIn("FAIL fixture", str(error.exception))
                self.assertEqual(process.call_args_list[-2].args[0], ["xcrun", "simctl", "shutdown", "fixture"])
                self.assertEqual(process.call_args_list[-1].args[0], ["xcrun", "simctl", "delete", "fixture"])
                if shutdown_timeout:
                    self.assertIn("shutdown timed out; attempting deletion", log.getvalue())

    def test_apple_setup_timeout_preserves_progress_and_cleans_up(self):
        profiles = {"runtimes": [{"name": "iOS 26", "version": "26.0", "identifier": "runtime", "isAvailable": True,
                                  "supportedDeviceTypes": [{"name": "iPhone 17", "identifier": "phone"}]}]}
        for phase in ("bootstatus", "install"):
            with self.subTest(phase=phase), redirect_stdout(StringIO()) as log:
                def command(args, **kwargs):
                    if args[2] == phase:
                        raise subprocess.TimeoutExpired(args, kwargs["timeout"], b"Waiting on system app launch\n", "setup fixture blocked\n")
                    output = json.dumps(profiles) if args[2] == "list" else "fixture" if args[2] == "create" else ""
                    return subprocess.CompletedProcess(args, 0, output, "")

                with patch.object(run_apple.subprocess, "run", side_effect=command) as process:
                    with self.assertRaises(TimeoutError) as error:
                        run_apple.run(Path("fixture.app"), "iOS")
                self.assertIn(phase, str(error.exception))
                self.assertIn("300" if phase == "bootstatus" else "180", str(error.exception))
                self.assertIn("Waiting on system app launch", str(error.exception))
                self.assertIn("setup fixture blocked", str(error.exception))
                self.assertIn("runtime", log.getvalue())
                self.assertIn("phone", log.getvalue())
                self.assertEqual(sum(call.args[0][2] == phase for call in process.call_args_list), 1)
                self.assertEqual(process.call_args_list[-2].args[0], ["xcrun", "simctl", "shutdown", "fixture"])
                self.assertEqual(process.call_args_list[-1].args[0], ["xcrun", "simctl", "delete", "fixture"])

    def test_matrix_rejects_missing_duplicate_and_wrong_platform(self):
        rows = rids.matrix()
        for changed in (rows[:-1], rows + [rows[0]], [dict(rows[0], platform="Linux")] + rows[1:]):
            with self.subTest(rows=changed), patch.object(rids.json, "loads", return_value=changed):
                with self.assertRaises(ValueError):
                    rids.matrix()
        for label in (None, "", "  ", 1, rows[1]["label"]):
            changed = [dict(rows[0], label=label)] + rows[1:]
            with self.subTest(label=label), patch.object(rids.json, "loads", return_value=changed):
                with self.assertRaises(ValueError):
                    rids.matrix()

    def test_artifacts_and_rejections(self):
        for row in rids.matrix():
            rid = row["rid"]
            with self.subTest(rid=rid), tempfile.TemporaryDirectory() as directory:
                output = Path(directory)
                profile = {"Properties": {"RuntimeIdentifier": rid, "TargetFramework": row["framework"],
                                         "Electron2DNativePlatform": row["platform"]},
                           "Items": {"PackageReference": []}}
                profile_file = output / "profile.json"
                profile_file.write_text(json.dumps(profile))
                (output / "Electron2D.dll").write_bytes(b"MZ")
                (output / "Electron2D.xml").write_text("<doc><assembly><name>Electron2D</name></assembly></doc>")
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
                profile["Items"]["PackageReference"].clear()
                profile_file.write_text(json.dumps(profile))
                flat = output / "libFAudio.so.0"
                flat.write_bytes(b"native payload in a managed-only product")
                with self.assertRaises(ValueError):
                    check_rid.check(rid, output)
                flat.unlink()
                native = output / "runtimes" / rid / "native"
                native.mkdir(parents=True)
                (native / "foreign.so").write_bytes(b"native payload in a managed-only product")
                with self.assertRaises(ValueError):
                    check_rid.check(rid, output)


if __name__ == "__main__":
    unittest.main()
