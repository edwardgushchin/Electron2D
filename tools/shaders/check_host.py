#!/usr/bin/env python3
"""Exercise the shared SDK task's external-producer boundary on Linux."""
from pathlib import Path
from tempfile import TemporaryDirectory
from xml.sax.saxutils import escape
import argparse
import hashlib
import json
import subprocess
import sys
import time


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--receipt', type=Path)
    args = parser.parse_args()
    if not sys.platform.startswith('linux'):
        parser.error('This fixture uses executable scripts and /proc on the current Linux platform gate.')
    target = Path(__file__).resolve().with_name('Electron2D.Shaders.targets')
    rows = []
    with TemporaryDirectory(prefix='electron2d-sdk-host-') as folder:
        root = Path(folder)
        (root / 'probe.proj').write_text('<Project><Import Project="' + escape(str(target), {'"': '&quot;'}) + '"/>'
            '<ItemGroup><Source Include="source.glsl"><Stage>fragment</Stage><TargetPath>probe.spv</TargetPath></Source></ItemGroup>'
            '<Target Name="Probe"><Electron2DImportShaders Sources="@(Source)" ProjectDirectory="$(MSBuildProjectDirectory)" '
            'OutputDirectory="result" ToolPath="producer.py" TimeoutMS="$(ProbeTimeout)" Compile="true"/></Target></Project>')
        (root / 'source.glsl').write_text('// Producer-boundary fixture.\n')
        (root / 'result').mkdir()
        output = root / 'result/probe.spv'
        cases = [
            ('failed-producer', 'Path(sys.argv[3]).write_bytes(b"failed")\nraise SystemExit(1)', 2000, False),
            ('success', 'Path(sys.argv[3]).write_bytes(b"new payload")', 2000, True),
            ('identical', 'Path(sys.argv[3]).write_bytes(b"old artifact")', 2000, True),
            ('no-output', 'pass', 2000, False),
            ('timeout-after-write', 'Path(sys.argv[3]).write_bytes(b"uncommitted")\nimport time,subprocess,json\n'
                'child=subprocess.Popen([sys.executable,"-c","import time;time.sleep(60)"],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)\n'
                'Path("child.json").write_text(json.dumps({"pid":child.pid,"ticks":int(Path("/proc",str(child.pid),"stat").read_text().rsplit(")",1)[1].split()[19])}))\n'
                'time.sleep(60)', 500, False),
            ('invalid-deadline', 'raise RuntimeError("Producer must not run")', 0, False),
            ('symlink-output', 'Path(sys.argv[3]).symlink_to("../source.glsl")', 2000, False),
        ]
        for name, script, timeout, success in cases:
            tool = root / 'producer.py'
            tool.write_text('#!' + sys.executable + '\nfrom pathlib import Path\nimport sys\n' + script + '\n')
            tool.chmod(0o700)
            output.write_bytes(b'old artifact')
            before = hashlib.sha256(output.read_bytes()).hexdigest()
            mtime = output.stat().st_mtime_ns
            command = ['dotnet', 'msbuild', 'probe.proj', '-nologo', '-t:Probe', '-p:ProbeTimeout=' + str(timeout)]
            started = time.monotonic()
            run = subprocess.run(command, cwd=root, capture_output=True, timeout=40)
            diagnostic = (run.stdout + run.stderr).decode(errors='replace')
            assert (run.returncode == 0) == success, (name, diagnostic)
            if name == 'success':
                assert output.read_bytes() == b'new payload', name
            else:
                assert hashlib.sha256(output.read_bytes()).hexdigest() == before, name
            if name == 'identical':
                assert output.stat().st_mtime_ns == mtime, name
            assert sorted(f.name for f in output.parent.iterdir()) == ['probe.spv'], name
            if name == 'timeout-after-write':
                child = json.loads((root / 'child.json').read_text())
                stat = Path('/proc', str(child['pid']), 'stat')
                if stat.exists():
                    fields = stat.read_text().rsplit(')', 1)[1].split()
                    assert int(fields[19]) != child['ticks'] or fields[0] == 'Z', 'Owned descendant remains running.'
            rows.append(dict(name=name, exit=run.returncode, seconds=round(time.monotonic() - started, 3)))
            print(name + ': passed', flush=True)
    if args.receipt:
        args.receipt.write_text(json.dumps(rows, indent=2) + '\n')
    print('SDK publication, unchanged timestamps, deadlines, descendant termination and staged cleanup passed.')


if __name__ == '__main__':
    main()
