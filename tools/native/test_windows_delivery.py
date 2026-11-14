"""Exercise real MSBuild migration of retired Windows build/publish payloads."""

from pathlib import Path
import os
import shutil
import subprocess
import tempfile
import time
import unittest
from xml.sax.saxutils import escape

import package


class WindowsDeliveryTests(unittest.TestCase):
    def test_current_application_harfbuzz_assets_are_preserved(self):
        for item in ("NativeCopyLocalItems", "RuntimeTargetsCopyLocalItems"):
            for relative in ("", "runtimes/win-x64/native/"):
                with self.subTest(item=item, relative=relative), tempfile.TemporaryDirectory() as temporary:
                    root = Path(temporary)
                    selected = {"libHarfBuzzSharp.dll", "libHarfBuzzSharp.pdb"}
                    for output in ("build", "publish"):
                        for directory in (root / output, root / output / "runtimes/win-x64/native"):
                            directory.mkdir(parents=True, exist_ok=True)
                            for name in package.retired_windows_files():
                                (directory / name).write_bytes(b"retired")
                        for name in selected:
                            (root / output / relative / name).write_bytes(b"application dependency")
                    assets = ''.join(f'<{item} Include="package/{name}" NuGetPackageId="HarfBuzzSharp.NativeAssets.Win32" DestinationSubDirectory="{relative}" />'
                        f'<ResolvedFileToPublish Include="package/{name}" RelativePath="{relative}{name}" />' for name in selected)
                    project = root / "Consumer.proj"
                    project.write_text('<Project><PropertyGroup><RuntimeIdentifier>win-x64</RuntimeIdentifier>'
                        f'<TargetDir>{escape(str(root / "build"))}/</TargetDir>'
                        f'<PublishDir>{escape(str(root / "publish"))}/</PublishDir></PropertyGroup>'
                        f'<ItemGroup>{assets}</ItemGroup>'
                        f'<Import Project="{escape(str(package.ROOT / "tools/native/WindowsNative.targets"))}" />'
                        '<Target Name="CopyFilesToOutputDirectory" /><Target Name="Publish" /></Project>')
                    result = subprocess.run(["dotnet", "msbuild", str(project), "-nologo",
                        "-target:CopyFilesToOutputDirectory;Publish"], capture_output=True, text=True)
                    self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                    for output in ("build", "publish"):
                        files = {path.relative_to(root / output).as_posix(): path.read_bytes()
                                 for path in (root / output).rglob("*") if path.is_file()}
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
                        "-target:CopyFilesToOutputDirectory;Publish"], capture_output=True, text=True)
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
