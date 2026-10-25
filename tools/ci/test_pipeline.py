"""Regression tests for development scope, package identity and promotion boundaries."""
import copy
import hashlib
from contextlib import redirect_stdout
from io import BytesIO, StringIO
import json
import os
from pathlib import Path
import subprocess
import tempfile
import urllib.error
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET
from zipfile import ZipFile

import check_status
import packages
import plan


class Pipeline(unittest.TestCase):
    def test_native_source_fingerprint_is_independent_of_platform_path_order(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory)
            for name in ['src/Vendor/FAudio/B.h','src/Vendor/FAudio/a.h',
                         '.github/workflows/native.yml','tools/check_native_publish.py']:
                path=root/name;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(name.encode())
            with patch.object(packages.native,'ROOT',root):
                expected=packages.native.source_hash()
                with patch.object(Path,'__lt__',lambda left,right:str(left)>str(right)):
                    self.assertEqual(packages.native.source_hash(),expected)
                (root/'src/Vendor/FAudio/a.h').write_bytes(b'changed source')
                self.assertNotEqual(packages.native.source_hash(),expected)

    def test_scope_preserves_full_gate_for_code_unknown_paths_and_ready_prs(self):
        for name, event, paths, expected in [
            ('push', {}, ['README.ru.md', 'docs/classes/Node.md'], False),
            ('push', {}, ['tools/package-readme.md'], True),
            ('push', {}, ['licence/THIRD_PARTY_NOTICES.md'], True),
            ('push', {}, ['src/Node.cs'], True),
            ('push', {}, ['docs/coverage/data/electron2d.json'], True),
            ('push', {}, None, True), ('push', {}, [], True),
            ('pull_request', {'pull_request': {'draft': True}}, ['src/Node.cs'], False),
            ('pull_request', {'pull_request': {'draft': False}}, ['src/Node.cs'], True),
            ('workflow_dispatch', {'inputs': {'profile': 'fast'}}, None, False),
            ('workflow_dispatch', {'inputs': {'profile': 'full'}}, ['README.md'], True),
            ('workflow_dispatch', {'inputs': {'profile': 'clean'}}, None, True),
            ('schedule', {}, ['README.md'], True)]:
            with self.subTest(name=name, event=event, paths=paths):
                self.assertEqual(plan.plan(name, event, paths)[0], expected)
        with patch.object(plan.subprocess, 'check_output', side_effect=subprocess.CalledProcessError(1, 'git')):
            self.assertIsNone(plan.changed_paths('push', {'before': 'a'*40}))

    def test_development_never_turns_cancelled_failed_or_unplanned_work_green(self):
        for full in ('true', 'false'):
            results = {'plan': {'result': 'success', 'outputs': {'full': full}}, 'fast': {'result': 'success'},
                       'build_status': {'result': 'success' if full == 'true' else 'skipped'},
                       'tests_status': {'result': 'success' if full == 'true' else 'skipped'},
                       'qualify': {'result': 'success' if full == 'true' else 'skipped'}}
            check_status.check(results, 'development')
            for name in results:
                for result in ('failure', 'cancelled', None):
                    broken = copy.deepcopy(results); broken[name]['result'] = result
                    with self.subTest(full=full, name=name, result=result), self.assertRaises(RuntimeError):
                        check_status.check(broken, 'development')
            results['plan']['outputs']['full'] = 'unknown'
            with self.assertRaises(ValueError): check_status.check(results, 'development')

    def make_feed(self, root):
        version = packages.native.configuration()['version']
        for identity in ['Electron2D', *('Electron2D.' + p for p in packages.native.PLATFORMS)]:
            with ZipFile(root / f'{identity}.{version}.nupkg', 'w') as archive:
                archive.writestr(identity + '.nuspec', f'<package><metadata><id>{identity}</id><version>{version}</version></metadata></package>')
                if identity == 'Electron2D':
                    for tfm in ('net10.0', 'net10.0-android36.0', 'net10.0-ios26.0', 'net10.0-tvos26.0'):
                        for ext in ('dll', 'xml'): archive.writestr(f'lib/{tfm}/Electron2D.{ext}', tfm)
                    for ext in ('dll', 'xml'): archive.writestr(f'runtimes/browser-wasm/lib/net10.0/Electron2D.{ext}', 'browser')
                else:
                    manifest = {}
                    for rid in packages.native.PLATFORMS[identity.split('.')[1]]:
                        content = rid.encode()
                        manifest[rid] = {'minimumEngineVersion': version, 'sourceSHA256': 'source',
                                         'files': {name: hashlib.sha256(content).hexdigest() for name in packages.native.LIBRARIES[identity.split('.')[1]]}}
                        for name in manifest[rid]['files']:
                            archive.writestr(f'runtimes/{rid}/native/{name}', content)
                    archive.writestr('native-manifest.json', json.dumps(manifest))
        return version

    def rewrite(self, filename, transform):
        with ZipFile(filename) as archive: items = {n: archive.read(n) for n in archive.namelist()}
        transform(items)
        with ZipFile(filename, 'w') as archive:
            for name, data in items.items(): archive.writestr(name, data)

    def test_manifest_rejects_tampered_package_bytes_version_rids_and_frameworks(self):
        with tempfile.TemporaryDirectory(prefix='e2d packages ') as directory, \
             patch.object(packages.native, 'source_hash', return_value='source'), \
             patch.dict(os.environ, GITHUB_REPOSITORY='owner/engine', GITHUB_SHA='a'*40,
                        GITHUB_RUN_ID='17', GITHUB_RUN_ATTEMPT='2'):
            root = Path(directory); version = self.make_feed(root)
            receipt = packages.write_manifest(root)
            run = {'repository': {'full_name': 'owner/engine'}, 'head_sha': 'a'*40, 'id': 17, 'run_attempt': 2}
            self.assertEqual(len(packages.verify(root, run)['packages']), 8)
            self.assertEqual(len(receipt['packages']), 8)
            for key, value in [('head_sha', 'b'*40), ('id', 18), ('run_attempt', 3)]:
                changed = dict(run, **{key: value})
                with self.assertRaises(ValueError): packages.verify(root, changed)
            native = root / f'Electron2D.Linux.{version}.nupkg'
            original = native.read_bytes()
            self.rewrite(native, lambda items: items.update({'runtimes/linux-x64/native/libFAudio.so.0': b'changed'}))
            with self.assertRaises(ValueError): packages.inventory(root)
            native.write_bytes(original)
            def missing_rid(items):
                manifest = json.loads(items['native-manifest.json']); del manifest['linux-arm64']
                items['native-manifest.json'] = json.dumps(manifest).encode()
            self.rewrite(native, missing_rid)
            with self.assertRaises(ValueError): packages.inventory(root)
            native.write_bytes(original)
            master = root / f'Electron2D.{version}.nupkg'; original_master = master.read_bytes()
            self.rewrite(master, lambda items: items.pop('lib/net10.0-ios26.0/Electron2D.dll'))
            with self.assertRaises(ValueError): packages.inventory(root)
            master.write_bytes(original_master)
            master.rename(root / 'wrong-name.nupkg')
            with self.assertRaises(ValueError): packages.inventory(root)

    def test_public_comparison_allows_repository_signature_but_not_changed_content(self):
        with tempfile.TemporaryDirectory() as directory:
            a,b = Path(directory)/'candidate',Path(directory)/'public'
            with ZipFile(a,'w') as z: z.writestr('payload',b'good')
            with ZipFile(b,'w') as z:
                z.writestr('payload',b'good'); z.writestr('.signature.p7s',b'signature')
            packages.compare_public(a,b)
            self.rewrite(b, lambda items: items.update(payload=b'bad'))
            with self.assertRaises(ValueError): packages.compare_public(a,b)

    def test_trust_rejects_pr_fork_failure_wrong_workflow_and_branch(self):
        run = {'status':'completed','conclusion':'success','event':'push','head_branch':'main',
               'path':'.github/workflows/ci.yml','repository':{'full_name':'owner/engine'},
               'head_repository':{'full_name':'owner/engine'},'head_sha':'a'*40}
        packages.trusted_run(run,'owner/engine')
        for key,value in [('status','in_progress'),('conclusion','failure'),('event','pull_request'),
                          ('head_branch','feature'),('path','.github/workflows/other.yml'),
                          ('head_repository',{'full_name':'attacker/fork'}),('head_sha','unknown')]:
            with self.subTest(key=key),self.assertRaises(ValueError):
                packages.trusted_run(dict(run,**{key:value}),'owner/engine')

    def test_resolve_searches_all_artifact_pages_and_rejects_expired_or_ambiguous_matches(self):
        run={'id':17,'status':'completed','conclusion':'success','event':'push','head_branch':'main',
             'path':'.github/workflows/ci.yml','repository':{'full_name':'owner/engine'},
             'head_repository':{'full_name':'owner/engine'},'head_sha':'a'*40}
        artifact={'id':91,'name':'release-qualified','expired':False}
        with tempfile.TemporaryDirectory() as directory, patch.dict(os.environ,
             GITHUB_REPOSITORY='owner/engine',RUNNER_TEMP=directory):
            for matches,valid in [([artifact],True),([dict(artifact,expired=True)],False),([artifact,dict(artifact,id=92)],False)]:
                calls=[json.dumps(run).encode(),json.dumps([{'artifacts':[]},{'artifacts':matches}]).encode()]
                output=StringIO()
                with patch.object(packages.subprocess,'check_output',side_effect=calls) as requests,redirect_stdout(output):
                    if valid:
                        packages.resolve('17')
                        self.assertIn('artifact=91',output.getvalue())
                        self.assertEqual(json.loads((Path(directory)/'qualification-run.json').read_text())['id'],17)
                    else:
                        with self.assertRaises(ValueError):packages.resolve('17')
                    self.assertIn('--paginate',requests.call_args.args[0])
                    self.assertIn('--slurp',requests.call_args.args[0])

    def test_candidate_feed_mapping_escapes_paths_and_prevents_public_substitution(self):
        with tempfile.TemporaryDirectory(prefix='e2d & feed ') as directory:
            root = Path(directory); config = root/'nuget.config'
            packages.feed_config(root,config)
            tree = ET.parse(config)
            self.assertEqual(tree.find('./packageSources/add').get('value'),str(root.resolve()))
            self.assertEqual([p.get('pattern') for p in tree.findall('./packageSourceMapping/packageSource[@key="candidate"]/package')],['Electron2D','Electron2D.*'])
            self.assertIsNotNone(tree.find('./packageSources/clear'))

    def public_fixture(self, root):
        candidate = root / 'Electron2D.0.2.0-alpha.nupkg'
        with ZipFile(candidate, 'w') as archive: archive.writestr('payload', b'good')
        public = BytesIO()
        with ZipFile(public, 'w') as archive:
            archive.writestr('payload', b'good'); archive.writestr('.signature.p7s', b'signature')
        item = {'file': candidate.name, 'id': 'Electron2D', 'version': '0.2.0-alpha'}
        (root / 'qualification.json').write_text(json.dumps({'packages': [item], 'sha': 'a'*40}))
        return public.getvalue()

    def test_partial_publish_reconciles_existing_bytes_and_pushes_only_404(self):
        with tempfile.TemporaryDirectory() as directory, patch.dict(os.environ, NUGET_API_KEY='test-only'):
            root = Path(directory); content = self.public_fixture(root)
            with patch.object(packages.urllib.request, 'urlopen', return_value=BytesIO(content)), \
                 patch.object(packages.subprocess, 'run', return_value=SimpleNamespace(returncode=0)) as commands:
                packages.publish(root)
                self.assertEqual(commands.call_args.args[0][2], 'verify')
                self.assertFalse(any('push' in call.args[0] for call in commands.call_args_list))
            with patch.object(packages.urllib.request, 'urlopen', side_effect=urllib.error.HTTPError('url',404,'missing',{},None)), \
                 patch.object(packages.subprocess, 'run', return_value=SimpleNamespace(returncode=0)) as commands:
                packages.publish(root)
                self.assertEqual(commands.call_args.args[0][2], 'push')
            with patch.object(packages.urllib.request, 'urlopen', side_effect=urllib.error.HTTPError('url',404,'missing',{},None)), \
                 patch.object(packages.subprocess, 'run', return_value=SimpleNamespace(returncode=1)):
                with self.assertRaises(RuntimeError) as error: packages.publish(root)
                self.assertNotIn('test-only',str(error.exception))
            with patch.object(packages.urllib.request, 'urlopen', side_effect=urllib.error.HTTPError('url',403,'denied',{},None)), \
                 patch.object(packages.subprocess, 'run', return_value=SimpleNamespace(returncode=0)) as commands:
                with self.assertRaises(urllib.error.HTTPError): packages.publish(root)
                commands.assert_not_called()

    def test_public_index_timeout_cannot_claim_verification_or_restore(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); self.public_fixture(root)
            with patch.object(packages.urllib.request,'urlopen',return_value=BytesIO(b'{"versions":[]}')), \
                 patch.object(packages.time,'monotonic',side_effect=[0,1801]), \
                 patch.object(packages,'restore_public') as restore:
                with self.assertRaises(TimeoutError): packages.public_feed(root)
                restore.assert_not_called()
            content = self.public_fixture(root)
            with patch.object(packages.urllib.request,'urlopen',side_effect=[BytesIO(b'{"versions":["0.2.0-alpha"]}'),BytesIO(content)]), \
                 patch.object(packages.subprocess,'run') as commands, patch.object(packages,'restore_public') as restore:
                packages.public_feed(root)
                restore.assert_called_once()
                self.assertEqual(commands.call_args.args[0][2],'verify')
                self.assertEqual(len(json.loads((root/'public/verification.json').read_text())),1)

    def test_github_assets_resume_without_replacing_or_republishing_payloads(self):
        with tempfile.TemporaryDirectory() as directory, patch.dict(os.environ,GITHUB_REPOSITORY='owner/engine'):
            root=Path(directory); self.public_fixture(root)
            (root/'public').mkdir(); (root/'public/verification.json').write_text('[]')
            release={'draft':True,'target_commitish':'a'*40,'assets':[]}
            published=[]
            def command(args,**kwargs):
                if args[1]=='api' and '/git/ref/' in args[2]:
                    return subprocess.CompletedProcess(args,0,json.dumps({'object':{'type':'commit','sha':'a'*40}}),'')
                if args[1]=='api':
                    return subprocess.CompletedProcess(args,0,json.dumps(release),'')
                if args[1:3]==['release','upload']:
                    path=Path(args[4])
                    release['assets'].append({'name':path.name,'size':path.stat().st_size,
                        'digest':'sha256:'+hashlib.sha256(path.read_bytes()).hexdigest()})
                if args[1:3]==['release','edit']:
                    published.append(args); release['draft']=False
                return subprocess.CompletedProcess(args,0,'','')
            with patch.object(packages.subprocess,'run',side_effect=command), \
                 patch.object(packages.subprocess,'check_output',side_effect=lambda *a,**k:json.dumps(release).encode()):
                packages.release_assets(root)
                self.assertEqual(len(release['assets']),4)
                packages.release_assets(root)
                self.assertEqual(len(published),1)
                release['assets'][0]['digest']='sha256:wrong'
                with self.assertRaises(ValueError): packages.release_assets(root)
                self.assertEqual(len(published),1)

    def test_package_mode_removes_source_reference_through_real_msbuild_evaluation(self):
        root = Path(__file__).resolve().parents[2]
        for filename in ['tests/Electron2D.Tests/Electron2D.Tests.csproj',
                         'tests/PhysicsBackendExtension.Consumer/PhysicsBackendExtension.Consumer.csproj',
                         'tests/Electron2D.PortabilityTests/Electron2D.PortabilityTests.csproj']:
            output = subprocess.check_output(['dotnet','msbuild',str(root/filename),'-nologo',
                '-p:Electron2DUsePackage=true','-getItem:ProjectReference,PackageReference'],text=True)
            items = json.loads(output)['Items']
            self.assertFalse(any(Path(p['Identity']).name=='Electron2D.csproj' for p in items['ProjectReference']))
            self.assertEqual(sum(p['Identity']=='Electron2D' for p in items['PackageReference']),1)


if __name__ == '__main__':
    unittest.main()
