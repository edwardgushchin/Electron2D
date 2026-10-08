"""Exercise fresh project and NuGet consumers while every native compiler is blocked."""

import os
import hashlib
import json
from zipfile import ZipFile
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

import package as native_package

ROOT = Path(__file__).resolve().parents[2]
PROGRAM = '''using Electron2D;

foreach (var name in new[] { "Electron2D.Shaders.Canvas.vert.spv", "Electron2D.Shaders.CanvasInstanced.vert.spv",
    "Electron2D.Shaders.Canvas.frag.spv", "Electron2D.Shaders.Clip.frag.spv", "Electron2D.PhysicsShaders.PhysicsIntegrate.comp.spv" })
{
    using var shader = typeof(Engine).Assembly.GetManifestResourceStream(name) ?? throw new Exception("Missing shader " + name);
    using var reader = new BinaryReader(shader);
    if (reader.ReadUInt32() != 0x07230203) throw new Exception("Invalid SPIR-V " + name);
}
using var stream = new AudioStreamWAV { Data = new byte[48000 * 2], MixRate = 48000 };
var root = new Node();
var player = new AudioStreamPlayer { Stream = stream };
root.AddChild(player);
using var tree = new SceneTree(root);
Engine.Start(tree);
try
{
    var font = ThemeDB.FallbackFont ?? throw new Exception("Missing default font");
    if (font.GetMultilineStringSize("ภาษาไทยภาษาไทย", width: 32).Y <= 0)
        throw new Exception("Native text layout failed");
    using var host = new ENetConnection();
    host.CreateHostBound("127.0.0.1", 0);
    if (host.GetLocalPort() <= 0) throw new Exception("Native ENet failed");
    player.Play();
    Thread.Sleep(100);
    if (player.GetPlaybackPosition() <= 0 || AudioServer.GetOutputLatency() < 0)
        throw new Exception("Native audio/output bridge failed");
    Console.WriteLine("Public text, audio and ENet consumer passed");
}
finally { Engine.Stop(); }
'''


def run(args, cwd, environment):
    subprocess.run(args, cwd=cwd, env=environment, check=True)


def read_manifests(feed):
    manifests = {}
    for package in feed.glob("Electron2D.*.nupkg"):
        if not any(package.name.startswith("Electron2D." + name + ".") for name in native_package.PLATFORMS):
            continue
        with ZipFile(package) as archive:
            manifest = json.loads(archive.read("native-manifest.json"))
            expected = {f"runtimes/{target}/native/{name}" for target, receipt in manifest.items() for name in receipt["files"]}
            actual = {name for name in archive.namelist() if name.startswith("runtimes/")}
            if actual != expected or any(name.startswith("lib/") or name.endswith(".dll") and name not in expected
                                         for name in archive.namelist()):
                raise RuntimeError("Native package contains wrong asset paths or a managed assembly")
            for target, receipt in manifest.items():
                if target in manifests:
                    raise RuntimeError("Native feed contains duplicate RID payloads")
                manifests[target] = receipt
                for name, digest in receipt["files"].items():
                    if hashlib.sha256(archive.read(f"runtimes/{target}/native/{name}")).hexdigest() != digest:
                        raise RuntimeError("Native package manifest does not match its binaries")
    return manifests


def check(feed, rid):
    manifests = read_manifests(feed)
    if rid not in manifests:
        raise RuntimeError("The feed does not contain the current native consumer RID")
    version = native_package.configuration()["version"]
    platform = native_package.platform(rid)
    with ZipFile(feed / f"Electron2D.{platform}.{version}.nupkg") as archive:
        spec = ET.fromstring(archive.read(f"Electron2D.{platform}.nuspec"))
        minimum = [element.attrib["version"] for element in spec.iter()
                   if element.tag.endswith("dependency") and element.attrib.get("id") == "Electron2D"]
        if minimum != [version] or manifests[rid].get("minimumEngineVersion") != version:
            raise RuntimeError("Platform package must declare its minimum engine version")
    dotnet = shutil.which("dotnet")
    with tempfile.TemporaryDirectory(prefix="electron2d-native-consumer-") as directory:
        work = Path(directory)
        engine = work / "engine"
        shutil.copytree(ROOT, engine, ignore=shutil.ignore_patterns(".git", "bin", "obj", "__pycache__", ".dev-diary"))
        for domain in ('Rendering', 'Physics'):
            for binary in (engine / f'src/Servers/{domain}/Shaders').glob('*.spv'):
                binary.unlink()
        # Source builds bake shaders with prepared host tools; game packages need neither tools nor sources.
        run([sys.executable, '-B', str(ROOT / 'tools/shaders/build_toolchain.py')], ROOT, os.environ)
        shutil.copytree(ROOT / 'tools/shaders/obj/toolchain', engine / 'tools/shaders/obj/toolchain')
        blocked = work / "blocked"
        blocked.mkdir()
        for name in ("cmake", "ninja", "cc", "c++", "gcc", "g++", "clang", "clang++", "cl", "link", "lib", "nmake"):
            command = blocked / (name + ".cmd" if os.name == "nt" else name)
            command.write_text('@echo Native tool invoked during consumer build 1>&2\n@exit /b 97\n' if os.name == "nt" else
                               '#!/bin/sh\necho "Native tool invoked during consumer build" >&2\nexit 97\n')
            if os.name != "nt":
                command.chmod(0o755)
        environment = dict(os.environ, PATH=str(blocked) + os.pathsep + os.environ["PATH"],
                           RestoreAdditionalProjectSources=str(feed), NUGET_PACKAGES=str(work / "packages"), SDL_AUDIODRIVER="dummy")
        environment.pop("LD_LIBRARY_PATH", None)
        environment.pop("Electron2DBuildNativeFromSource", None)
        run([dotnet, "build", "Electron2D.csproj", "-c", "Release", "--nologo"], engine, environment)
        if any((engine / "obj" / name).exists() for name in ("text-native", "audio-native", "enet-native", "tls-native", "font-native", "native-windows", "native-cross")):
            raise RuntimeError("Consumer build created native compilation directories")
        # A package consumer has no reference to the engine source or its private build targets.
        engine_feed = work / "feed"
        engine_feed.mkdir()
        for package in feed.glob("*.nupkg"):
            shutil.copy2(package, engine_feed)
        run([dotnet, "pack", "Electron2D.csproj", "-c", "Release", "--no-build",
             "-p:PackageVersion=" + version, "-o", str(engine_feed)], engine, environment)
        # A managed package contains neither native payloads nor implicit platform dependencies.
        with ZipFile(engine_feed / f"Electron2D.{version}.nupkg") as archive:
            if any(name.startswith("runtimes/") for name in archive.namelist()):
                raise RuntimeError("Managed engine package contains native files")
            if any(name.startswith('tools/') or Path(name).suffix in ('.py', '.glsl', '.hlsl', '.spv')
                   for name in archive.namelist()):
                raise RuntimeError("Managed engine package leaked shader build inputs/tools")
            nuspec = ET.fromstring(archive.read("Electron2D.nuspec"))
            if any(element.tag.endswith("dependency") for element in nuspec.iter()):
                raise RuntimeError("Managed engine package selects native dependencies")
        environment["RestoreAdditionalProjectSources"] = str(engine_feed)
        # NuGet must refuse an explicitly pinned older managed engine.
        older = "0.0.0"
        run([dotnet, "pack", "Electron2D.csproj", "-c", "Release", "--no-build",
             "-p:PackageVersion=" + older, "-o", str(engine_feed)], engine, environment)
        downgrade = work / "downgrade"
        downgrade.mkdir()
        (downgrade / "Consumer.csproj").write_text(
            '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework>'
            '</PropertyGroup><ItemGroup>'
            f'<PackageReference Include="Electron2D" Version="[{older}]" />'
            f'<PackageReference Include="Electron2D.{platform}" Version="[{version}]" />'
            '</ItemGroup></Project>', encoding="utf-8")
        rejected = subprocess.run([dotnet, "restore", "Consumer.csproj", "-p:WarningsAsErrors=NU1605"],
                                  cwd=downgrade, env=environment, text=True, capture_output=True)
        if rejected.returncode == 0 or not any(code in rejected.stdout + rejected.stderr for code in ("NU1605", "NU1107")):
            raise RuntimeError("Expected a NuGet version conflict: " + (rejected.stdout + rejected.stderr)[-2000:])
        print("Older engine rejected by NuGet version constraints")
        host_rid = subprocess.check_output([dotnet, "msbuild", "Electron2D.csproj", "-nologo",
                                           "-getProperty:NETCoreSdkRuntimeIdentifier"],
                                          cwd=engine, env=environment, text=True).strip()
        # A default desktop build targets its SDK host, independently of the explicit CI RID.
        consumers = ("project", "package", "generic") if rid == host_rid else ("project", "package")
        for kind in consumers:
            if kind != 'project':
                for name in ('python', 'python3', 'glslangValidator', 'spirv-val'):
                    command = blocked / (name + '.cmd' if os.name == 'nt' else name)
                    command.write_text('@exit /b 97\n' if os.name == 'nt' else '#!/bin/sh\nexit 97\n')
                    if os.name != 'nt':
                        command.chmod(0o755)
            app = work / kind
            app.mkdir()
            reference = '<ProjectReference Include="../engine/Electron2D.csproj" />' if kind == "project" else (
                f'<PackageReference Include="Electron2D" Version="[{version}]" />')
            target = f'<RuntimeIdentifier>{rid}</RuntimeIdentifier>' if kind != "generic" else ''
            (app / "Consumer.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
                '<OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework>'
                + target +
                '<ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup>' + reference +
                f'<PackageReference Include="Electron2D.{platform}" Version="[{version}]" />' + '</ItemGroup></Project>', encoding="utf-8")
            (app / "Program.cs").write_text(PROGRAM, encoding="utf-8")
            environment["RestoreAdditionalProjectSources"] = str(engine_feed)
            if os.name != "nt":
                run([dotnet, "run", "-c", "Release", "--project", "Consumer.csproj"], app, environment)
            if kind == "generic":
                run([dotnet, "build", "Consumer.csproj", "-c", "Release", "--nologo"], app, environment)
                selected = app / "bin/Release/net10.0/runtimes"
                if {path.name for path in selected.iterdir()} != {rid}:
                    raise RuntimeError("A desktop build without an explicit RID copied foreign native assets")
            output = work / (kind + "-publish")
            publish = [dotnet, "publish", "Consumer.csproj", "-c", "Release", "-o", str(output), "--nologo"]
            if kind != "generic":
                publish += ["-r", rid, "--self-contained", "true"]
            run(publish, app, environment)
            for name in manifests[rid]["files"]:
                if (output / name).exists() or not (output / "runtimes" / rid / "native" / name).is_file():
                    raise RuntimeError(f"Private native directory lost: {kind}/{name}")
            run([str(output / ("Consumer.exe" if os.name == "nt" else "Consumer"))], work, environment)
            foreign = [path for path in (output / "runtimes").glob("*") if path.name != rid]
            if foreign:
                raise RuntimeError(f"Foreign native runtime directories shipped: {foreign}")
        print("Managed-only source/package, explicit platform project/NuGet consumers and target-only builds/publishes passed with native tools blocked (explicit RID and default host)")


if __name__ == "__main__":
    check(Path(sys.argv[1]).resolve(), sys.argv[2])
