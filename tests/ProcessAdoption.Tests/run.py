"""Instrument the actual launcher only in an isolated failure-test build."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--previous', help='read the launcher from a preceding Git commit')
parser.add_argument('--aot', action='store_true')
args = parser.parse_args()
work = ROOT / 'bin/os-process-adoption' / ('previous' if args.previous else 'fixed-aot' if args.aot else 'fixed')
work.mkdir(parents=True, exist_ok=True)
source_path = 'src/Core/OS/ProcessPlatform.Windows.cs'
source = subprocess.check_output(['git', 'show', f'{args.previous}:{source_path}'], cwd=ROOT, text=True) if args.previous else (ROOT / source_path).read_text(encoding='utf-8')
create = r'(?m)^(\s*if \(!.*CreateWindows[^\n]+\n)'
source, count = re.subn(create, r'\1                ProcessAdoptionFault.Created(process.Process, process.Thread, process.PID); ProcessAdoptionFault.Reach(0);\n', source)
assert count == 1, 'one successful native creation boundary'
if not args.previous:
    source, count = re.subn(r'(?m)^(\s*Marshal.InitHandle\(handle, process.Process\); process.Process = 0;\n)', r'\1                ProcessAdoptionFault.Reach(1);\n', source)
    assert count == 1, 'process-handle adoption boundary'
    source, count = re.subn(r'(?m)^(\s*pipe\?\.DisposeLocalCopyOfClientHandle\(\);\n)', r'\1                ProcessAdoptionFault.Reach(2);\n', source)
    assert count == 1, 'pipe adoption boundary'
    source, count = re.subn(r'(?m)^(\s*var owner = new OSProcess\(handle, process.PID\);\n)', r'\1                ProcessAdoptionFault.Reach(3);\n', source)
    assert count == 1, 'managed-owner adoption boundary'
overlay = work / 'ProcessPlatform.Windows.cs'
overlay.write_text(source, encoding='utf-8')
project = ET.Element('Project')
target = ET.SubElement(project, 'Target', Name='InstrumentProcessAdoption', BeforeTargets='CoreCompile', Condition="'$(MSBuildProjectName)' == 'Electron2D'")
items = ET.SubElement(target, 'ItemGroup')
ET.SubElement(items, 'Compile', Remove='@(Compile)', Condition="'%(Compile.Filename)%(Compile.Extension)' == 'ProcessPlatform.Windows.cs'")
ET.SubElement(items, 'Compile', Include=str(overlay))
ET.SubElement(items, 'Compile', Include=str(Path(__file__).with_name('Probe.cs')))
targets = work / 'Adoption.targets'
ET.ElementTree(project).write(targets, encoding='utf-8', xml_declaration=True)
command = ['dotnet', 'publish', str(Path(__file__).with_name('ProcessAdoption.Tests.csproj')), '-c', 'Release', '-r', 'win-x64', f'-p:CustomAfterMicrosoftCommonTargets={targets}', '-o', str(work / 'app')]
if args.aot:
    command += ['-p:RuntimeFrameworkVersion=10.0.14', '-p:IlcPackageVersion=10.0.14', '-p:PublishAot=true', '-p:PublishTrimmed=true', '-p:TrimMode=full', '-p:ILLinkTreatWarningsAsErrors=true', '-p:IlcTreatWarningsAsErrors=true']
with (work / 'build.log').open('w', encoding='utf-8') as log:
    subprocess.run(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, check=True)
with (work / 'run.log').open('w', encoding='utf-8') as log:
    result = subprocess.run([str(work / 'app/Electron2D.Tests.exe'), *(['--raw-only'] if args.previous else [])], cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, timeout=45)
files = [overlay, targets, Path(__file__), Path(__file__).with_name('Probe.cs'), Path(__file__).with_name('Program.cs'), work / 'app/Electron2D.Tests.exe', work / 'run.log']
if (work / 'app/Electron2D.dll').exists():
    files.append(work / 'app/Electron2D.dll')
(work / 'identities.json').write_text(json.dumps({'exitCode': result.returncode, 'previous': args.previous, 'aot': args.aot, 'command': command, 'sha256': {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in files}}, indent=2), encoding='utf-8')
print((work / 'run.log').read_text(encoding='utf-8'))
raise SystemExit(result.returncode)
