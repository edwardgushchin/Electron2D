#!/usr/bin/env python3
"""Exercise particle source/import/build profiles and atomic failure output."""
from pathlib import Path
import os
import subprocess
import tempfile
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[2]
TOOL = ROOT / 'tools/shaders/bin/Release/net10.0/Electron2D.ShaderImport.dll'

def run(arguments, success=True):
    result = subprocess.run(arguments, cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if (result.returncode == 0) != success:
        raise AssertionError(result.stdout)
    return result.stdout

with tempfile.TemporaryDirectory(prefix='electron2d-particle-import-') as temporary:
    directory = Path(temporary) / 'with spaces'
    directory.mkdir()
    for language, extension in [('GLSL', 'glsl'), ('HLSL', 'hlsl'), ('Minimal', 'glsl')]:
        source = ROOT / f'tests/Electron2D.Tests/Shaders/Particle{language}.comp.{extension}'
        output = directory / f'{language}.spv'
        run(['dotnet', str(TOOL), str(source), 'compute', str(output), '--particles'])
        expected = (source.parent / f'Particle{language}.comp.spv').read_bytes()
        assert output.read_bytes() == expected, language + ' fixture is stale'
        run(['dotnet', str(TOOL), str(output), 'compute', str(output), '--particles'])
        assert output.read_bytes() == expected, 'External SPIR-V import changed authored bytes'
        run(['dotnet', str(TOOL), str(source), 'fragment', str(output), '--particles'], success=False)
        assert output.read_bytes() == expected, 'Invalid profile/stage changed the destination'
    invalid = directory / 'invalid.comp.glsl'
    invalid.write_text('#version 450\nlayout(local_size_x=64) in; void main() {}\n')
    output = directory / 'GLSL.spv'
    before = output.read_bytes()
    run(['dotnet', str(TOOL), str(invalid), 'compute', str(output), '--particles'], success=False)
    assert output.read_bytes() == before, 'Missing state/frame ABI changed the destination'
    assert not list(directory.glob('*.tmp')), 'Failed import leaked adjacent output files'
    project = directory / 'Consumer.csproj'
    project.write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><TargetFramework>net10.0</TargetFramework><Electron2DShaderImportTool>{escape(str(TOOL.with_suffix('.exe') if os.name == 'nt' else TOOL.with_suffix('')))}</Electron2DShaderImportTool></PropertyGroup>
<ItemGroup><Electron2DShader Include="{escape(str(source))}" Stage="compute" Profile="particles" TargetPath="Shaders/process.spv" /></ItemGroup>
<Import Project="{escape(str(ROOT / 'tools/shaders/Electron2D.Shaders.targets'))}" />
</Project>''')
    run(['dotnet', 'build', str(project), '-c', 'Release', '--nologo'])
    assert (directory / 'bin/Release/net10.0/Shaders/process.spv').read_bytes() == expected
    project.write_text(project.read_text().replace('Stage="compute"', 'Stage="fragment"'))
    run(['dotnet', 'build', str(project), '-c', 'Release', '--nologo'], success=False)
print('Particle HLSL/GLSL source, external SPIR-V, atomic failure and fresh MSBuild profile checks passed')
