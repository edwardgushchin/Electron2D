#!/usr/bin/env python3
"""Verify the published shader importer in an ordinary public-API consumer project."""
from pathlib import Path
import argparse
import json
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
    hlsl = '#include "nested/entry.inc"\ncbuffer BuildValues:register(b0,space3){float authoredGain;};\nfloat4 main(float4 color : TEXCOORD0) : SV_Target0 { return color * (FACTOR+authoredGain); }\n'
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
    for key, value in [('OutputType', 'Exe'), ('TargetFramework', 'net10.0'), ('ImplicitUsings', 'enable'),
                       ('TreatWarningsAsErrors', 'true'), ('AppendRuntimeIdentifierToOutputPath', 'false')]:
        XML.SubElement(properties, key).text = value
    items = XML.SubElement(project, 'ItemGroup')
    XML.SubElement(items, 'ProjectReference', Include=escape(str(root / 'Electron2D.csproj')))
    for source in ['Surface.hlsl', 'Surface.glsl', special, 'external.spv']:
        XML.SubElement(items, 'Electron2DShader', Include=escape('Shaders/' + source), Stage='fragment')
    XML.SubElement(project, 'Import', Project=escape(str(root / 'tools/native-consumer.props')))
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
    generated = directory / 'obj/Release/net10.0/Electron2D/shaders/Shaders'
    artifacts = sorted(generated.glob('*.spv'))
    assert len(artifacts) == 4
    previous = {path.name: (path.read_bytes(), path.stat().st_mtime_ns) for path in artifacts}
    diagnostic = run('build', '--no-restore')
    assert diagnostic.count('Unchanged ') == 4, diagnostic
    assert previous == {path.name: (path.read_bytes(), path.stat().st_mtime_ns) for path in artifacts}
    assert not (directory / 'INJECTED').exists(), 'Build arguments were interpreted by a shell'
    print('Build: both languages, external bytecode, quoted paths, stable unchanged output.', flush=True)

    defaults = shaders / 'Surface.hlsl.defaults.json'
    defaults.write_text(json.dumps({'version': 1, 'parameters': {'authoredGain': .25}}))
    run('build', '--no-restore')
    authored = generated / 'Surface.hlsl.spv'
    assert authored.read_bytes() != previous[authored.name][0], 'Added defaults did not rebuild the shader'
    first_defaults = authored.read_bytes()
    stamp = defaults.stat()
    defaults.write_text(json.dumps({'version': 1, 'parameters': {'authoredGain': .75}}))
    os.utime(defaults, ns=(stamp.st_atime_ns, stamp.st_mtime_ns))
    run('build', '--no-restore')
    assert authored.read_bytes() != first_defaults, 'A same-timestamp default edit was ignored'
    valid_defaults = authored.read_bytes()
    defaults.write_text('{"version":1,"parameters":{"missing":1}}')
    assert str(defaults) in run('build', '--no-restore', success=False)
    assert authored.read_bytes() == valid_defaults, 'Invalid defaults replaced usable output'
    defaults.unlink(); run('build', '--no-restore')
    assert all(path.read_bytes() == previous[path.name][0] for path in artifacts), 'Removed defaults remained in a rebuilt artifact'
    previous = {path.name: (path.read_bytes(), path.stat().st_mtime_ns) for path in artifacts}
    print('Defaults: addition, unchanged-timestamp edit, invalid atomic failure and removal rebuild correctly.', flush=True)

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
    assert 'Stage must be vertex/fragment' in run('build', '--no-restore', success=False)
    declarations[0].set('Stage', 'fragment')
    save_project()
    assert 'Electron2DShaderImportTool is missing' in run('build', '--no-restore', '-p:Electron2DShaderImportTool=missing', success=False)

    declarations[0].set('Profile', 'invalid')
    save_project()
    assert 'Profile must be canvas, device, texture-blit or particles' in run('build', '--no-restore', success=False)
    del declarations[0].attrib['Profile']
    for name, stage, original in [('DeviceComputeGLSL.glsl', 'compute', 'LocalCompute.comp.glsl'),
                                   ('DeviceComputeHLSL.hlsl', 'compute', 'LocalComputeHLSL.comp.hlsl'),
                                   ('DeviceVertex.glsl', 'vertex', 'DevicePushGLSL.vert.glsl'),
                                   ('DeviceFragment.glsl', 'fragment', 'DevicePushGLSL.frag.glsl')]:
        shutil.copyfile(root / 'tests/Electron2D.Tests/Shaders' / original, shaders / name)
        XML.SubElement(items, 'Electron2DShader', Include='Shaders/' + name, Stage=stage, Profile='device')
    (directory / 'Program.cs').write_text((directory / 'Program.cs').read_text().replace('if (files.Length != 4)', 'if (files.Length != 8)').replace('foreach (var file in files) {', 'foreach (var file in files.Where(f => !System.IO.Path.GetFileName(f).StartsWith("Device"))) {') + '\n' + """using var device = RenderingServer.CreateLocalRenderingDevice();
foreach (var file in files.Where(f => System.IO.Path.GetFileName(f).StartsWith("DeviceCompute"))) {
    using var module = new RDShaderSPIRV { BytecodeCompute = File.ReadAllBytes(file) };
    var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
    float[] input = [1, 2, 3, 4]; var buffer = device.StorageBufferCreate(16, System.Runtime.InteropServices.MemoryMarshal.AsBytes(input.AsSpan()));
    float[] settings = [2, 4, 3, 0]; var uniform = device.UniformBufferCreate(16, System.Runtime.InteropServices.MemoryMarshal.AsBytes(settings.AsSpan()));
    using var storage = new RDUniform { Binding = 7, UniformType = RenderingDevice.UniformType.StorageBuffer }; storage.AddID(buffer);
    using var parameters = new RDUniform { Binding = 4, UniformType = RenderingDevice.UniformType.UniformBuffer }; parameters.AddID(uniform);
    var dataSet = device.UniformSetCreate([storage], shader, 3); var inputSet = device.UniformSetCreate([parameters], shader, 2);
    var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, dataSet, 3); device.ComputeListBindUniformSet(list, inputSet, 2); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
    device.BufferGetData(buffer, System.Runtime.InteropServices.MemoryMarshal.AsBytes(input.AsSpan()));
    if (!input.SequenceEqual(new float[] {5, 7, 9, 11})) throw new Exception("Imported compute result differs");
}
using var graphics = new RDShaderSPIRV { BytecodeVertex = File.ReadAllBytes(files.Single(f => f.EndsWith("DeviceVertex.glsl.spv"))), BytecodeFragment = File.ReadAllBytes(files.Single(f => f.EndsWith("DeviceFragment.glsl.spv"))) };
if (device.ShaderGetVertexInputAttributeMask(device.ShaderCreateFromSPIRV(graphics)) != 0) throw new Exception("Imported procedural graphics interface differs");
Console.WriteLine("Public device shader consumer passed");
""")
    save_project()
    run('build', '--no-restore')
    assert len(list(generated.glob('*.spv'))) == 8

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
