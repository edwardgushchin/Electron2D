#!/usr/bin/env python3
"""Build an example/application compute module with the pinned shader toolchain."""
import argparse
import hashlib
import os
from pathlib import Path
import subprocess
import tempfile
import build_toolchain

parser = argparse.ArgumentParser()
parser.add_argument('--source', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
source, output = args.source.resolve(), args.output.resolve()
toolchain = build_toolchain.build()
output.parent.mkdir(parents=True, exist_ok=True)
fingerprint = hashlib.sha256(source.read_bytes() + source.suffix.encode() + Path(__file__).read_bytes() + (toolchain / 'recipe.sha256').read_bytes()).hexdigest()
stamp = output.with_suffix('.sha256')
with build_toolchain.exclusive(output.with_suffix('.lock')):
    if not (output.exists() and stamp.exists() and stamp.read_text() == fingerprint):
        with tempfile.TemporaryDirectory(dir=output.parent) as temporary:
            compiled = Path(temporary) / 'module.spv'
            command = [str(toolchain / 'bin' / ('glslangValidator' + build_toolchain.suffix)), '-V', '--target-env', 'vulkan1.0', '-S', 'comp', '-e', 'main']
            if source.suffix == '.hlsl':
                command += ['-D', '--hlsl-iomap']
            subprocess.run([*command, '-o', str(compiled), str(source)], check=True)
            subprocess.run([str(toolchain / 'bin' / ('spirv-val' + build_toolchain.suffix)), '--target-env', 'vulkan1.0', str(compiled)], check=True)
            os.replace(compiled, output)
        stamp.write_text(fingerprint)
