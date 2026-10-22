#!/usr/bin/env python3
"""Exercise compiled custom providers through shared import and SDK build/publish/Clean."""
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
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--host', type=Path, required=True)
    parser.add_argument('--stock-tool', type=Path, required=True)
    parser.add_argument('--consumer', type=Path, required=True)
    parser.add_argument('--receipt', type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    host, stock, consumer = (p.resolve() for p in (args.host, args.stock_tool, args.consumer))
    def executable(path):
        return ['dotnet', str(path)] if path.suffix == '.dll' else [str(path)]
    records = []
    def run(label, command, cwd=root, success=True):
        result = subprocess.run(command, cwd=cwd, capture_output=True, text=True, timeout=240)
        diagnostic = result.stdout + result.stderr
        records.append(dict(name=label, command=[str(s) for s in command], exit=result.returncode, output=diagnostic[-12000:]))
        if args.receipt:
            args.receipt.write_text(json.dumps(records, indent=2) + '\n')
        assert (result.returncode == 0) == success, label + '\n' + diagnostic
        print(label + ': passed', flush=True)
        return diagnostic
    with tempfile.TemporaryDirectory(prefix='electron2d custom host ') as folder:
        project = Path(folder); sources = project / 'authored'
        run('author', executable(consumer) + ['custom-author', str(sources)])
        run('entry-guards', executable(host) + ['--entry-check'])
        source = sources / 'custom-numeric-5-1-1.e2dres'; target = project / 'compiled.e2dres'
        command = [str(source), 'canvas', str(target), '--visual-shader']
        trace = run('direct', executable(host) + command)
        assert trace.count('numeric:Fragment') == 1, trace
        run('fresh-public-load', executable(consumer) + ['read', str(target)])
        before = target.read_bytes(); stamp = target.stat().st_mtime_ns
        for label, prefix in [('unchanged', []), ('reentry', ['--reentry-check']), ('concurrent', ['--concurrency-check'])]:
            run(label, executable(host) + prefix + command)
            assert target.read_bytes() == before and target.stat().st_mtime_ns == stamp, label
        run('missing-factory', executable(stock) + command, success=False)
        assert target.read_bytes() == before and target.stat().st_mtime_ns == stamp
        for kind in [100, 101, 102, 103, 104, 106, 107, 109, 110]:
            invalid = sources / ('invalid-custom/custom-procedural-' + str(kind) + '-0.e2dres')
            run('invalid-' + str(kind), executable(host) + [str(invalid), 'canvas', str(target), '--visual-shader'], success=False)
            assert target.read_bytes() == before and target.stat().st_mtime_ns == stamp, kind
        if host.suffix == '.dll':
            sdk_tool = host.with_suffix('')
        else:
            sdk_tool = host
        assert sdk_tool.is_file(), 'The SDK requires the compiled host apphost executable.'
        editable = project / 'editable.e2dres'; shutil.copyfile(sources / 'custom-procedural-12-1.e2dres', editable)
        project_file = project / 'CustomBuild.csproj'
        project_file.write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable>
    <Electron2DShaderImportTool>{html.escape(str(sdk_tool))}</Electron2DShaderImportTool>
  </PropertyGroup>
  <ItemGroup><ProjectReference Include="{html.escape(str(root / 'Electron2D.csproj'))}" />
    <Compile Include="{html.escape(str(root / 'tests/VisualShader.Custom/CustomNodes.cs'))}" Link="CustomNodes.cs" />
    <Electron2DShader Include="editable.e2dres" Stage="canvas" Profile="visual-shader" TargetPath="Graphs/custom graph.e2dres" />
  </ItemGroup>
  <Import Project="{html.escape(str(root / 'tools/native-consumer.props'))}" />
  <Import Project="{html.escape(str(root / 'tools/shaders/Electron2D.Shaders.targets'))}" />
</Project>''')
        (project / 'Program.cs').write_text('''using Electron2D;
CustomNodes.Register();
using var graph = ResourceLoader.Load<VisualShader>(System.IO.Path.Combine(AppContext.BaseDirectory,"Graphs/custom graph.e2dres"),ResourceLoader.CacheMode.IgnoreDeep);
var node=(CustomProcedureNode)graph.GetNode(VisualShader.Type.Fragment,2);
if(node.GetOptionIndex(0)!=0 || node.GetInputPortDefaultValue<bool>(0)!=true || graph.GetSPIRV(RenderingDevice.ShaderStage.Fragment).Length<20) throw new Exception("Compiled custom state changed.");
Console.WriteLine("Typed custom graph loaded.");
''')
        build = ['dotnet', 'build', str(project_file), '-c', 'Release', '-r', 'linux-x64']
        run('sdk-build', build, cwd=project)
        artifact = next((project / 'obj').rglob('custom graph.e2dres'))
        original = artifact.read_bytes(); original_stamp = artifact.stat().st_mtime_ns
        run('sdk-run', ['dotnet', str(project / 'bin/Release/net10.0/linux-x64/CustomBuild.dll')], cwd=project)
        run('sdk-unchanged', build + ['--no-restore'], cwd=project)
        assert artifact.read_bytes() == original and artifact.stat().st_mtime_ns == original_stamp
        saved_source = editable.read_bytes(); source_stamp = editable.stat().st_mtime_ns
        shutil.copyfile(sources / 'invalid-custom/custom-procedural-105-0.e2dres', editable)
        os.utime(editable, ns=(editable.stat().st_atime_ns, source_stamp))
        run('sdk-callback-deadline', build + ['--no-restore', '-p:Electron2DShaderImportTimeoutMS=1500'], cwd=project, success=False)
        assert artifact.read_bytes() == original and artifact.stat().st_mtime_ns == original_stamp
        assert not list(artifact.parent.glob('.*.import-*.e2dres')), 'Staged custom artifact leaked.'
        editable.write_bytes(saved_source)
        run('sdk-recovery', build + ['--no-restore'], cwd=project)
        publish = project / 'published'
        run('sdk-publish', ['dotnet', 'publish', str(project_file), '-c', 'Release', '-r', 'linux-x64', '--no-build', '-o', str(publish)], cwd=project)
        assert (publish / 'Graphs/custom graph.e2dres').read_bytes() == original
        run('published-run', ['dotnet', str(publish / 'CustomBuild.dll')], cwd=project)
        run('sdk-clean', ['dotnet', 'clean', str(project_file), '-c', 'Release', '-r', 'linux-x64'], cwd=project)
        assert not artifact.exists(), 'Clean retained the custom graph artifact.'
        records.append(dict(name='artifact-identity', sha256=hashlib.sha256(original).hexdigest(), unchanged_timestamp=True, callback_deadline=True, publish_and_clean=True))
        if args.receipt:
            args.receipt.write_text(json.dumps(records, indent=2) + '\n')
    print('Compiled custom import, guards, diagnostics, atomic failures and SDK publication passed.')


if __name__ == '__main__':
    main()
