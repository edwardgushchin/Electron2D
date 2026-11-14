"""Exercise real MSBuild migration of retired Windows build/publish payloads."""

from pathlib import Path
import subprocess
import tempfile
import unittest
from xml.sax.saxutils import escape

import package


class WindowsDeliveryTests(unittest.TestCase):
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
                for stale in (False, True):
                    for output in ("build", "publish"):
                        directory = root / output / "runtimes" / rid / "native"
                        directory.mkdir(parents=True, exist_ok=True)
                        for name in expected | (set(package.retired_windows_files()) if stale else set()):
                            (directory / name).write_bytes(name.encode())
                    result = subprocess.run(["dotnet", "msbuild", str(project), "-nologo",
                        "-target:CopyFilesToOutputDirectory;Publish"], capture_output=True, text=True)
                    self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                    for output in ("build", "publish"):
                        directory = root / output / "runtimes" / rid / "native"
                        self.assertEqual({path.name for path in directory.iterdir()}, expected)
                        self.assertTrue(all(path.read_bytes() == path.name.encode() for path in directory.iterdir()))

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
