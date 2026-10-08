#!/usr/bin/env python3
"""Check source-only built-ins, include invalidation, failure, cleanup and embedded delivery."""
from pathlib import Path
import os
import subprocess
import tempfile
from xml.sax.saxutils import escape

from build_runtime import generate, ROOT


with tempfile.TemporaryDirectory(prefix='electron2d-runtime-shaders-') as temporary:
    root = Path(temporary)
    rendering = root / 'src/Servers/Rendering/Shaders'
    physics = root / 'src/Servers/Physics/Shaders'
    rendering.mkdir(parents=True); physics.mkdir(parents=True)
    (rendering / 'Test.frag.glsl').write_text('#version 450\nlayout(location=0) out vec4 c; void main() { c=vec4(1); }\n')
    (physics / 'Test.comp.glsl').write_text('''#version 450
#extension GL_GOOGLE_include_directive : require
#include "Value.inc.glsl"
layout(local_size_x=1) in;
layout(set=0,binding=0) buffer Data { float value; } data;
void main() { data.value = VALUE; }
''')
    include = physics / 'Value.inc.glsl'
    include.write_text('#define VALUE 1.0\n')
    output = root / 'generated'
    first = generate(output, root)
    stamps = {key: (output / key).stat().st_mtime_ns for key in first}
    assert generate(output, root) == first
    assert stamps == {key: (output / key).stat().st_mtime_ns for key in first}, 'No-op generation rewrote outputs'
    include.write_text('#define VALUE 2.0\n')
    second = generate(output, root)
    assert first['Physics/Test.comp.spv']['binary'] != second['Physics/Test.comp.spv']['binary']
    assert (output / 'Rendering/Test.frag.spv').stat().st_mtime_ns == stamps['Rendering/Test.frag.spv']
    good = (output / 'Physics/Test.comp.spv').read_bytes()
    include.write_text('invalid shader\n')
    try:
        generate(output, root)
        raise AssertionError('Invalid source accepted')
    except subprocess.CalledProcessError:
        pass
    assert (output / 'Physics/Test.comp.spv').read_bytes() == good, 'Failure replaced last valid bytecode'
    include.write_text('#define VALUE 2.0\n')
    stale = output / 'Physics/Removed.comp.spv'; stale.write_bytes(good)
    generate(output, root)
    assert not stale.exists(), 'Removed shader was retained'
    (output / 'Physics/Test.comp.spv').write_bytes(b'corrupt')
    generate(output, root)
    assert (output / 'Physics/Test.comp.spv').read_bytes() == good, 'Corrupt cache was reused'
    # A fresh MSBuild consumer embeds generated resources and ships no compiler/toolchain.
    (root / 'Probe.csproj').write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType>
<Electron2DNativePython>{'python' if os.name == 'nt' else 'python3'}</Electron2DNativePython></PropertyGroup>
<Import Project="{escape(str(ROOT / 'tools/shaders/RuntimeShaders.targets'))}" />
</Project>''')
    (root / 'Program.cs').write_text('''using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
var assembly = Assembly.GetExecutingAssembly();
var manifest = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(args[0] + "/manifest.json"));
var count = 0;
foreach (var item in manifest.EnumerateObject()) {
    var parts = item.Name.Split('/');
    var prefix = parts[0] == "Rendering" ? "Electron2D.Shaders." : "Electron2D.PhysicsShaders.";
    using var resource = assembly.GetManifestResourceStream(prefix + parts[1]) ?? throw new Exception(item.Name);
    if (Convert.ToHexStringLower(SHA256.HashData(resource)) != item.Value.GetProperty("binary").GetString())
        throw new Exception("Embedded bytes differ: " + item.Name);
    count++;
}
if (count < 5 || count != assembly.GetManifestResourceNames().Length) throw new Exception("Missing or stale shaders");
Console.WriteLine($"Verified {count} generated resources in published assembly");
''')
    publish = root / 'published'
    subprocess.run(['dotnet', 'publish', str(root / 'Probe.csproj'), '-c', 'Release', '-o', str(publish), '--nologo'], check=True)
    generated = root / 'obj/Release/net10.0/shaders'
    subprocess.run(['dotnet', str(publish / 'Probe.dll'), str(generated)], check=True)
    assert not any(path.suffix in ('.spv', '.glsl', '.hlsl', '.py') or path.name in ('glslangValidator', 'spirv-val')
                   for path in publish.rglob('*')), 'Build inputs/tools leaked into publish'
    subprocess.run(['dotnet', 'clean', str(root / 'Probe.csproj'), '-c', 'Release', '--nologo'], check=True)
    assert not list(generated.rglob('*.spv')) and not (generated / 'manifest.json').exists(), 'Clean left generated shaders'
print('Runtime shader generation checks passed')
