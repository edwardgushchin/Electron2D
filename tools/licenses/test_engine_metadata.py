#!/usr/bin/env python3
"""Regression controls for attribution/source drift before embedding metadata."""

import hashlib
import json
from pathlib import Path
import tempfile
import unittest

import generate_engine_metadata as metadata


class EngineMetadataTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        (self.root / 'tools/licenses').mkdir(parents=True)
        (self.root / 'licence').mkdir()
        self.text = 'Copyright (c) 2026 Example Author\n\nExample terms.\n'
        self.catalog = {'authors': {}, 'donors': {}, 'components': [{
            'name': 'Example', 'parts': [{'files': ['licence/Electron2D-LICENSE.txt'],
                                        'copyright': ['Copyright (c) 2026 Example Author'],
                                        'license': 'Electron2D-LICENSE.txt',
                                        'sha256': hashlib.sha256(self.text.encode()).hexdigest()}]}]}
        (self.root / 'licence/Electron2D-LICENSE.txt').write_text(self.text, encoding='utf-8', newline='\n')
        (self.root / 'licence/THIRD_PARTY_NOTICES.md').write_text('[Engine](Electron2D-LICENSE.txt)', encoding='utf-8')
        (self.root / 'Electron2D.csproj').write_text('<Project><PropertyGroup><Authors>Example Author</Authors></PropertyGroup></Project>')
        self.save()

    def save(self):
        (self.root / 'tools/licenses/engine_metadata.json').write_text(json.dumps(self.catalog), encoding='utf-8')

    def test_source_and_package_authors(self):
        self.assertEqual(metadata.load(self.root)[2], ('Example Author',))
        before = metadata.generate(self.root)
        (self.root / 'Electron2D.csproj').write_text('<Project><Authors>Changed Author</Authors></Project>')
        self.assertNotEqual(metadata.generate(self.root), before)

    def test_line_endings_and_bom_are_portable(self):
        before = metadata.generate(self.root)
        (self.root / 'licence/Electron2D-LICENSE.txt').write_bytes(b'\xef\xbb\xbf' + self.text.replace('\n', '\r\n').encode())
        self.assertEqual(metadata.generate(self.root), before)

    def test_new_missing_and_duplicate_documents(self):
        document = self.root / 'licence/Added.txt'
        document.write_text('new terms')
        with self.assertRaisesRegex(ValueError, 'incomplete or duplicated'):
            metadata.generate(self.root)
        document.unlink()
        self.catalog['components'][0]['parts'].append(dict(self.catalog['components'][0]['parts'][0]))
        self.save()
        with self.assertRaisesRegex(ValueError, 'incomplete or duplicated'):
            metadata.generate(self.root)
        (self.root / 'licence/Electron2D-LICENSE.txt').unlink()
        with self.assertRaisesRegex(ValueError, 'incomplete or duplicated'):
            metadata.generate(self.root)

    def test_changed_legal_text_requires_review(self):
        (self.root / 'licence/Electron2D-LICENSE.txt').write_text(self.text + 'Changed terms.\n', encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'review attribution'):
            metadata.generate(self.root)

    def test_false_attribution_rejected(self):
        self.catalog['components'][0]['parts'][0]['copyright'] = ['Invented Author']
        self.save()
        with self.assertRaisesRegex(ValueError, 'attribution differs'):
            metadata.generate(self.root)

    def test_broken_scope_and_notice_links(self):
        self.catalog['components'][0]['parts'][0]['files'] = ['missing/source.cs']
        self.save()
        with self.assertRaisesRegex(ValueError, 'source scope'):
            metadata.generate(self.root)
        (self.root / 'licence/THIRD_PARTY_NOTICES.md').write_text('[Missing](missing.txt)')
        with self.assertRaisesRegex(ValueError, 'notice source'):
            metadata.generate(self.root)

    def test_unrecognized_or_shadowed_author_categories(self):
        for key in ('developers', 'unrecognized'):
            self.catalog['authors'] = {key: ['Invented Author']}
            self.save()
            with self.assertRaisesRegex(ValueError, 'contributor category'):
                metadata.generate(self.root)

    def test_source_header_attribution_and_drift(self):
        source = self.root / 'source.h'
        source.write_text('Copyright (c) 2026 Source Author\n', encoding='utf-8', newline='\n')
        part = self.catalog['components'][0]['parts'][0]
        part['files'].append('source.h')
        part['copyright'] = ['Copyright (c) 2026 Source Author']
        part['attributionSources'] = {'source.h': hashlib.sha256(source.read_bytes()).hexdigest()}
        self.save()
        metadata.generate(self.root)
        source.write_text('Copyright (c) 2026 Added Author\n', encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'Attribution source changed'):
            metadata.generate(self.root)


if __name__ == '__main__':
    unittest.main()
