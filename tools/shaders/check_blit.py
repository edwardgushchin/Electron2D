#!/usr/bin/env python3
"""Check reproducible blit imports, diagnostics and the project build profile."""
from pathlib import Path
import subprocess
import tempfile
from xml.sax.saxutils import escape

root = Path(__file__).resolve().parents[2]
tool = root / 'tools/shaders/bin/Release/net10.0/Electron2D.ShaderImport'
subprocess.run(['dotnet', 'build', str(root / 'tools/shaders/ShaderImport.csproj'), '-c', 'Release'], check=True)
with tempfile.TemporaryDirectory(prefix='electron2d-blit-import-') as scratch:
    directory = Path(scratch)
    output = directory / 'program.spv'
    items = []
    for language, suffix in [('GLSL', 'glsl'), ('HLSL', 'hlsl')]:
        source = root / f'tests/Electron2D.Tests/Shaders/Drawable{language}.frag.{suffix}'
        command = [str(tool), str(source), 'fragment', str(output)]
        subprocess.run([*command, '--texture-blit'], check=True)
        expected = (source.parent / f'Drawable{language}.spv').read_bytes()
        assert output.read_bytes() == expected
        stamp = output.stat().st_mtime_ns
        subprocess.run([*command, '--texture-blit'], check=True)
        assert output.stat().st_mtime_ns == stamp
        failure = subprocess.run(command, capture_output=True, text=True)
        assert failure.returncode != 0 and 'one fragment color output' in failure.stderr
        assert output.read_bytes() == expected
        items.append(f'<Electron2DShader Include="{escape(str(source))}" Stage="fragment" Profile="texture-blit" TargetPath="{language}.spv" />')
    project = directory / 'Blit.csproj'
    project.write_text(f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><ProjectReference Include="{escape(str(root / 'Electron2D.csproj'))}" />{''.join(items)}</ItemGroup><Import Project="{escape(str(root / 'tools/native-consumer.props'))}" /><Import Project="{escape(str(root / 'tools/shaders/Electron2D.Shaders.targets'))}" /></Project>''')
    (directory / 'Program.cs').write_text('''using Electron2D;
foreach (var file in new[] { "GLSL.spv", "HLSL.spv" }) {
    using var shader = Shader.CreateFromSPIRV(File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, file)), Shader.Mode.TextureBlit);
    if (shader.GetMode() != Shader.Mode.TextureBlit || shader.GetShaderUniformList().Count != 2) throw new Exception("Invalid built blit program.");
}
Console.WriteLine("Fresh public blit project passed.");
''')
    subprocess.run(['dotnet', 'run', '--project', str(project), '-c', 'Release', '-p:Electron2DShaderImportTool=' + str(tool)], cwd=directory, check=True)
print('Blit import and build profile checks passed.')
