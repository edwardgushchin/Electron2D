"""Exercise real MSBuild migration of retired Windows build/publish payloads."""

from pathlib import Path
import os
import hashlib
import json
from itertools import product
import shutil
import subprocess
import tempfile
import time
import unittest
from xml.sax.saxutils import escape

import package


class WindowsDeliveryTests(unittest.TestCase):
    def test_project_copy_items_are_preserved(self):
        sdk = subprocess.check_output(["dotnet", "--version"], text=True).strip()
        sdk_path = subprocess.check_output(["dotnet", "msbuild", str(package.ROOT / "Electron2D.csproj"),
            "-nologo", "-getProperty:MSBuildSDKsPath"], text=True).strip()
        publish_targets = Path(sdk_path) / "Microsoft.NET.Sdk/targets/Microsoft.NET.Publish.targets"
        publish_if_different = "_CopyResolvedFilesToPublishIfDifferent" in publish_targets.read_text(encoding="utf-8")
        for item, relative, mode in product(("None", "Content"), ("", "runtimes/win-x64/native/"),
                                           ("PreserveNewest", "Always", "IfDifferent")):
            with self.subTest(item=item, mode=mode, relative=relative), tempfile.TemporaryDirectory(prefix="project native ") as temporary:
                root = Path(temporary)
                source = root / "application-harfbuzz.bin"
                source.write_bytes(b"application-owned native file")
                checksum = hashlib.sha256(source.read_bytes()).hexdigest()
                project = root / "Consumer.csproj"
                project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
                    '<OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings>'
                    '<TargetFramework>net10.0</TargetFramework><RuntimeIdentifier>win-x64</RuntimeIdentifier>'
                    '</PropertyGroup><ItemGroup>'
                    f'<{item} Include="{source.name}" Link="{relative}libHarfBuzzSharp.dll" '
                    f'CopyToOutputDirectory="{mode}" CopyToPublishDirectory="{mode}" />'
                    '</ItemGroup>'
                    f'<Import Project="{escape(str(package.ROOT / "tools/native/WindowsNative.targets"))}" /></Project>')
                (root / "Program.cs").write_text(
                    f'var file = Path.Combine(AppContext.BaseDirectory, "{relative}libHarfBuzzSharp.dll");'
                    'if (!File.ReadAllBytes(file).AsSpan().SequenceEqual("application-owned native file"u8)) '
                    'throw new Exception("Selected application file mismatch");'
                    'Console.WriteLine("Selected application file matched.");')
                record = dict(sdk=sdk, item=item, relative=relative, mode=mode, sourceSHA256=checksum)
                stages = [("build", root / "bin/Release/net10.0/win-x64")]
                if mode != "IfDifferent" or publish_if_different:
                    stages.append(("publish", root / "publish"))
                else:
                    record["publish"] = "unsupported by SDK"
                for stage, output in stages:
                    command = ["dotnet", stage, str(project), "-c", "Release", "--nologo"]
                    if stage == "publish":
                        command += ["-o", str(output)]
                    result = subprocess.run(command, capture_output=True, text=True)
                    self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                    selected = output / relative / "libHarfBuzzSharp.dll"
                    self.assertEqual(selected.read_bytes(), source.read_bytes())
                    clean_run = subprocess.run(["dotnet", str(output / "Consumer.dll")], capture_output=True, text=True)
                    self.assertEqual(clean_run.returncode, 0, clean_run.stdout + clean_run.stderr)
                    selected.write_bytes(b"x" * len(source.read_bytes()))
                    future = time.time() + 864000
                    os.utime(selected, (future, future))
                    result = subprocess.run([*command, "--no-restore"], capture_output=True, text=True)
                    self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                    self.assertEqual(selected.read_bytes(), source.read_bytes())
                    repeated_run = subprocess.run(["dotnet", str(output / "Consumer.dll")], capture_output=True, text=True)
                    self.assertEqual(repeated_run.returncode, 0, repeated_run.stdout + repeated_run.stderr)
                    record[stage] = dict(cleanSHA256=checksum, repeatedSHA256=hashlib.sha256(selected.read_bytes()).hexdigest(),
                                         cleanRun=clean_run.returncode, repeatedRun=repeated_run.returncode)
                    receipt = output / "Electron2D.application-native.sha256"
                    values = receipt.read_text(encoding="utf-8-sig").split()
                    self.assertEqual([values[0], values[1].replace("\\", "/")],
                        [checksum.upper(), relative + "libHarfBuzzSharp.dll"])
                    record[stage]["receipt"] = receipt.read_text(encoding="utf-8-sig").strip()
                project.write_text(project.read_text().replace(
                    f'CopyToOutputDirectory="{mode}" CopyToPublishDirectory="{mode}"', ''))
                removed = subprocess.run(["dotnet", "build", str(project), "-c", "Release", "--no-restore"], capture_output=True, text=True)
                self.assertEqual(removed.returncode, 0, removed.stdout + removed.stderr)
                self.assertFalse((stages[0][1] / relative / "libHarfBuzzSharp.dll").exists())
                self.assertFalse((stages[0][1] / "Electron2D.application-native.sha256").exists())
                if len(stages) == 2:
                    removed = subprocess.run(["dotnet", "publish", str(project), "-c", "Release", "--no-restore",
                        "-o", str(root / "publish")], capture_output=True, text=True)
                    self.assertEqual(removed.returncode, 0, removed.stdout + removed.stderr)
                    self.assertFalse((root / "publish" / relative / "libHarfBuzzSharp.dll").exists())
                    self.assertFalse((root / "publish/Electron2D.application-native.sha256").exists())
                print("MATRIX " + json.dumps(record), flush=True)

    def test_publish_replaces_future_dated_package_native_files(self):
        with tempfile.TemporaryDirectory(prefix="native publish ") as temporary:
            root = Path(temporary)
            targets = root / "package/buildTransitive"
            targets.mkdir(parents=True)
            shutil.copyfile(package.ROOT / "tools/native/Electron2D.targets", targets / "Electron2D.Windows.targets")
            shutil.copyfile(package.ROOT / "tools/native/WindowsNative.targets", targets / "WindowsNative.targets")
            selected = {name: ("selected " + name).encode() for name in package.LIBRARIES["Windows"]}
            (targets.parent / "native-manifest.json").write_text("selected manifest")
            for name, content in selected.items():
                (targets.parent / name).write_bytes(content)
            assets = ''.join(f'<_ResolvedCopyLocalPublishAssets Include="{escape(str(targets.parent / name))}" '
                'NuGetPackageId="Electron2D.Windows" AssetType="native" DestinationSubDirectory="runtimes/win-x64/native/" />'
                for name in selected)
            project = root / "Consumer.csproj"
            project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
                '<TargetFramework>net10.0</TargetFramework><RuntimeIdentifier>win-x64</RuntimeIdentifier>'
                '</PropertyGroup>'
                f'<Import Project="{escape(str(targets / "Electron2D.Windows.targets"))}" />'
                '<Target Name="SelectFixtureNativeFiles" BeforeTargets="ComputeResolvedFilesToPublishList">'
                f'<ItemGroup>{assets}</ItemGroup></Target></Project>')
            command = ["dotnet", "publish", str(project), "-c", "Release", "--nologo", "-o", str(root / "publish")]
            result = subprocess.run(command, capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            native = root / "publish/runtimes/win-x64/native"
            future = time.time() + 864000
            for name, content in selected.items():
                output = native / name
                self.assertEqual(output.read_bytes(), content)
                output.write_bytes(b"x" * len(content))
                os.utime(output, (future, future))
            manifest = root / "publish/Electron2D.native-manifest.json"
            manifest.write_text("stale manifest")
            os.utime(manifest, (future, future))
            result = subprocess.run([*command, "--no-restore"], capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertEqual(manifest.read_text(), "selected manifest")
            self.assertEqual({name: (native / name).read_bytes() for name in selected}, selected)

    def test_current_application_harfbuzz_assets_are_preserved(self):
        for item in ("NativeCopyLocalItems", "RuntimeTargetsCopyLocalItems"):
            for relative in ("", "runtimes/win-x64/native/"):
                with self.subTest(item=item, relative=relative), tempfile.TemporaryDirectory() as temporary:
                    root = Path(temporary)
                    selected = {"libHarfBuzzSharp.dll", "libHarfBuzzSharp.pdb"}
                    (root / "package").mkdir()
                    for name in selected:
                        (root / "package" / name).write_bytes(b"application dependency")
                    for output in ("build", "publish"):
                        for directory in (root / output, root / output / "runtimes/win-x64/native"):
                            directory.mkdir(parents=True, exist_ok=True)
                            for name in package.retired_windows_files():
                                (directory / name).write_bytes(b"retired")
                        for name in selected:
                            (root / output / relative / name).write_bytes(b"application dependency")
                    assets = ''.join(f'<{item} Include="{escape(str(root / "package" / name))}" NuGetPackageId="HarfBuzzSharp.NativeAssets.Win32" DestinationSubDirectory="{relative}" />'
                        f'<ResolvedFileToPublish Include="{escape(str(root / "package" / name))}" RelativePath="{relative}{name}" />' for name in selected)
                    project = root / "Consumer.proj"
                    project.write_text('<Project><PropertyGroup><RuntimeIdentifier>win-x64</RuntimeIdentifier>'
                        f'<TargetDir>{escape(str(root / "build"))}/</TargetDir>'
                        f'<PublishDir>{escape(str(root / "publish"))}/</PublishDir></PropertyGroup>'
                        f'<ItemGroup>{assets}</ItemGroup>'
                        f'<Import Project="{escape(str(package.ROOT / "tools/native/WindowsNative.targets"))}" />'
                        '<Target Name="CopyFilesToOutputDirectory" /><Target Name="Publish" /></Project>')
                    result = subprocess.run(["dotnet", "msbuild", str(project), "-nologo",
                        "-target:CopyFilesToOutputDirectory;Electron2DWindowsNativePublishCopy;Publish"], capture_output=True, text=True)
                    self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                    for output in ("build", "publish"):
                        files = {path.relative_to(root / output).as_posix(): path.read_bytes()
                                 for path in (root / output).rglob("*") if path.is_file()}
                        if "Electron2D.application-native.sha256" in files:
                            receipt = files.pop("Electron2D.application-native.sha256").decode("utf-8-sig")
                            self.assertEqual({tuple(line.split()) for line in receipt.splitlines()},
                                {(hashlib.sha256(b"application dependency").hexdigest().upper(), relative + name) for name in selected})
                        self.assertEqual(files, {relative + name: b"application dependency" for name in selected})

    def test_build_replaces_future_dated_manifest(self):
        with tempfile.TemporaryDirectory(prefix="native manifest ") as temporary:
            root = Path(temporary)
            targets = root / "package/buildTransitive"
            targets.mkdir(parents=True)
            shutil.copyfile(package.ROOT / "tools/native/Electron2D.targets", targets / "Electron2D.Windows.targets")
            shutil.copyfile(package.ROOT / "tools/native/WindowsNative.targets", targets / "WindowsNative.targets")
            selected = b'{"selected":"package manifest"}'
            (targets.parent / "native-manifest.json").write_bytes(selected)
            project = root / "Consumer.csproj"
            project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
                '<TargetFramework>net10.0</TargetFramework><RuntimeIdentifier>win-x64</RuntimeIdentifier>'
                '</PropertyGroup>'
                f'<Import Project="{escape(str(targets / "Electron2D.Windows.targets"))}" /></Project>')
            command = ["dotnet", "build", str(project), "-c", "Release", "--nologo"]
            built = subprocess.run(command, capture_output=True, text=True)
            self.assertEqual(built.returncode, 0, built.stdout + built.stderr)
            manifest = root / "bin/Release/net10.0/win-x64/Electron2D.native-manifest.json"
            self.assertEqual(manifest.read_bytes(), selected)
            manifest.write_bytes(b'{"stale":"future dated"}')
            future = time.time() + 864000
            os.utime(manifest, (future, future))
            rebuilt = subprocess.run([*command, "--no-restore"], capture_output=True, text=True)
            self.assertEqual(rebuilt.returncode, 0, rebuilt.stdout + rebuilt.stderr)
            self.assertEqual(manifest.read_bytes(), selected)

    def test_clean_and_incremental_outputs_preserve_selected_and_unrelated_files(self):
        for rid in package.PLATFORMS["Windows"]:
            with self.subTest(rid=rid), tempfile.TemporaryDirectory(prefix="native layout ") as temporary:
                root = Path(temporary)
                project = root / "Consumer.proj"
                project.write_text('<Project><PropertyGroup>'
                    f'<RuntimeIdentifier>{rid}</RuntimeIdentifier>'
                    f'<TargetDir>{escape(str(root / "build"))}/</TargetDir>'
                    f'<PublishDir>{escape(str(root / "publish"))}/</PublishDir>'
                    '</PropertyGroup>'
                    f'<Import Project="{escape(str(package.ROOT / "tools/native/WindowsNative.targets"))}" />'
                    '<Target Name="CopyFilesToOutputDirectory" /><Target Name="Publish" /></Project>')
                expected = set(package.LIBRARIES["Windows"]) | {"application-plugin.dll", "SDL3.dll"}
                root_expected = {"application-plugin.dll", "SDL3.dll", "Electron2D.dll"}
                for stale in (False, True):
                    for output in ("build", "publish"):
                        directory = root / output / "runtimes" / rid / "native"
                        directory.mkdir(parents=True, exist_ok=True)
                        for name in expected | (set(package.retired_windows_files()) if stale else set()):
                            (directory / name).write_bytes(name.encode())
                        for name in root_expected | (set(package.retired_windows_files()) if stale else set()):
                            (root / output / name).write_bytes(name.encode())
                    result = subprocess.run(["dotnet", "msbuild", str(project), "-nologo",
                        "-target:CopyFilesToOutputDirectory;Electron2DWindowsNativePublishCopy;Publish"], capture_output=True, text=True)
                    self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                    for output in ("build", "publish"):
                        directory = root / output / "runtimes" / rid / "native"
                        self.assertEqual({path.name for path in directory.iterdir()}, expected)
                        self.assertTrue(all(path.read_bytes() == path.name.encode() for path in directory.iterdir()))
                        files = [path for path in (root / output).iterdir() if path.is_file()]
                        self.assertEqual({path.name for path in files}, root_expected)
                        self.assertTrue(all(path.read_bytes() == path.name.encode() for path in files))

    def test_retired_package_is_rejected_before_copying(self):
        for item in ("NativeCopyLocalItems", "RuntimeTargetsCopyLocalItems"):
            for owner, succeeds in (("Electron2D.Windows", False), ("Application.Plugin", True)):
                with self.subTest(item=item, owner=owner), tempfile.TemporaryDirectory() as temporary:
                    project = Path(temporary) / "Consumer.proj"
                    project.write_text('<Project><PropertyGroup><RuntimeIdentifier>win-x64</RuntimeIdentifier>'
                        '</PropertyGroup><ItemGroup>'
                        f'<{item} Include="old/Electron2DENet.dll" NuGetPackageId="{owner}" />'
                        '</ItemGroup>'
                        f'<Import Project="{escape(str(package.ROOT / "tools/native/WindowsNative.targets"))}" />'
                        '<Target Name="ResolvePackageAssets" /></Project>')
                    result = subprocess.run(["dotnet", "msbuild", str(project), "-nologo",
                        "-target:ResolvePackageAssets"], capture_output=True, text=True)
                    self.assertEqual(result.returncode == 0, succeeds, result.stdout + result.stderr)
                    if not succeeds:
                        self.assertIn("Restore a qualified package", result.stdout)


if __name__ == "__main__":
    unittest.main()
