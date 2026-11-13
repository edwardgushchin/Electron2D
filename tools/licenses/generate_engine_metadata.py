#!/usr/bin/env python3
"""Embed the reviewed distribution catalog and verbatim shipped license documents."""

import argparse
import hashlib
import io
import json
from pathlib import Path
import re
import struct
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
AUTHOR_KEYS = ('lead_developers', 'founders', 'project_managers', 'developers')
DONOR_KEYS = ('platinum_sponsors', 'gold_sponsors', 'silver_sponsors',
              'bronze_sponsors', 'mini_sponsors', 'gold_donors',
              'silver_donors', 'bronze_donors')
EXCLUDED = {'ReferenceData-LICENSE.txt', 'THIRD_PARTY_NOTICES.md'}


def load(root=ROOT):
    catalog = json.loads((root / 'tools/licenses/engine_metadata.json').read_text(encoding='utf-8'))
    if not catalog['authors'].keys() <= set(AUTHOR_KEYS) - {'developers'} or not catalog['donors'].keys() <= set(DONOR_KEYS):
        raise ValueError('Unknown contributor category or overridden package authors')
    documents = {p.name: p.read_bytes().decode('utf-8-sig').replace('\r\n', '\n')
                 for p in sorted((root / 'licence').iterdir())
                 if p.suffix in ('.txt', '.md') and p.name not in EXCLUDED}
    recorded = [part['license'] for component in catalog['components'] for part in component['parts']]
    if len(set(recorded)) != len(recorded) or set(recorded) != documents.keys():
        raise ValueError('Distribution license catalog is incomplete or duplicated')
    notices = (root / 'licence/THIRD_PARTY_NOTICES.md').read_text(encoding='utf-8')
    for link in re.findall(r'\]\(([^)]+)\)', notices):
        if not link.startswith(('http:', 'https:')):
            if not (root / 'licence' / link.split('#')[0]).is_file():
                raise ValueError(f'Missing notice source: {link}')
    for component in catalog['components']:
        for part in component['parts']:
            if not part['files'] or not all(list(root.glob(path)) for path in part['files']):
                raise ValueError(f'Missing component source scope: {component["name"]}')
            if hashlib.sha256(documents[part['license']].encode('utf-8')).hexdigest() != part['sha256']:
                raise ValueError(f'License document changed; review attribution: {part["license"]}')
            attribution_texts = [documents[part['license']]]
            for source, digest in part.get('attributionSources', {}).items():
                contents = (root / source).read_text(encoding='utf-8-sig')
                if source not in part['files'] or hashlib.sha256(contents.encode('utf-8')).hexdigest() != digest:
                    raise ValueError(f'Attribution source changed; review attribution: {source}')
                attribution_texts.append(contents)
            for attribution in part['copyright']:
                if not any(attribution in contents for contents in attribution_texts):
                    raise ValueError(f'Copyright attribution differs: {part["license"]}')
    authors = tuple(filter(None, (name.strip() for name in
                    ET.parse(root / 'Electron2D.csproj').findtext('.//Authors').split(';'))))
    return catalog, documents, authors


def generate(root=ROOT):
    catalog, documents, authors = load(root)
    output = io.BytesIO()

    def count(value):
        output.write(struct.pack('<i', value))

    def text(value):
        data = value.encode('utf-8')
        count(len(data))
        output.write(data)

    def strings(values):
        count(len(values))
        for value in values:
            text(value)

    def categories(keys, values):
        count(len(keys))
        for key in keys:
            text(key)
            strings(values.get(key, []))

    count(1)
    categories(AUTHOR_KEYS, {'developers': authors, **catalog['authors']})
    categories(DONOR_KEYS, catalog['donors'])
    count(len(documents))
    for name, contents in documents.items():
        text(name)
        text(contents)
    count(len(catalog['components']))
    for component in catalog['components']:
        text(component['name'])
        count(len(component['parts']))
        for part in component['parts']:
            strings(part['files'])
            strings(part['copyright'])
            text(part['license'])
    return output.getvalue()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    data = generate()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    if not args.output.exists() or args.output.read_bytes() != data:
        args.output.write_bytes(data)
