#!/usr/bin/env python3
"""Import or verify the immutable PCRE2 interpreter source closure."""
import argparse
import hashlib
import io
import json
from pathlib import Path
import tarfile
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
DESTINATION = ROOT / 'src/Vendor/PCRE2'
COMMIT = 'f454e231fe5006dd7ff8f4693fd2b8eb94333429'
ARCHIVE_SHA256 = 'adeff182406731fd92364386a20063ddfb7a1011b9ebec134e0ffbdcca219e5e'
SOURCES = '''auto_possess chkdint compile compile_cgroup compile_class config context convert dfa_match error extuni find_bracket jit_compile maketables match match_data match_next newline ord2utf pattern_info script_run serialize string_utils study substitute substring tables ucd valid_utf xclass'''.split()

def check():
    manifest = json.loads((DESTINATION / 'manifest.json').read_text())
    assert manifest['commit'] == COMMIT and manifest['version'] == '10.47' and manifest['archiveSha256'] == ARCHIVE_SHA256
    files = {p.relative_to(DESTINATION).as_posix() for p in DESTINATION.rglob('*') if p.is_file() and p.name != 'manifest.json'}
    assert files == set(manifest['files'])
    for name, digest in manifest['files'].items():
        assert hashlib.sha256((DESTINATION / name).read_bytes()).hexdigest() == digest, name
    assert (ROOT / 'licence/PCRE2-LICENSE.md').read_bytes() == (DESTINATION / 'LICENCE.md').read_bytes()
    print('PCRE2 pinned source closure verified:', len(files), 'files')

def main():
    parser = argparse.ArgumentParser(); parser.add_argument('--check', action='store_true'); parser.add_argument('--archive', type=Path); args = parser.parse_args()
    if args.check:
        check()
        if args.archive:
            data = args.archive.read_bytes(); assert hashlib.sha256(data).hexdigest() == ARCHIVE_SHA256
            with tarfile.open(fileobj=io.BytesIO(data), mode='r:gz') as archive:
                entries = {x.name.split('/',1)[1]:x for x in archive.getmembers() if '/' in x.name and x.isfile()}
                for name in json.loads((DESTINATION/'manifest.json').read_text())['files']:
                    assert (DESTINATION/name).read_bytes() == archive.extractfile(entries[name]).read(), name
        return
    data = args.archive.read_bytes() if args.archive else urllib.request.urlopen('https://codeload.github.com/PCRE2Project/pcre2/tar.gz/' + COMMIT, timeout=60).read()
    assert hashlib.sha256(data).hexdigest() == ARCHIVE_SHA256
    chosen = {'src/pcre2_' + name + '.c' for name in SOURCES} | {'src/pcre2_chartables.c.dist', 'src/config.h.generic', 'src/pcre2.h.generic', 'LICENCE.md', 'AUTHORS.md'}
    with tarfile.open(fileobj=io.BytesIO(data), mode='r:gz') as archive:
        entries = {item.name.split('/', 1)[1]: item for item in archive.getmembers() if '/' in item.name and item.isfile()}
        chosen |= {name for name in entries if name.startswith('src/') and name.endswith('.h') and name.count('/') == 1 and name not in {'src/pcre2posix.h', 'src/pcre2_printint_inc.h', 'src/pcre2test_inc.h'}}
        assert chosen <= entries.keys()
        files = {}
        for name in sorted(chosen):
            content = archive.extractfile(entries[name]).read(); destination = DESTINATION / name; destination.parent.mkdir(parents=True, exist_ok=True); destination.write_bytes(content); files[name] = hashlib.sha256(content).hexdigest()
    manifest = {'repository': 'https://github.com/PCRE2Project/pcre2', 'commit': COMMIT, 'version': '10.47', 'archiveSha256': hashlib.sha256(data).hexdigest(), 'runtimeSources': ['src/pcre2_' + name + '.c' for name in SOURCES], 'files': files, 'adaptations': []}
    (DESTINATION / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
    (ROOT / 'licence/PCRE2-LICENSE.md').write_bytes((DESTINATION / 'LICENCE.md').read_bytes()); check()

if __name__ == '__main__':
    main()
