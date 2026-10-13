#!/usr/bin/env python3
"""Validate canvas glyph composition independently of GPU driver acceptance."""
from pathlib import Path
import os
import struct
import subprocess
import tempfile
import build_toolchain

root = Path(__file__).resolve().parents[2]
validator = build_toolchain.build() / 'bin' / ('spirv-val' + build_toolchain.suffix)
with tempfile.TemporaryDirectory(prefix='e2d-canvas-glyphs-') as directory:
    environment = dict(os.environ, ELECTRON2D_TEST_CANVAS_GLYPH='modules', ELECTRON2D_GLYPH_EXPORT_DIR=directory)
    subprocess.run(['dotnet', 'run', '--project', str(root / 'tests/Electron2D.Tests/Electron2D.Tests.csproj'), '-c', 'Release', '--no-build'], env=environment, cwd=root, check=True)
    modules = list(Path(directory).glob('*.spv'))
    assert len(modules) == 732, f'Missing glyph/profile combinations: {len(modules)}'
    for module in modules:
        version = struct.unpack_from('<I', module.read_bytes(), 4)[0]
        target = 'vulkan1.2' if version >= 0x10500 else 'vulkan1.0'
        subprocess.run([str(validator), '--target-env', target, str(module)], check=True)
print('732 glyph modules validated: combined/paired samplers, GLSL/DXC HLSL, SPIR-V 1.5, four uniform slots, no canvas sampler, lighting channels, discard and early return.')
