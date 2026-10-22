#!/usr/bin/env python3
"""Exercise domain selection, atomic failures and automatic SDK graph profiles."""
import argparse
import hashlib
import html
import json
from pathlib import Path
import subprocess
import tempfile


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--tool', type=Path, required=True)
    parser.add_argument('--consumer', type=Path, required=True)
    parser.add_argument('--receipt', type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    tool, consumer = args.tool.resolve(), args.consumer.resolve()
    rows = []

    def run(name, command, success=True):
        result = subprocess.run(command, cwd=root, text=True, capture_output=True, timeout=240)
        rows.append(dict(name=name, command=[str(v) for v in command], exit=result.returncode,
                         output=(result.stdout + result.stderr)[-6000:]))
        if args.receipt:
            args.receipt.parent.mkdir(parents=True, exist_ok=True)
            args.receipt.write_text(json.dumps(rows, indent=2) + '\n')
        assert (result.returncode == 0) == success, name + '\n' + result.stdout + result.stderr

    with tempfile.TemporaryDirectory(prefix='electron2d graph domains ') as folder:
        folder = Path(folder)
        source, target = folder / 'source', folder / 'compiled.e2dres'
        run('author', ['dotnet', str(consumer), 'domains-author', str(source)])
        for name, mode in [('particle-basic', 'particles'), ('blit-4', 'texture-blit')]:
            item = source / (name + '.e2dres')
            run('direct-' + mode, [str(tool), str(item), mode, str(target), '--visual-shader'])
            before, stamp = target.read_bytes(), target.stat().st_mtime_ns
            run('auto-' + mode, [str(tool), str(item), 'graph', str(target), '--visual-shader'])
            assert target.read_bytes() == before and target.stat().st_mtime_ns == stamp
            run('mismatch-' + mode, [str(tool), str(item), 'canvas', str(target), '--visual-shader'], False)
            assert target.read_bytes() == before and target.stat().st_mtime_ns == stamp
        for item in sorted((source / 'invalid-domains').glob('*.e2dres')):
            run(item.stem, [str(tool), str(item), 'graph', str(target), '--visual-shader'], False)
            assert target.read_bytes() == before and target.stat().st_mtime_ns == stamp
        assert not list(folder.rglob('*.tmp')), 'Failed imports leaked adjacent artifacts'
        (folder / 'empty.cs').write_text('internal static class Empty {}\n')
        project = folder / 'DomainBuild.csproj'
        project.write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><TargetFramework>net10.0</TargetFramework><Electron2DShaderImportTool>{html.escape(str(tool))}</Electron2DShaderImportTool></PropertyGroup>
<ItemGroup><Electron2DShader Include="source/particle-basic.e2dres" Profile="visual-shader" TargetPath="Graphs/particle.e2dres" />
<Electron2DShader Include="source/blit-4.e2dres" Profile="visual-shader" TargetPath="Graphs/blit.e2dres" /></ItemGroup>
<Import Project="{html.escape(str(root / 'tools/shaders/Electron2D.Shaders.targets'))}" /></Project>''')
        run('sdk-auto', ['dotnet', 'build', str(project), '-c', 'Release'])
        artifacts = [(folder / 'bin/Release/net10.0/Graphs' / (name + '.e2dres')) for name in ['particle', 'blit']]
        digests = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in artifacts}
        publish = folder / 'published'
        run('sdk-publish', ['dotnet', 'publish', str(project), '-c', 'Release', '--no-build', '-o', str(publish)])
        assert all(hashlib.sha256((publish / 'Graphs' / name).read_bytes()).hexdigest() == digest for name, digest in digests.items())
        run('sdk-clean', ['dotnet', 'clean', str(project), '-c', 'Release'])
        assert not any(p.exists() for p in artifacts), 'Clean retained output domain graphs'
    print(f'{len(rows)} domain import/SDK checks passed')


if __name__ == '__main__':
    main()
