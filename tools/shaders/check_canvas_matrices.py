#!/usr/bin/env python3
"""Verify canvas matrix import ownership and public component consumers."""
import argparse
import hashlib
import json
import sys
from pathlib import Path
import subprocess


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--tool', type=Path, required=True)
    parser.add_argument('--consumer', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--render', action='store_true')
    args = parser.parse_args()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    sources, graphs = output / 'sources', output / 'graphs'
    graphs.mkdir(exist_ok=True)
    tool = ['dotnet', str(args.tool.resolve())] if args.tool.suffix == '.dll' else [str(args.tool.resolve())]
    consumer = ['dotnet', str(args.consumer.resolve())] if args.consumer.suffix == '.dll' else [str(args.consumer.resolve())]
    checks = []

    def run(name, command, success=True):
        result = subprocess.run(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
        (output / (name + '.log')).write_text(result.stdout)
        checks.append({'name': name, 'exit_code': result.returncode})
        if (result.returncode == 0) != success:
            raise AssertionError(f'{name}: {result.stdout[-5000:]}')
        return result.stdout

    run('cpu', consumer + ['matrix-inputs-cpu'])
    run('author', consumer + ['matrix-inputs-author', str(sources)])
    for name in ('matrix-nodes', 'matrix-expression', 'matrix-no-id'):
        run('import-' + name, tool + [str(sources / (name + '.e2dres')), 'canvas', str(graphs / (name + '.e2dres')), '--visual-shader'])
    for language in ('glsl', 'hlsl'):
        run('source-' + language, tool + [str(sources / ('matrix.vert.' + language)), 'vertex', str(graphs / ('matrix-' + language + '.vert.spv'))])
    compiler = args.tool.resolve().parent / 'toolchain' / 'bin' / ('glslangValidator.exe' if sys.platform == 'win32' else 'glslangValidator')
    validator = compiler.with_name('spirv-val.exe' if sys.platform == 'win32' else 'spirv-val')
    for suffix, target in (('13', 'vulkan1.1'), ('15', 'vulkan1.2')):
        module = graphs / ('matrix-spv' + suffix + '.vert.spv')
        run('compile-spv' + suffix, [str(compiler), '-V', '--target-env', target, '-S', 'vert', '-o', str(module), str(sources / 'matrix.vert.glsl')])
        run('validate-spv' + suffix, [str(validator), '--target-env', target, str(module)])
    run('fragment', tool + [str(sources / 'matrix.frag.glsl'), 'fragment', str(graphs / 'matrix.frag.spv')])
    run('process', tool + [str(sources / 'matrix-process.comp.glsl'), 'compute', str(graphs / 'matrix-process.spv'), '--particles'])
    target = graphs / 'matrix-glsl.vert.spv'
    digest = hashlib.sha256(target.read_bytes()).hexdigest()
    for source in sorted((sources / 'invalid').glob('*.glsl')):
        log = run('invalid-' + source.stem, tool + [str(source), 'fragment' if source.stem.endswith('-fragment') else 'vertex', str(target)], False)
        assert source.stem.split('-')[0] in log, log
        assert hashlib.sha256(target.read_bytes()).hexdigest() == digest
    if args.render:
        run('native', consumer + ['matrix-inputs-gpu', str(graphs)])
        run('native-modern', consumer + ['matrix-inputs-gpu', str(graphs), 'modern'])
    receipt = {'checks': checks, 'rendered': args.render, 'sha256': {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(graphs.iterdir()) if p.is_file()}}
    (output / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(f'Canvas matrices: {len(checks)} checks passed; rendered={args.render}.')


if __name__ == '__main__':
    main()
