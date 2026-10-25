"""Bind qualified package bytes to one trusted CI run; never rebuild during promotion."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import time
import tempfile
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
from xml.sax.saxutils import escape
from zipfile import ZipFile

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'native'))
import package as native


def feed_config(feed, output):
    output.write_text('<configuration><packageSources><clear />'
        f'<add key="candidate" value="{escape(str(feed.resolve()), {chr(34): "&quot;"})}" />'
        '<add key="public" value="https://api.nuget.org/v3/index.json" />'
        '</packageSources><packageSourceMapping><packageSource key="candidate">'
        '<package pattern="Electron2D" /><package pattern="Electron2D.*" />'
        '</packageSource><packageSource key="public"><package pattern="*" />'
        '</packageSource></packageSourceMapping></configuration>')


def inventory(feed):
    expected = {'Electron2D', *('Electron2D.' + x for x in native.PLATFORMS)}
    version = native.configuration()['version']
    fingerprint = native.source_hash()
    result = []
    for path in sorted(feed.glob('*.nupkg')):
        with ZipFile(path) as archive:
            names = archive.namelist()
            if len(set(names)) != len(names):
                raise ValueError('Duplicate package entries')
            specs = [name for name in names if name.endswith('.nuspec')]
            if len(specs) != 1:
                raise ValueError('Expected one package identity')
            spec = ET.fromstring(archive.read(specs[0]))
            fields = {item.tag.rsplit('}', 1)[-1]: item.text for item in list(spec)[0]}
            identity = fields.get('id')
            if identity not in expected or fields.get('version') != version or path.name != f'{identity}.{version}.nupkg':
                raise ValueError('Unexpected package identity, version or filename')
            expected.remove(identity)
            if identity == 'Electron2D':
                required = {f'lib/{tfm}/Electron2D.{ext}' for tfm in
                            ('net10.0', 'net10.0-android36.0', 'net10.0-ios26.0', 'net10.0-tvos26.0') for ext in ('dll', 'xml')}
                browser = {f'runtimes/browser-wasm/lib/net10.0/Electron2D.{ext}' for ext in ('dll', 'xml')}
                if not required.issubset(names) or {n for n in names if n.startswith('runtimes/')} != browser:
                    raise ValueError('Incomplete or foreign managed framework assets')
            else:
                platform = identity.split('.')[1]
                manifest = json.loads(archive.read('native-manifest.json'))
                if set(manifest) != set(native.PLATFORMS[platform]):
                    raise ValueError('Incomplete native RID cohort')
                payload = set()
                for rid, receipt in manifest.items():
                    if set(receipt['files']) != set(native.LIBRARIES[platform]):
                        raise ValueError('Incomplete native library closure')
                    if receipt['minimumEngineVersion'] != version or receipt['sourceSHA256'] != fingerprint:
                        raise ValueError('Stale native source/version receipt')
                    for name, digest in receipt['files'].items():
                        member = f'runtimes/{rid}/native/{name}'
                        payload.add(member)
                        if hashlib.sha256(archive.read(member)).hexdigest() != digest:
                            raise ValueError('Changed native payload')
                if {n for n in names if n.startswith('runtimes/')} != payload:
                    raise ValueError('Foreign native assets')
        result.append({'file': path.name, 'id': identity, 'version': version,
                       'size': path.stat().st_size, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
    if expected:
        raise ValueError('Missing packages: ' + ', '.join(sorted(expected)))
    return result


def write_manifest(feed):
    manifest = {'schema': 1, 'repository': os.environ['GITHUB_REPOSITORY'],
                'sha': os.environ['GITHUB_SHA'], 'run': int(os.environ['GITHUB_RUN_ID']),
                'attempt': int(os.environ['GITHUB_RUN_ATTEMPT']), 'packages': inventory(feed)}
    (feed / 'qualification.json').write_text(json.dumps(manifest, indent=2) + '\n')
    return manifest


def verify(feed, run):
    manifest = json.loads((feed / 'qualification.json').read_text())
    if (manifest.get('schema') != 1 or manifest.get('repository') != run['repository']['full_name']
            or manifest.get('sha') != run['head_sha'] or manifest.get('run') != run['id']
            or manifest.get('attempt') != run['run_attempt'] or manifest.get('packages') != inventory(feed)):
        raise ValueError('Qualification manifest does not match the successful run/package bytes')
    return manifest


def trusted_run(run, repository):
    if (run.get('status') != 'completed' or run.get('conclusion') != 'success'
            or run.get('event') not in {'push', 'workflow_dispatch', 'schedule'}
            or run.get('head_branch') != 'main' or run.get('path') != '.github/workflows/ci.yml'
            or run.get('repository', {}).get('full_name') != repository
            or run.get('head_repository', {}).get('full_name') != repository
            or not re.fullmatch('[0-9a-f]{40}', run.get('head_sha', ''))):
        raise ValueError('Publication requires a successful trusted main CI qualification')


def resolve(run_id):
    if not run_id.isdecimal():
        raise ValueError('Expected an explicit numeric qualification run ID')
    repo = os.environ['GITHUB_REPOSITORY']
    run = json.loads(subprocess.check_output(['gh', 'api', f'repos/{repo}/actions/runs/{run_id}']))
    trusted_run(run, repo)
    pages = json.loads(subprocess.check_output(['gh', 'api', '--paginate', '--slurp',
        f'repos/{repo}/actions/runs/{run_id}/artifacts?per_page=100']))
    artifacts = [artifact for page in pages for artifact in page['artifacts']]
    matches = [a for a in artifacts if a['name'] == 'release-qualified' and not a['expired']]
    if len(matches) != 1:
        raise ValueError('Missing, expired or ambiguous qualified artifact')
    print('artifact=' + str(matches[0]['id']))
    print('sha=' + run['head_sha'])
    (Path(os.environ.get('RUNNER_TEMP', '.')) / 'qualification-run.json').write_text(json.dumps(run))


def compare_public(original, published):
    with ZipFile(original) as left, ZipFile(published) as right:
        if len(right.namelist()) != len(set(right.namelist())):
            raise ValueError('Duplicate public package entries')
        if set(right.namelist()) != set(left.namelist()) | {'.signature.p7s'}:
            raise ValueError('Public package entries differ')
        for name in left.namelist():
            if left.read(name) != right.read(name):
                raise ValueError('Public package changed entry: ' + name)


def publish(feed):
    # Reconcile prior partial pushes before using a publishing credential again.
    manifest = json.loads((feed / 'qualification.json').read_text())
    with tempfile.TemporaryDirectory(prefix='e2d-public-compare-') as directory:
        for item in manifest['packages']:
            identity, version = item['id'].lower(), item['version'].lower()
            url = f'https://api.nuget.org/v3-flatcontainer/{identity}/{version}/{identity}.{version}.nupkg'
            try:
                with urllib.request.urlopen(url, timeout=60) as response:
                    public = Path(directory) / item['file']
                    public.write_bytes(response.read())
            except urllib.error.HTTPError as error:
                error.close()
                if error.code != 404:
                    raise
                pushed = subprocess.run(['dotnet', 'nuget', 'push', str(feed / item['file']),
                                         '--api-key', os.environ['NUGET_API_KEY'],
                                         '--source', 'https://api.nuget.org/v3/index.json'])
                if pushed.returncode:
                    raise RuntimeError('NuGet push failed for ' + item['id'])
            else:
                compare_public(feed / item['file'], public)
                subprocess.run(['dotnet', 'nuget', 'verify', str(public), '--all'], check=True)
                print('Already published identical payload: ' + item['id'])


def restore_public(manifest):
    with tempfile.TemporaryDirectory(prefix='e2d-public-restore-') as directory:
        root = Path(directory)
        config = root / 'nuget.config'
        config.write_text('<configuration><packageSources><clear />'
                          '<add key="public" value="https://api.nuget.org/v3/index.json" />'
                          '</packageSources></configuration>')
        references = ''.join(f'<PackageReference Include="{p["id"]}" Version="[{p["version"]}]" />'
                             for p in manifest['packages'])
        (root / 'Consumer.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
            '<TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup>' + references + '</ItemGroup></Project>')
        subprocess.run(['dotnet', 'restore', str(root / 'Consumer.csproj'), '--configfile', str(config),
                        '--packages', str(root / 'packages')], check=True,
                       env={k: v for k, v in os.environ.items() if k not in
                            {'RestoreAdditionalProjectSources', 'RestoreConfigFile', 'NUGET_PACKAGES'}})


def release_assets(feed):
    manifest = json.loads((feed / 'qualification.json').read_text())
    version = manifest['packages'][0]['version']
    tag = 'v' + version
    repo = os.environ['GITHUB_REPOSITORY']
    endpoint = f'repos/{repo}/releases/tags/{tag}'
    query = subprocess.run(['gh', 'api', endpoint], capture_output=True, text=True)
    if query.returncode:
        if 'HTTP 404' not in query.stderr:
            raise RuntimeError('Cannot reconcile GitHub release: ' + query.stderr)
        command = ['gh', 'release', 'create', tag, '--repo', repo, '--draft', '--target', manifest['sha'],
                   '--title', 'Electron2D ' + version, '--generate-notes']
        if '-' in version:
            command.append('--prerelease')
        subprocess.run(command, check=True)
        query = subprocess.run(['gh', 'api', endpoint], capture_output=True, text=True, check=True)
    release = json.loads(query.stdout)
    if release['draft'] and release['target_commitish'] != manifest['sha']:
        raise ValueError('GitHub draft targets a different source revision')
    # An existing tag may be annotated. Resolve it before uploading or publishing.
    ref = subprocess.run(['gh', 'api', f'repos/{repo}/git/ref/tags/{tag}'], capture_output=True, text=True)
    if ref.returncode == 0:
        obj = json.loads(ref.stdout)['object']
        for _ in range(8):
            if obj['type'] != 'tag':
                break
            obj = json.loads(subprocess.check_output(['gh', 'api', f'repos/{repo}/git/tags/{obj["sha"]}']))['object']
        if obj['type'] != 'commit' or obj['sha'] != manifest['sha']:
            raise ValueError('GitHub tag differs from qualified source')
    elif 'HTTP 404' not in ref.stderr or not release['draft']:
        raise RuntimeError('Cannot verify GitHub release tag')
    receipt = feed / 'public-verification.json'
    receipt.write_bytes((feed / 'public/verification.json').read_bytes())
    paths = [feed / p['file'] for p in manifest['packages']] + [feed / 'qualification.json', receipt]
    checksums = feed / 'SHA256SUMS'
    checksums.write_text(''.join(hashlib.sha256(p.read_bytes()).hexdigest() + '  ' + p.name + '\n' for p in paths))
    paths.append(checksums)
    assets = {a['name']: a for a in release['assets']}
    if set(assets) - {p.name for p in paths}:
        raise ValueError('Unexpected GitHub release assets')
    for path in paths:
        digest = 'sha256:' + hashlib.sha256(path.read_bytes()).hexdigest()
        if path.name in assets:
            if assets[path.name]['size'] != path.stat().st_size or assets[path.name].get('digest') != digest:
                raise ValueError('GitHub asset differs: ' + path.name)
        elif not release['draft']:
            raise ValueError('Published release is incomplete')
        else:
            subprocess.run(['gh', 'release', 'upload', tag, str(path), '--repo', repo], check=True)
    fresh = json.loads(subprocess.check_output(['gh', 'api', endpoint]))
    actual = {a['name']: (a['size'], a.get('digest')) for a in fresh['assets']}
    expected = {p.name: (p.stat().st_size, 'sha256:' + hashlib.sha256(p.read_bytes()).hexdigest()) for p in paths}
    if actual != expected:
        raise ValueError('GitHub upload reconciliation is incomplete')
    if fresh['draft']:
        subprocess.run(['gh', 'release', 'edit', tag, '--draft=false', '--repo', repo], check=True)


def public_feed(feed):
    manifest = json.loads((feed / 'qualification.json').read_text())
    output = feed / 'public'; output.mkdir(exist_ok=True)
    deadline = time.monotonic() + 1800
    pending = list(manifest['packages'])
    receipts = []
    while pending:
        for item in list(pending):
            identity, version = item['id'].lower(), item['version'].lower()
            url = f'https://api.nuget.org/v3-flatcontainer/{identity}/{version}/{identity}.{version}.nupkg'
            target = output / item['file']
            try:
                with urllib.request.urlopen(f'https://api.nuget.org/v3-flatcontainer/{identity}/index.json', timeout=60) as response:
                    if version not in json.load(response)['versions']:
                        continue
                with urllib.request.urlopen(url, timeout=60) as response:
                    target.write_bytes(response.read())
            except urllib.error.HTTPError as error:
                error.close()
                if error.code != 404:
                    raise
                continue
            compare_public(feed / item['file'], target)
            subprocess.run(['dotnet', 'nuget', 'verify', str(target), '--all'], check=True)
            receipts.append(dict(item, publicSHA256=hashlib.sha256(target.read_bytes()).hexdigest()))
            pending.remove(item)
            (output / 'verification.json').write_text(json.dumps(receipts, indent=2) + '\n')
        if pending:
            if time.monotonic() >= deadline:
                raise TimeoutError('Public NuGet availability remains incomplete')
            print(f'Waiting for {len(pending)} public packages', flush=True)
            time.sleep(20)
    restore_public(manifest)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['config', 'record', 'resolve', 'verify', 'publish', 'public', 'release'])
    parser.add_argument('target')
    args = parser.parse_args()
    if args.command == 'resolve':
        resolve(args.target)
    else:
        feed = Path(args.target).resolve()
        if args.command == 'config':
            feed_config(feed, feed / 'nuget.config')
        elif args.command == 'record':
            write_manifest(feed)
        elif args.command == 'verify':
            verify(feed, json.loads((Path(os.environ.get('RUNNER_TEMP', '.')) / 'qualification-run.json').read_text()))
        elif args.command == 'release':
            release_assets(feed)
        elif args.command == 'publish':
            publish(feed)
        else:
            public_feed(feed)
