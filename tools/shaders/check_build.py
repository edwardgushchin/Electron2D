#!/usr/bin/env python3
"""Verify the published shader importer in an ordinary public-API consumer project."""
from pathlib import Path
import argparse
import os
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as XML

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--tool', type=Path, required=True, help='Published Electron2D.ShaderImport executable.')
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]
tool = args.tool.resolve()
targets = tool.parent / 'Electron2D.Shaders.targets'
assert targets.is_file(), f'Missing packaged build integration: {targets}'


def escape(value):
    for character in "%$@;'()*?":
        value = value.replace(character, f'%{ord(character):02X}')
    return value


with tempfile.TemporaryDirectory(prefix="electron2d shader build '&-") as temporary:
    directory = Path(temporary)
    shaders = directory / 'Shaders'
    (shaders / 'nested').mkdir(parents=True)
    factor = shaders / 'factor.inc'
    factor.write_text('#define FACTOR 0.5\n')
    (shaders / 'nested/entry.inc').write_text('#ifndef INCLUDED\n#define INCLUDED\n#include "../factor.inc"\n#endif\n')
    hlsl = '#include "nested/entry.inc"\nfloat4 main(float4 color : TEXCOORD0) : SV_Target0 { return color * FACTOR; }\n'
    (shaders / 'Surface.hlsl').write_text(hlsl)
    (shaders / 'Surface.glsl').write_text('#version 450\n#extension GL_GOOGLE_include_directive : require\n'
                                        '#include "nested/entry.inc"\nlayout(location=0) in vec4 color;\n'
                                        'layout(location=0) out vec4 result;\nvoid main() { result = color * FACTOR; }\n')
    # A shell-based build command would try to execute this filename's substitutions.
    special = 'name $(touch INJECTED) `touch INJECTED` & "quoted".hlsl'
    (shaders / special).write_text(hlsl)
    external = shaders / 'external.spv'
    shutil.copyfile(root / 'tests/Electron2D.Tests/Shaders/CanvasHLSL.spv', external)
    project = XML.Element('Project', Sdk='Microsoft.NET.Sdk')
    properties = XML.SubElement(project, 'PropertyGroup')
    for key, value in [('OutputType', 'Exe'), ('TargetFramework', 'net8.0'), ('ImplicitUsings', 'enable'),
                       ('TreatWarningsAsErrors', 'true')]:
        XML.SubElement(properties, key).text = value
    items = XML.SubElement(project, 'ItemGroup')
    XML.SubElement(items, 'ProjectReference', Include=escape(str(root / 'Electron2D.csproj')))
    for source in ['Surface.hlsl', 'Surface.glsl', special, 'external.spv']:
        XML.SubElement(items, 'Electron2DShader', Include=escape('Shaders/' + source), Stage='fragment')
    XML.SubElement(project, 'Import', Project=escape(str(targets)))
    project_file = directory / 'Consumer.csproj'

    def save_project():
        XML.ElementTree(project).write(project_file, encoding='unicode')

    save_project()
    (directory / 'Program.cs').write_text('''using Electron2D;
var files = Directory.GetFiles(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders"), "*.spv");
if (files.Length != 4) throw new Exception("Missing imported shaders");
foreach (var file in files) {
    using var shader = Shader.CreateFromSPIRV(File.ReadAllBytes(file));
    if (shader.GetSPIRV().Length == 0) throw new Exception("Empty shader");
}
Console.WriteLine("Public shader consumer passed");
''')

    def run(*arguments, success=True):
        result = subprocess.run(['dotnet', *arguments, str(project_file), '-c', 'Release', '--nologo', '-v:normal'],
                                cwd=directory, text=True, capture_output=True, timeout=180)
        diagnostic = result.stdout + result.stderr
        assert (result.returncode == 0) == success, diagnostic
        return diagnostic

    run('build')
    generated = directory / 'obj/Release/net8.0/Electron2D/shaders/Shaders'
    artifacts = sorted(generated.glob('*.spv'))
    assert len(artifacts) == 4
    previous = {path.name: (path.read_bytes(), path.stat().st_mtime_ns) for path in artifacts}
    diagnostic = run('build', '--no-restore')
    assert diagnostic.count('Unchanged ') == 4, diagnostic
    assert previous == {path.name: (path.read_bytes(), path.stat().st_mtime_ns) for path in artifacts}
    assert not (directory / 'INJECTED').exists(), 'Build arguments were interpreted by a shell'
    print('Build: both languages, external bytecode, quoted paths, stable unchanged output.', flush=True)

    stamp = factor.stat()
    factor.write_text('#define FACTOR 0.2\n')
    os.utime(factor, ns=(stamp.st_atime_ns, stamp.st_mtime_ns))
    run('build', '--no-restore')
    for path in artifacts:
        assert (path.read_bytes() != previous[path.name][0]) == (path.name != 'external.spv.spv')
    previous = {path.name: (path.read_bytes(), path.stat().st_mtime_ns) for path in artifacts}
    damaged = artifacts[0]
    damaged.write_bytes(bytes(len(previous[damaged.name][0])))
    os.utime(damaged, ns=(previous[damaged.name][1], previous[damaged.name][1]))
    run('build', '--no-restore')
    assert damaged.read_bytes() == previous[damaged.name][0], 'A corrupt generated artifact was reused'
    previous = {path.name: (path.read_bytes(), path.stat().st_mtime_ns) for path in artifacts}
    factor.unlink()
    diagnostic = run('build', '--no-restore', success=False)
    assert 'factor.inc' in diagnostic and 'fragment' in diagnostic, diagnostic
    assert previous == {path.name: (path.read_bytes(), path.stat().st_mtime_ns) for path in artifacts}
    factor.write_text('#define FACTOR 0.2\n')
    external.write_bytes(b'broken SPIR-V')
    assert 'spirv-val failed' in run('build', '--no-restore', success=False)
    assert previous == {path.name: (path.read_bytes(), path.stat().st_mtime_ns) for path in artifacts}
    external.write_bytes(previous['external.spv.spv'][0])
    print('Dependencies: nested guarded macro include, unchanged timestamp, missing include; failures preserve artifacts.', flush=True)

    declarations = items.findall('Electron2DShader')
    for invalid, message in [('../escape.spv', 'TargetPath must be'), ('/escape.spv', 'TargetPath must be'),
                              ('Shaders/Surface.glsl.spv', 'Duplicate shader TargetPath')]:
        declarations[0].set('TargetPath', invalid)
        save_project()
        assert message in run('build', '--no-restore', success=False)
    del declarations[0].attrib['TargetPath']
    declarations[0].set('Stage', 'compute')
    save_project()
    assert 'Stage must be vertex or fragment' in run('build', '--no-restore', success=False)
    declarations[0].set('Stage', 'fragment')
    save_project()
    assert 'Electron2DShaderImportTool is missing' in run('build', '--no-restore', '-p:Electron2DShaderImportTool=missing', success=False)

    publish = directory / 'publish'
    # SDK publish transforms do not escape apostrophes in an absolute PublishDir.
    # Keep it relative while the project and shader paths still exercise quoting.
    run('publish', '-r', 'linux-x64', '--self-contained', 'true', '-p:PublishDir=publish/')
    names = [path.name for path in publish.rglob('*')]
    assert 'glslangValidator' not in names and 'Electron2D.ShaderImport.dll' not in names and 'toolchain' not in names
    assert not any(name.endswith(('.hlsl', '.glsl', '.inc')) for name in names)
    environment = os.environ.copy()
    environment.pop('LD_LIBRARY_PATH', None)
    environment['PATH'] = ''
    result = subprocess.run([str(publish / 'Consumer')], cwd='/tmp', env=environment, capture_output=True, text=True, timeout=30)
    assert result.returncode == 0 and 'Public shader consumer passed' in result.stdout, result.stdout + result.stderr
    factor.unlink()
    run('publish', '--no-build', '-r', 'linux-x64', '--self-contained', 'true', '-p:PublishDir=publish/')
    factor.write_text('#define FACTOR 0.2\n')
    run('clean', '-p:Electron2DShaderImportTool=missing')
    assert not any(path.exists() for path in artifacts), 'Clean left generated shaders behind'
    print('Build validation, self-contained public consumer, publish --no-build and Clean passed.', flush=True)
