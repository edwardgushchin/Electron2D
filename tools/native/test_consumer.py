"""Exercise fresh project and NuGet consumers while every native compiler is blocked."""

import os
import platform
import hashlib
import json
import uuid
from zipfile import ZipFile
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]
PROGRAM = '''using Electron2D;

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


def check(feed):
    rid = ("osx-arm64" if platform.machine() == "arm64" else "osx-x64") if platform.system() == "Darwin" else "linux-x64"
    manifests = {}
    for package in feed.glob("Electron2D.Native.*.nupkg"):
        with ZipFile(package) as archive:
            manifest = json.loads(archive.read("native-manifest.json"))
            expected = {f"runtimes/{target}/native/{name}" for target, receipt in manifest.items() for name in receipt["files"]}
            actual = {name for name in archive.namelist() if name.startswith("runtimes/")}
            if actual != expected or any(name.endswith(".dll") for name in archive.namelist()):
                raise RuntimeError("Native package contains wrong asset paths or a managed assembly")
            for target, receipt in manifest.items():
                if target in manifests:
                    raise RuntimeError("Native feed contains duplicate RID payloads")
                manifests[target] = receipt
                for name, digest in receipt["files"].items():
                    if hashlib.sha256(archive.read(f"runtimes/{target}/native/{name}")).hexdigest() != digest:
                        raise RuntimeError("Native package manifest does not match its binaries")
    if rid not in manifests:
        raise RuntimeError("The feed does not contain the current native consumer RID")
    version = "0.0.0-native-consumer-" + uuid.uuid4().hex[:8]
    dotnet = shutil.which("dotnet")
    with tempfile.TemporaryDirectory(prefix="electron2d-native-consumer-") as directory:
        work = Path(directory)
        engine = work / "engine"
        shutil.copytree(ROOT, engine, ignore=shutil.ignore_patterns(".git", "bin", "obj", "__pycache__", ".dev-diary"))
        blocked = work / "blocked"
        blocked.mkdir()
        for name in ("cmake", "ninja", "cc", "c++", "gcc", "g++", "clang", "clang++"):
            command = blocked / name
            command.write_text('#!/bin/sh\necho "Native tool invoked during consumer build" >&2\nexit 97\n')
            command.chmod(0o755)
        environment = dict(os.environ, PATH=str(blocked) + os.pathsep + os.environ["PATH"],
                           RestoreAdditionalProjectSources=str(feed), SDL_AUDIODRIVER="dummy")
        environment.pop("LD_LIBRARY_PATH", None)
        environment.pop("Electron2DBuildNativeFromSource", None)
        run([dotnet, "build", "Electron2D.csproj", "-c", "Release", "--nologo"], engine, environment)
        if any((engine / "obj" / name).exists() for name in ("text-native", "audio-native", "enet-native", "tls-native", "font-native")):
            raise RuntimeError("Consumer build created native compilation directories")
        # A package consumer has no reference to the engine source or its private build targets.
        engine_feed = work / "feed"
        engine_feed.mkdir()
        for package in feed.glob("*.nupkg"):
            shutil.copy2(package, engine_feed)
        run([dotnet, "pack", "Electron2D.csproj", "-c", "Release", "--no-build",
             "-p:PackageVersion=" + version, "-o", str(engine_feed)], engine, environment)
        for kind in ("project", "package"):
            app = work / kind
            app.mkdir()
            reference = '<ProjectReference Include="../engine/Electron2D.csproj" />' if kind == "project" else (
                f'<PackageReference Include="Electron2D" Version="[{version}]" />')
            (app / "Consumer.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
                '<OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework>'
                '<ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup>' + reference + '</ItemGroup></Project>')
            (app / "Program.cs").write_text(PROGRAM)
            environment["RestoreAdditionalProjectSources"] = str(engine_feed)
            run([dotnet, "run", "-c", "Release", "--project", "Consumer.csproj"], app, environment)
            output = work / (kind + "-publish")
            run([dotnet, "publish", "Consumer.csproj", "-c", "Release", "-r", rid,
                 "--self-contained", "true", "-o", str(output), "--nologo"], app, environment)
            for name in manifests[rid]["files"]:
                if (output / name).exists() or not (output / "runtimes" / rid / "native" / name).is_file():
                    raise RuntimeError(f"Private native directory lost: {kind}/{name}")
            run([str(output / "Consumer")], work, environment)
        print("Fresh source build, project/NuGet consumers and self-contained publishes passed with native tools blocked")


if __name__ == "__main__":
    check(Path(sys.argv[1]).resolve())
