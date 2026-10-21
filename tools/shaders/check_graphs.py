#!/usr/bin/env python3
"""Exercise public graph authoring, atomic import and the real SDK build/publish/Clean path."""

import argparse
import hashlib
import html
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--tool", type=Path, required=True)
    parser.add_argument("--consumer", type=Path, required=True)
    parser.add_argument("--receipt", type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    tool = args.tool.resolve()
    consumer_path = args.consumer.resolve()
    consumer = (["dotnet", str(consumer_path)] if consumer_path.suffix == ".dll" else [str(consumer_path)])
    records = []

    def run(label, command, cwd=root, success=True):
        result = subprocess.run(command, cwd=cwd, capture_output=True, text=True, timeout=240)
        records.append({"name": label, "command": [str(s) for s in command], "exit": result.returncode,
                        "output": (result.stdout + result.stderr)[-12000:]})
        if args.receipt:
            args.receipt.parent.mkdir(parents=True, exist_ok=True)
            args.receipt.write_text(json.dumps(records, indent=2) + "\n")
        assert (result.returncode == 0) == success, label + "\n" + result.stdout + result.stderr
        return result.stdout + result.stderr

    with tempfile.TemporaryDirectory(prefix="electron2d graph build ") as folder:
        project = Path(folder)
        sources = project / "source graphs"
        run("author", consumer + ["author", str(sources)])
        valid = sources / "color.e2dres"
        target = project / "compiled.e2dres"
        run("direct-import", [str(tool), str(valid), "canvas", str(target), "--visual-shader"])
        run("fresh-public-load", consumer + ["read", str(target)])
        before = target.read_bytes()
        stamp = target.stat().st_mtime_ns
        run("unchanged-import", [str(tool), str(valid), "canvas", str(target), "--visual-shader"])
        assert target.read_bytes() == before and target.stat().st_mtime_ns == stamp
        run("cyclic-import", [str(tool), str(sources / "cycle.e2dres"), "canvas", str(target), "--visual-shader"], success=False)
        assert target.read_bytes() == before and target.stat().st_mtime_ns == stamp
        run("wrong-domain", [str(tool), str(valid), "compute", str(target), "--visual-shader"], success=False)
        assert target.read_bytes() == before and target.stat().st_mtime_ns == stamp
        output = run("missing-output-port", [str(tool), str(sources / "invalid/flow-missing-output.e2dres"), "canvas", str(target), "--visual-shader"], success=False)
        assert "Invalid stored visual shader connection" in output
        assert target.read_bytes() == before and target.stat().st_mtime_ns == stamp
        invalid = project / "truncated.e2dres"
        invalid.write_bytes(valid.read_bytes()[:63])
        run("truncated-import", [str(tool), str(invalid), "canvas", str(target), "--visual-shader"], success=False)
        assert target.read_bytes() == before and target.stat().st_mtime_ns == stamp

        source = project / "editable.e2dres"
        shutil.copy2(valid, source)
        project_file = project / "GraphBuild.csproj"
        project_file.write_text(f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable>
    <Electron2DShaderImportTool>{html.escape(str(tool))}</Electron2DShaderImportTool>
  </PropertyGroup>
  <ItemGroup><ProjectReference Include="{html.escape(str(root / 'Electron2D.csproj'))}" />
    <Electron2DShader Include="editable.e2dres" Stage="canvas" Profile="visual-shader" TargetPath="Graphs/actual graph.e2dres" />
  </ItemGroup>
  <Import Project="{html.escape(str(root / 'tools/native-consumer.props'))}" />
  <Import Project="{html.escape(str(root / 'tools/shaders/Electron2D.Shaders.targets'))}" />
</Project>
""")
        (project / "Program.cs").write_text("""using Electron2D;
using var graph = ResourceLoader.Load<VisualShader>(System.IO.Path.Combine(AppContext.BaseDirectory, "Graphs/actual graph.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
if (graph.GetMode() != Shader.Mode.CanvasItem || graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex).Length < 20) throw new Exception("Missing compiled stages.");
Console.WriteLine(((VisualShaderNodeColorConstant)graph.GetNode(VisualShader.Type.Fragment, 2)).Constant.R.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
""")
        command = ["dotnet", "build", str(project_file), "-c", "Release", "-r", "linux-x64"]
        run("sdk-build", command, cwd=project)
        artifact = next((project / "obj").rglob("actual graph.e2dres"))
        original = artifact.read_bytes()
        original_stamp = artifact.stat().st_mtime_ns
        executable = project / "bin/Release/net10.0/linux-x64/GraphBuild.dll"
        assert "0.25" in run("sdk-run", ["dotnet", str(executable)], cwd=project)
        run("sdk-unchanged", command + ["--no-restore"], cwd=project)
        assert artifact.read_bytes() == original and artifact.stat().st_mtime_ns == original_stamp
        source_stamp = source.stat().st_mtime_ns
        run("edit-source", consumer + ["edit", str(source), ".625"])
        os.utime(source, ns=(source.stat().st_atime_ns, source_stamp))
        run("sdk-same-timestamp-edit", command + ["--no-restore"], cwd=project)
        changed = artifact.read_bytes()
        changed_stamp = artifact.stat().st_mtime_ns
        assert changed != original
        assert "0.625" in run("sdk-edited-run", ["dotnet", str(executable)], cwd=project)
        valid_source = source.read_bytes()
        shutil.copyfile(sources / "cycle.e2dres", source)
        os.utime(source, ns=(source.stat().st_atime_ns, source_stamp))
        run("sdk-cyclic-source", command + ["--no-restore"], cwd=project, success=False)
        assert artifact.read_bytes() == changed and artifact.stat().st_mtime_ns == changed_stamp
        source.write_bytes(valid_source)
        run("sdk-recover", command + ["--no-restore"], cwd=project)
        publish = project / "published"
        run("sdk-publish-no-build", ["dotnet", "publish", str(project_file), "-c", "Release", "-r", "linux-x64", "--no-build", "-o", str(publish)], cwd=project)
        assert (publish / "Graphs/actual graph.e2dres").read_bytes() == changed
        assert "0.625" in run("published-run", ["dotnet", str(publish / "GraphBuild.dll")], cwd=project)
        run("sdk-clean", ["dotnet", "clean", str(project_file), "-c", "Release", "-r", "linux-x64"], cwd=project)
        assert not artifact.exists(), "Clean retained the generated graph resource."
        records.append({"name": "artifact-identity", "sha256": hashlib.sha256(changed).hexdigest(), "unchanged_timestamp": True,
                        "same_timestamp_source_edit": True, "atomic_failure": True, "publish_and_clean": True})
        if args.receipt:
            args.receipt.write_text(json.dumps(records, indent=2) + "\n")
    print("Visual graph direct import and SDK author/edit/error/recovery/publish/Clean checks passed.")


if __name__ == "__main__":
    main()
