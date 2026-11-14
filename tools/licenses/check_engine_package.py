#!/usr/bin/env python3
"""Verify exact legal documents and managed identity in a freshly packed consumer."""

import argparse
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[2]


def check(package, consumer):
    expected = {path.name: path.read_bytes() for path in (ROOT / 'licence').iterdir()
                if path.is_file() and path.name != 'ReferenceData-LICENSE.txt'}
    with zipfile.ZipFile(package) as archive:
        delivered = {name.removeprefix('licence/'): archive.read(name)
                     for name in archive.namelist() if name.startswith('licence/') and not name.endswith('/')}
        if delivered != expected:
            raise ValueError('Packaged legal document closure or exact bytes differ')
        nuspec, = [name for name in archive.namelist() if name.endswith('.nuspec')]
        metadata = ET.fromstring(archive.read(nuspec))
        values = {item.tag.split('}')[-1]: item.text for item in metadata.iter()}
        authors = ET.parse(ROOT / 'Electron2D.csproj').findtext('.//Authors')
        if values['authors'] != authors:
            raise ValueError('Packaged authors differ from source metadata')
        identity = 'Electron2D/' + values['version']
        dependencies = json.loads((consumer / 'EngineMetadata.Consumer.deps.json').read_text(encoding='utf-8'))
        if identity not in dependencies['libraries'] or dependencies['libraries'][identity]['type'] != 'package':
            raise ValueError('Consumer did not reference the fresh package')
        assembly = archive.read('lib/net10.0/Electron2D.dll')
        if (consumer / 'Electron2D.dll').read_bytes() != assembly:
            raise ValueError('Consumer assembly differs from the fresh package')
        if not archive.read('lib/net10.0/Electron2D.xml'):
            raise ValueError('Packaged API XML is missing')
    print(f'{identity}: {len(expected)} exact legal files; consumer DLL SHA256={hashlib.sha256(assembly).hexdigest()}; package SHA256={hashlib.sha256(package.read_bytes()).hexdigest()}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('package', type=Path)
    parser.add_argument('consumer', type=Path)
    args = parser.parse_args()
    check(args.package, args.consumer)
