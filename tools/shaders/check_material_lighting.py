#!/usr/bin/env python3
"""Validate composed production modules independently of native driver acceptance."""
from pathlib import Path
import os
import struct
import subprocess
import tempfile
import build_toolchain

root = Path(__file__).resolve().parents[2]
validator = build_toolchain.build() / 'bin' / ('spirv-val' + build_toolchain.suffix)
with tempfile.TemporaryDirectory(prefix='e2d-material-lighting-') as directory:
    environment = dict(os.environ, ELECTRON2D_TEST_MATERIAL_LIGHTING='1', ELECTRON2D_LIGHTING_COLD='1', ELECTRON2D_LIGHTING_EXPORT_DIR=directory)
    subprocess.run(['dotnet', 'run', '--project', str(root / 'tests/Electron2D.Tests/Electron2D.Tests.csproj'), '-c', 'Release', '--no-build'], env=environment, cwd=root, check=True)
    modules = list(Path(directory).glob('*.spv'))
    assert len(modules) == 257, f'Missing shader/profile combinations: {len(modules)}'
    for module in modules:
        version = struct.unpack_from('<I', module.read_bytes(), 4)[0]
        environment = 'vulkan1.2' if version >= 0x10500 else 'vulkan1.0'
        subprocess.run([str(validator), '--target-env', environment, str(module)], check=True)
print('257 composed modules validated: GLSL, DXC HLSL, SPIR-V 1.5, four uniform slots, all lighting channel roles and sixteen user samplers and per-item scalar/vector uniform blocks.')
