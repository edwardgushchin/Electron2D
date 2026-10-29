#!/usr/bin/env python3
"""Exercise native overrides across source-content and package-content builds."""
import hashlib
import subprocess
import tempfile
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
PROPS = ROOT / 'tools/native-consumer.props'


def main():
    tree = ET.parse(PROPS)
    defaults = {item.tag: item.text for group in tree.iter('PropertyGroup')
                for item in group if 'Condition' not in item.attrib}
    roles = []
    for target in tree.findall('Target'):
        for copy in target.findall('Copy'):
            folder = copy.get('DestinationFolder', '')
            if folder.startswith('$(TargetDir)'):
                assert copy.get('SourceFiles') == '@(_Electron2DConsumerTLS)'
                relative = folder.removeprefix('$(TargetDir)').replace('$(RuntimeIdentifier)', 'linux-x64')
                for name in ['_Electron2DConsumerCryptoName', '_Electron2DConsumerSSLName']:
                    roles.append(('Electron2DTLSNativeDirectory', relative + '/' + defaults[name]))
                continue
            destination = copy.get('DestinationFiles', '')
            if not destination.startswith('$(TargetDir)'):
                continue
            source = copy.get('SourceFiles', '')
            assert source.startswith('$(') and source.endswith(')')
            relative = destination.removeprefix('$(TargetDir)').replace('$(RuntimeIdentifier)', 'linux-x64')
            for name, value in defaults.items():
                relative = relative.replace('$(' + name + ')', value)
            assert '$(' not in relative, relative
            roles.append((source[2:-1], relative))
    assert roles
    with tempfile.TemporaryDirectory(prefix='electron2d native overrides ') as directory:
        work = Path(directory)
        project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
        properties = ET.SubElement(project, 'PropertyGroup')
        for name, value in {'OutputType': 'Exe', 'TargetFramework': 'net10.0',
                            'RuntimeIdentifier': 'linux-x64', 'UseAppHost': 'false',
                            'SelfContained': 'false', 'TreatWarningsAsErrors': 'true'}.items():
            ET.SubElement(properties, name).text = value
        ET.SubElement(project, 'Import', Project=str(PROPS))
        ET.SubElement(ET.SubElement(project, 'ItemGroup'), 'PackageReference', Remove='@(PackageReference)')
        source_items = ET.SubElement(project, 'ItemGroup', Condition="'$(WithSourceAssets)' == 'true'")
        arguments = []
        expected = {}
        for index, (name, relative) in enumerate(roles):
            selected = work / ('selected source ' + str(index))
            if name == 'Electron2DTLSNativeDirectory':
                selected = work / 'selected TLS source' / Path(relative).name
                selected.parent.mkdir(exist_ok=True)
            selected.write_text('selected override ' + name + relative)
            original = work / ('old source ' + str(index))
            original.write_text('old source artifact ' + name)
            content = ET.SubElement(source_items, 'Content', Include=str(original))
            ET.SubElement(content, 'Link').text = relative
            ET.SubElement(content, 'CopyToOutputDirectory').text = 'PreserveNewest'
            argument = '-p:' + name + '=' + str(selected.parent if name == 'Electron2DTLSNativeDirectory' else selected)
            if argument not in arguments:
                arguments.append(argument)
            expected[work / 'bin/Release/net10.0/linux-x64' / relative] = hashlib.sha256(selected.read_bytes()).digest()
        ET.ElementTree(project).write(work / 'Consumer.csproj', encoding='unicode')
        (work / 'Program.cs').write_text('System.Console.WriteLine("native override fixture");\n')
        for source in [True, False, True]:
            result = subprocess.run(['dotnet', 'build', str(work / 'Consumer.csproj'), '-c', 'Release',
                                     '-p:WithSourceAssets=' + str(source).lower(), *arguments],
                                    cwd=work, capture_output=True, text=True)
            if result.returncode:
                raise AssertionError(result.stdout + result.stderr)
            lost = [str(path.relative_to(work)) for path, digest in expected.items()
                    if not path.is_file() or hashlib.sha256(path.read_bytes()).digest() != digest]
            assert not lost, f'Native overrides lost after WithSourceAssets={source}: {lost}'
        print(f'Native overrides: {len(roles)} roles survive source/package/source transitions and paths with spaces.')


if __name__ == '__main__':
    main()
