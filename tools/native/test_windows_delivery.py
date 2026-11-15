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
from zipfile import ZipFile

import package


def sdk_host_rid():
    project = Path(__file__).resolve().parents[2] / "Electron2D.csproj"
    query_output = subprocess.check_output(["dotnet", "msbuild", str(project), "-nologo",
        "-getProperty:NETCoreSdkRuntimeIdentifier"], text=True)
    return query_output.strip().splitlines()[-1]


def write_package_consumer(root, explicit_rid, copy_local, rid):
    feed = root / "feed"
    feed.mkdir()
    with ZipFile(feed / "NativeCopyFixture.1.0.0.nupkg", "w") as archive:
        archive.writestr("NativeCopyFixture.nuspec", '<package><metadata><id>NativeCopyFixture</id>'
            '<version>1.0.0</version><authors>Fixture</authors><description>Native copy fixture</description></metadata></package>')
        archive.writestr(f"runtimes/{rid}/native/libHarfBuzzSharp.dll", b"nuget-owned native file")
    (root / "NuGet.Config").write_text('<configuration><packageSources><clear/>'
        '<add key="fixture" value="feed"/></packageSources></configuration>')
    project = root / "Consumer.csproj"
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
        '<OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><TargetFramework>net10.0</TargetFramework>'
        + (f'<RuntimeIdentifier>{rid}</RuntimeIdentifier>' if explicit_rid else '')
        + f'<CopyLocalLockFileAssemblies>{str(copy_local).lower()}</CopyLocalLockFileAssemblies>'
        '<RestorePackagesPath>packages</RestorePackagesPath><NuGetAudit>false</NuGetAudit>'
        '</PropertyGroup><ItemGroup><PackageReference Include="NativeCopyFixture" Version="1.0.0"/></ItemGroup></Project>')
    (root / "Program.cs").write_text('if (args.Length == 2 && !File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, args[0]))'
        '.AsSpan().SequenceEqual(System.Text.Encoding.UTF8.GetBytes(args[1]))) throw new Exception("Native file mismatch");'
        'Console.WriteLine("Native selection consumer passed.");')
    return project, "" if explicit_rid else f"runtimes/{rid}/native/"


class WindowsDeliveryTests(unittest.TestCase):
    def assert_application_receipt(self, output):
        receipt = output / "Electron2D.application-native.sha256"
        lines = receipt.read_text(encoding="utf-8-sig").splitlines() if receipt.exists() else []
        entries = {}
        for line in lines:
            checksum, relative = line.split(maxsplit=1)
            relative = relative.replace("\\", "/")
            self.assertNotIn(relative, entries, "Duplicate output path in receipt")
            self.assertEqual(hashlib.sha256((output / relative).read_bytes()).hexdigest(), checksum.lower())
            entries[relative] = checksum.lower()
        return entries

    def test_build_respects_disabled_package_copying(self):
        rid = sdk_host_rid()
        for explicit_rid in (True, False):
            with self.subTest(explicit_rid=explicit_rid), tempfile.TemporaryDirectory(prefix="native copy disabled ") as temporary:
                root = Path(temporary)
                project, relative = write_package_consumer(root, explicit_rid, False, rid)
                output = root / "bin/Release/net10.0" / (rid if explicit_rid else "")
                command = ["dotnet", "build", str(project), "-c", "Release", "--nologo"]
                baseline = subprocess.run(command, capture_output=True, text=True)
                self.assertEqual(baseline.returncode, 0, baseline.stdout + baseline.stderr)
                selected = output / relative / "libHarfBuzzSharp.dll"
                receipt = output / "Electron2D.application-native.sha256"
                self.assertFalse(selected.exists())
                baseline_publish = root / "baseline-publish"
                baseline = subprocess.run(["dotnet", "publish", str(project), "-c", "Release", "--no-restore",
                    "-o", str(baseline_publish)], capture_output=True, text=True)
                self.assertEqual(baseline.returncode, 0, baseline.stdout + baseline.stderr)
                published_files = list(baseline_publish.rglob("libHarfBuzzSharp.dll"))
                self.assertEqual(len(published_files), 1)
                publish_relative = published_files[0].relative_to(baseline_publish).as_posix()
                project.write_text(project.read_text().replace('</Project>',
                    f'<Import Project="{escape(str(package.ROOT / "tools/native/WindowsNative.targets"))}" /></Project>'))
                for stale in (False, True):
                    if stale:
                        selected.parent.mkdir(parents=True, exist_ok=True)
                        selected.write_bytes(b"retired native file")
                        future = time.time() + 864000
                        os.utime(selected, (future, future))
                        receipt.write_text("stale receipt")
                    result = subprocess.run([*command, "--no-restore"], capture_output=True, text=True)
                    self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                    self.assertFalse(selected.exists(), "Disabled NuGet copy was restored by cleanup")
                    self.assertFalse(receipt.exists())
                    launched = subprocess.run(["dotnet", str(output / "Consumer.dll")], capture_output=True, text=True)
                    self.assertEqual(launched.returncode, 0, launched.stdout + launched.stderr)
                publish = subprocess.run(["dotnet", "publish", str(project), "-c", "Release", "--no-restore",
                    "-o", str(root / "publish")], capture_output=True, text=True)
                self.assertEqual(publish.returncode, 0, publish.stdout + publish.stderr)
                self.assertEqual((root / "publish" / publish_relative).read_bytes(), b"nuget-owned native file")
                values = (root / "publish/Electron2D.application-native.sha256").read_text(encoding="utf-8-sig").split()
                self.assertEqual([values[0], values[1].replace("\\", "/")],
                    [hashlib.sha256(b"nuget-owned native file").hexdigest().upper(), publish_relative])
                print("SELECTION " + json.dumps(dict(case="copy-local-disabled", explicitRID=explicit_rid,
                    build="absent on clean and repeated build", receipt="absent", publish=publish_relative)), flush=True)

    def test_build_matches_sdk_output_collisions(self):
        rid = sdk_host_rid()
        for explicit_rid, item, mode in product((True, False), ("None", "Content"), ("PreserveNewest", "Always", "IfDifferent")):
            with self.subTest(explicit_rid=explicit_rid, item=item, mode=mode), tempfile.TemporaryDirectory(prefix="native collision ") as temporary:
                root = Path(temporary)
                project, relative = write_package_consumer(root, explicit_rid, True, rid)
                source = root / "application.bin"
                source.write_bytes(b"application-owned native file")
                plain = project.read_text().replace('</ItemGroup>',
                    f'<{item} Include="application.bin" Link="{relative}libHarfBuzzSharp.dll" '
                    f'CopyToOutputDirectory="{mode}" CopyToPublishDirectory="Never" /></ItemGroup>')
                project.write_text(plain)
                restored = subprocess.run(["dotnet", "restore", str(project)], capture_output=True, text=True)
                self.assertEqual(restored.returncode, 0, restored.stdout + restored.stderr)
                package_file = root / f"packages/nativecopyfixture/1.0.0/runtimes/{rid}/native/libHarfBuzzSharp.dll"
                output = root / "bin/Release/net10.0" / (rid if explicit_rid else "")
                selected = output / relative / "libHarfBuzzSharp.dll"
                command = ["dotnet", "build", str(project), "-c", "Release", "--no-restore", "--nologo"]
                for newer in ("package", "project"):
                    with self.subTest(newer=newer):
                        timestamp = package_file.stat().st_mtime + (-60 if newer == "package" else 60)
                        os.utime(source, (timestamp, timestamp))
                        project.write_text(plain)
                        selected.unlink(missing_ok=True)
                        baseline = subprocess.run(command, capture_output=True, text=True)
                        self.assertEqual(baseline.returncode, 0, baseline.stdout + baseline.stderr)
                        expected = selected.read_bytes()
                        project.write_text(plain.replace('</Project>',
                            f'<Import Project="{escape(str(package.ROOT / "tools/native/WindowsNative.targets"))}" /></Project>'))
                        for stale in (False, True):
                            selected.unlink(missing_ok=True)
                            if stale:
                                selected.write_bytes(b"x" * len(expected))
                                future = time.time() + 864000
                                os.utime(selected, (future, future))
                            result = subprocess.run(command, capture_output=True, text=True)
                            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                            self.assertEqual(selected.read_bytes(), expected)
                            self.assertEqual(self.assert_application_receipt(output),
                                {relative + "libHarfBuzzSharp.dll": hashlib.sha256(expected).hexdigest()})
                            launched = subprocess.run(["dotnet", str(output / "Consumer.dll"), relative + "libHarfBuzzSharp.dll",
                                expected.decode()], capture_output=True, text=True)
                            self.assertEqual(launched.returncode, 0, launched.stdout + launched.stderr)
                        print("SELECTION " + json.dumps(dict(case="project-package-collision", hostRID=rid,
                            explicitRID=explicit_rid, item=item, mode=mode, newer=newer,
                            SDKExpectedSHA256=hashlib.sha256(expected).hexdigest(), receiptEntries=1, cleanAndRepeatedRun=0)), flush=True)

    def test_transitive_content_matches_sdk(self):
        rid = sdk_host_rid()
        for mode in ("PreserveNewest", "Always", "IfDifferent"):
            with self.subTest(mode=mode), tempfile.TemporaryDirectory(prefix="native transitive ") as temporary:
                root = Path(temporary)
                app = root / "app"
                app.mkdir()
                child = root / "child"
                child.mkdir()
                project, relative = write_package_consumer(app, True, False, rid)
                source = app / "application.bin"
                source.write_bytes(b"application-owned native file")
                child_source = child / "child.bin"
                child_source.write_bytes(b"transitive-owned native file")
                (child / "Child.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework>'
                    '</PropertyGroup><ItemGroup><None Include="child.bin" Link="libHarfBuzzSharp.dll" '
                    f'CopyToOutputDirectory="{mode}" /></ItemGroup></Project>')
                plain = project.read_text().replace('</PropertyGroup>', '<CopyConflictingTransitiveContent>true</CopyConflictingTransitiveContent></PropertyGroup>')
                plain = plain.replace('</ItemGroup>', '<ProjectReference Include="../child/Child.csproj" />'
                    f'<Content Include="application.bin" Link="libHarfBuzzSharp.dll" CopyToOutputDirectory="{mode}" /></ItemGroup>')
                project.write_text(plain)
                restored = subprocess.run(["dotnet", "restore", str(project)], capture_output=True, text=True)
                self.assertEqual(restored.returncode, 0, restored.stdout + restored.stderr)
                output = app / f"bin/Release/net10.0/{rid}"
                selected = output / "libHarfBuzzSharp.dll"
                command = ["dotnet", "build", str(project), "-c", "Release", "--no-restore", "--nologo"]
                for newer in ("child", "project"):
                    with self.subTest(newer=newer):
                        timestamp = child_source.stat().st_mtime + (-60 if newer == "child" else 60)
                        os.utime(source, (timestamp, timestamp))
                        project.write_text(plain)
                        selected.unlink(missing_ok=True)
                        baseline = subprocess.run(command, capture_output=True, text=True)
                        self.assertEqual(baseline.returncode, 0, baseline.stdout + baseline.stderr)
                        expected = selected.read_bytes()
                        project.write_text(plain.replace('</Project>',
                            f'<Import Project="{escape(str(package.ROOT / "tools/native/WindowsNative.targets"))}" /></Project>'))
                        for stale in (False, True):
                            selected.unlink(missing_ok=True)
                            if stale:
                                selected.write_bytes(b"x" * len(expected))
                                future = time.time() + 864000
                                os.utime(selected, (future, future))
                            result = subprocess.run(command, capture_output=True, text=True)
                            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                            self.assertEqual(selected.read_bytes(), expected)
                            self.assertEqual(self.assert_application_receipt(output), {"libHarfBuzzSharp.dll": hashlib.sha256(expected).hexdigest()})
                            launched = subprocess.run(["dotnet", str(output / "Consumer.dll"), "libHarfBuzzSharp.dll", expected.decode()], capture_output=True, text=True)
                            self.assertEqual(launched.returncode, 0, launched.stdout + launched.stderr)
                        print("SELECTION " + json.dumps(dict(case="transitive-content", hostRID=rid,
                            mode=mode, newer=newer, SDKExpectedSHA256=hashlib.sha256(expected).hexdigest(), receiptEntries=1)), flush=True)

    def test_foreign_runtime_cleanup_preserves_selected_files(self):
        rid = sdk_host_rid()
        foreign = next(candidate for candidate in package.PLATFORMS["Windows"] if candidate != rid)
        with tempfile.TemporaryDirectory(prefix="native foreign RID ") as temporary:
            root = Path(temporary)
            project, relative = write_package_consumer(root, False, False, rid)
            source = root / "application.bin"
            source.write_bytes(b"application-owned foreign file")
            relative = f"runtimes/{foreign}/native/libHarfBuzzSharp.dll"
            project.write_text(project.read_text().replace('</ItemGroup>',
                f'<None Include="application.bin" Link="{relative}" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" /></ItemGroup>')
                .replace('</Project>', f'<Import Project="{escape(str(package.ROOT / "tools/native/WindowsNative.targets"))}" /></Project>'))
            for stage, output in (("build", root / "bin/Release/net10.0"), ("publish", root / "publish")):
                runtime_directories = [output / f"runtimes/{candidate}/native"
                                       for candidate in (*package.PLATFORMS["Windows"], "linux-x64")]
                for directory in runtime_directories:
                    directory.mkdir(parents=True, exist_ok=True)
                    (directory / "Electron2DWebRTC.dll").write_bytes(b"retired engine")
                    (directory / "application-plugin.dll").write_bytes(b"unrelated file")
                command = ["dotnet", stage, str(project), "-c", "Release", "--nologo"]
                if stage == "publish":
                    command += ["-o", str(output)]
                result = subprocess.run(command, capture_output=True, text=True)
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertEqual((output / relative).read_bytes(), source.read_bytes())
                for directory in runtime_directories:
                    self.assertFalse((directory / "Electron2DWebRTC.dll").exists())
                    self.assertEqual((directory / "application-plugin.dll").read_bytes(), b"unrelated file")
                entries = self.assert_application_receipt(output)
                self.assertEqual(entries[relative], hashlib.sha256(source.read_bytes()).hexdigest())
                print("SELECTION " + json.dumps(dict(case="foreign-runtime", hostRID=rid, foreignRID=foreign,
                    checkedRIDs=[*package.PLATFORMS["Windows"], "linux-x64"],
                    stage=stage, retired="removed", selected="preserved", unrelated="preserved")), flush=True)

    def test_project_copy_items_are_preserved(self):
        rid = sdk_host_rid()
        sdk = subprocess.check_output(["dotnet", "--version"], text=True).strip()
        sdk_path = subprocess.check_output(["dotnet", "msbuild", str(package.ROOT / "Electron2D.csproj"),
            "-nologo", "-getProperty:MSBuildSDKsPath"], text=True).strip()
        publish_targets = Path(sdk_path) / "Microsoft.NET.Sdk/targets/Microsoft.NET.Publish.targets"
        publish_if_different = "_CopyResolvedFilesToPublishIfDifferent" in publish_targets.read_text(encoding="utf-8")
        for item, relative, mode in product(("None", "Content"), ("", f"runtimes/{rid}/native/"),
                                           ("PreserveNewest", "Always", "IfDifferent")):
            with self.subTest(item=item, mode=mode, relative=relative), tempfile.TemporaryDirectory(prefix="project native ") as temporary:
                root = Path(temporary)
                source = root / "application-harfbuzz.bin"
                source.write_bytes(b"application-owned native file")
                checksum = hashlib.sha256(source.read_bytes()).hexdigest()
                project = root / "Consumer.csproj"
                project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
                    '<OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings>'
                    f'<TargetFramework>net10.0</TargetFramework><RuntimeIdentifier>{rid}</RuntimeIdentifier>'
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
                stages = [("build", root / f"bin/Release/net10.0/{rid}")]
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
                        f'<ReferenceCopyLocalPaths Include="{escape(str(root / "package" / name))}" DestinationSubDirectory="{relative}" />'
                        f'<ResolvedFileToPublish Include="{escape(str(root / "package" / name))}" RelativePath="{relative}{name}" />' for name in selected)
                    project = root / "Consumer.proj"
                    project.write_text('<Project><PropertyGroup><RuntimeIdentifier>win-x64</RuntimeIdentifier>'
                        f'<TargetDir>{escape(str(root / "build"))}/</TargetDir>'
                        f'<PublishDir>{escape(str(root / "publish"))}/</PublishDir></PropertyGroup>'
                        f'<ItemGroup>{assets}</ItemGroup>'
                        f'<Import Project="{escape(str(package.ROOT / "tools/native/WindowsNative.targets"))}" />'
                        '<Target Name="GetCopyToOutputDirectoryItems" /><Target Name="CopyFilesToOutputDirectory"><Copy SourceFiles="@(ReferenceCopyLocalPaths)" DestinationFiles="@(ReferenceCopyLocalPaths->&apos;$(TargetDir)%(DestinationSubDirectory)%(Filename)%(Extension)&apos;)" /></Target><Target Name="Publish"><Copy SourceFiles="@(ResolvedFileToPublish)" DestinationFiles="@(ResolvedFileToPublish->&apos;$(PublishDir)%(RelativePath)&apos;)" /></Target></Project>')
                    result = subprocess.run(["dotnet", "msbuild", str(project), "-nologo",
                        "-target:PrepareElectron2DLegacyWindowsBuild;CopyFilesToOutputDirectory;Electron2DWindowsNativePublishCopy;PrepareElectron2DLegacyWindowsPublish;Publish"], capture_output=True, text=True)
                    self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                    for output in ("build", "publish"):
                        files = {path.relative_to(root / output).as_posix(): path.read_bytes()
                                 for path in (root / output).rglob("*") if path.is_file()}
                        if "Electron2D.application-native.sha256" in files:
                            receipt = files.pop("Electron2D.application-native.sha256").decode("utf-8-sig")
                            self.assertEqual({(line.split()[0], line.split()[1].replace("\\", "/")) for line in receipt.splitlines()},
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
                    '<Target Name="GetCopyToOutputDirectoryItems" /><Target Name="CopyFilesToOutputDirectory"><Copy SourceFiles="@(ReferenceCopyLocalPaths)" DestinationFiles="@(ReferenceCopyLocalPaths->&apos;$(TargetDir)%(DestinationSubDirectory)%(Filename)%(Extension)&apos;)" /></Target><Target Name="Publish"><Copy SourceFiles="@(ResolvedFileToPublish)" DestinationFiles="@(ResolvedFileToPublish->&apos;$(PublishDir)%(RelativePath)&apos;)" /></Target></Project>')
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
                        "-target:PrepareElectron2DLegacyWindowsBuild;CopyFilesToOutputDirectory;Electron2DWindowsNativePublishCopy;PrepareElectron2DLegacyWindowsPublish;Publish"], capture_output=True, text=True)
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
