"""Regression checks for CI matrix drift and target artifact rejection."""

from contextlib import redirect_stdout
from io import BytesIO, StringIO
import hashlib
import json
import os
from pathlib import Path
import re
import struct
import subprocess
import shutil
import tempfile
import threading
from types import SimpleNamespace
import unittest
import xml.etree.ElementTree as ET
from unittest.mock import Mock, patch
from zipfile import ZipFile

import check_rid
import check_status
import rids
import run_android
import run_apple
import run_browser
import run_macos_runtime
import select_lavapipe


class Checks(unittest.TestCase):
    def test_macos_runtime_samples_its_silent_child_and_keeps_exit_and_marker_gates(self):
        with tempfile.TemporaryDirectory(prefix='e2d macOS sample ') as directory:
            root = Path(directory)
            fixture = root / 'fixture'
            output = root / 'full-runtime.log'
            for code, marker, expected in ((0, 'Electron2D checks passed.', 0),
                                           (7, 'Electron2D checks passed.', 7), (0, 'Missing result', 1)):
                with self.subTest(code=code, marker=marker):
                    fixture.write_text('#!/usr/bin/env python3\nimport os,time\nos.write(1,b"prefix\\xff\\n")\ntime.sleep(.15)\nos.write(1,' + repr((marker + '\n').encode()) + ')\nraise SystemExit(' + str(code) + ')\n')
                    fixture.chmod(0o755)
                    def sample(command, **kwargs):
                        self.assertEqual(command[0], '/usr/bin/sample')
                        os.kill(int(command[1]), 0)
                        self.assertEqual(command[2:4], ['1', '-file'])
                        Path(command[4]).write_text('Owned process sample\n')
                        return subprocess.CompletedProcess(command, 0, b'')
                    console = BytesIO()
                    with patch.object(run_macos_runtime, 'IDLE_SECONDS', .03), patch.object(run_macos_runtime, 'POLL_SECONDS', .005), \
                         patch.object(run_macos_runtime.subprocess, 'run', side_effect=sample) as sampled, \
                         patch.object(run_macos_runtime.sys, 'stdout', SimpleNamespace(buffer=console)):
                        self.assertEqual(run_macos_runtime.run(fixture, output), expected)
                    self.assertGreater(sampled.call_count, 0)
                    self.assertEqual(output.read_bytes(), b'prefix\xff\n' + (marker + '\n').encode())
                    self.assertIn(f'Runtime exit: {code}\n'.encode(), console.getvalue())
                    self.assertFalse(any(t.name == 'macos-runtime-sampler' for t in threading.enumerate()))
            with patch.object(run_macos_runtime, 'IDLE_SECONDS', .03), patch.object(run_macos_runtime, 'POLL_SECONDS', .005), \
                 patch.object(run_macos_runtime.subprocess, 'run', side_effect=FileNotFoundError('sample unavailable')), \
                 patch.object(run_macos_runtime.sys, 'stdout', SimpleNamespace(buffer=BytesIO())):
                self.assertEqual(run_macos_runtime.run(fixture, output), 1)
            self.assertIn('Sample unavailable:', (root / 'full-runtime-stall-1.log').read_text())
        workflow = (rids.ROOT / '.github/workflows/macos-runtime-verification.yml').read_text()
        self.assertIn('python3 -B tools/ci/run_macos_runtime.py bin/diagnostics/runner/Electron2D.Tests bin/diagnostics/full-runtime.log', workflow)
        self.assertIn('timeout-minutes: 90', workflow)

    def test_windows_motion_probe_preserves_default_shader_and_reports_device_state(self):
        workflow = (rids.ROOT / '.github/workflows/windows-gpu-diagnostics.yml').read_text()
        step = workflow.partition('      - name: Record source and trace cold pipeline preparation')[2].split('\n      - ', 1)[0]
        inline = step.partition("          python - <<'PY'\n")[2].split('\n          PY', 1)[0]
        code = '\n'.join(line[10:] for line in inline.splitlines())
        names = ('src/Servers/Physics/GPUPhysicsDevice.cs', 'src/Servers/Rendering/ShaderCompiler.cs',
                 'src/Servers/Physics/GPUPhysicsBodyStore.MotionQueries.cs',
                 'src/Servers/Physics/Shaders/PhysicsResidentMotionQueries.comp.glsl', 'tools/shaders/build_runtime.py')
        originals = {name: (rids.ROOT / name).read_bytes().replace(b'\r\n', b'\n') for name in names}
        previous = Path.cwd()
        try:
            with tempfile.TemporaryDirectory(prefix='e2d motion probe ') as directory:
                os.chdir(directory)
                for compiler, optimization, seeded in ((c, o, s) for c in ('dxil', 'dxbc') for o in ('false', 'true') for s in ('false', 'true')):
                    for newline in (b'\n', b'\r\n'):
                        with self.subTest(compiler=compiler, optimization=optimization, seeded=seeded, newline=newline):
                            for name, original in originals.items():
                                path = Path(name); path.parent.mkdir(parents=True, exist_ok=True)
                                path.write_bytes(original.replace(b'\n', newline))
                            with patch.dict(os.environ, RECOVERY_COMPILER=compiler, MOTION_SPIRV_OPTIMIZATION=optimization, MOTION_SEED_BOUNDS=seeded, GITHUB_SHA='self-check'), redirect_stdout(StringIO()):
                                exec(compile(code, 'windows-motion-workflow', 'exec'), {})
                            query = Path(names[2]).read_text()
                            shader = Path(names[3]).read_text()
                            if compiler == 'dxil' and optimization == 'false':
                                self.assertEqual(Path(names[1]).read_text(), originals[names[1]].decode())
                                self.assertEqual(query, originals[names[2]].decode())
                                if seeded == 'false': self.assertEqual(shader, originals[names[3]].decode())
                            else:
                                self.assertEqual(query.count('Motion stage receipt:'), 1)
                                self.assertIn('result.objectID.zw=outputWords[scratchStart].xy;', shader)
                                self.assertIn('result.owner.w=state.w|(motionProbe<<8)|(blocked?1048576u:0u)|(rested?2097152u:0u)|(outputWords[scratchStart].w<<22);', shader)
                                self.assertIn('outputWords[stateAt+2u].w=motionProbe;', shader)
                                self.assertIn('outputWords[stateAt+3u]=uvec4(0u);', shader)
                                self.assertIn('atomicOr(outputWords[gl_GlobalInvocationID.x*8u+3u].z,(safe==0?4u:0u)|(blocked?8u:0u));', shader)
                                self.assertIn('((outputWords[stateAt+3u].z|(safe==0?16u:0u))<<6)', shader)
                                self.assertIn('outputWords[gl_GlobalInvocationID.x*8u+2u].x=floatBitsToUint(safe);', shader)
                                self.assertIn('outputWords[gl_GlobalInvocationID.x*8u+2u].y=floatBitsToUint(safe);', shader)
                                bounds = shader.index('vec4 bounds=queryBounds(shapeA,ga,recovering||resting);int at=1;')
                                self.assertLess(shader.index('atomicOr(outputWords[gl_GlobalInvocationID.x*8u+3u].w,128u);'), bounds)
                                self.assertGreater(shader.index('atomicOr(outputWords[gl_GlobalInvocationID.x*8u+3u].w,256u);'), bounds)
                                if compiler == 'dxbc':
                                    self.assertIn('Marshal.Copy(compiled, actualBytecode, 0, actualBytecode.Length);', Path(names[1]).read_text())
                                else:
                                    self.assertIn('Marshal.Copy(compiled, diagnosticCode, 0, diagnosticCode.Length);', Path(names[1]).read_text())
                                    self.assertIn('finally { SDL.Free(diagnosticHLSL); }', Path(names[1]).read_text())
                            receipt = json.loads(Path('bin/diagnostics/compiler-source.json').read_text())
                            self.assertEqual(receipt['motionSPIRVOptimization'], optimization == 'true')
                            self.assertEqual(receipt['motionSeededBounds'], seeded == 'true')
                            self.assertEqual(receipt['motionPersistentSweep'], seeded == 'true')
                            self.assertEqual(receipt['motionPersistentRest'], seeded == 'true')
                            if seeded == 'true':
                                self.assertIn('for(uint i=1u;i<g.data.y;i++)', shader)
                                self.assertIn('rested=outputWords[stateAt+2u].z!=0u;', shader)
                                self.assertIn('if(contact.depth>uintBitsToFloat(outputWords[restAt+5u].z))', shader)
                                self.assertEqual(shader.count('uvec4 savedSweep=outputWords[gl_GlobalInvocationID.x*8u+7u];'), 3)
                                self.assertEqual(shader.count('outputWords[gl_GlobalInvocationID.x*8u+7u]=uvec4(floatBitsToUint(vec2(safe,unsafeFraction)),motionShape,blocked?1u:0u);'), 2)
                            if optimization == 'false':
                                self.assertEqual(Path(names[4]).read_text(), originals[names[4]].decode())
                            else:
                                self.assertIn("or source.name == 'PhysicsResidentMotionQueries.comp.glsl'", Path(names[4]).read_text())
                            self.assertEqual(receipt['actualSHA256'], hashlib.sha256(Path(names[0]).read_bytes()).hexdigest())
                            self.assertEqual(receipt['headSHA'], 'self-check')
        finally:
            os.chdir(previous)
        self.assertLess(workflow.index('name: windows-gpu-fixture'), workflow.index('name: Capture native exception context'))
        self.assertNotIn('ELECTRON2D_TEST_', workflow)
        self.assertIn('Run the complete untraced runtime suite', workflow)
        self.assertIn("-ccontains 'Electron2D checks passed.'", workflow)
        self.assertIn('timeout-minutes: 90', workflow)
        self.assertIn('python -m pip install --require-hashes -r tools/native/requirements-windows.txt', workflow)
        self.assertIn('choco install ffmpeg --yes --no-progress', workflow)

    def test_headless_linux_selects_one_installed_lavapipe_driver(self):
        for workflow in ('rid.yml', 'linux-tls-diagnostics.yml', 'linux-gpu-diagnostics.yml'):
            self.assertIn('python -B tools/ci/select_lavapipe.py', (rids.ROOT / '.github/workflows' / workflow).read_text())
        with tempfile.TemporaryDirectory(prefix='e2d Vulkan driver ') as directory:
            root = Path(directory)
            with self.assertRaises(ValueError): select_lavapipe.environment(root)
            for name in ('lvp_icd.json', 'lvp_icd.aarch64.json'):
                manifest = root / name
                manifest.write_text(json.dumps({'ICD': {'library_path': 'libvulkan_lvp.so'}}))
                expected = str(manifest.resolve())
                self.assertEqual(select_lavapipe.environment(root), {'VK_DRIVER_FILES': expected, 'VK_ICD_FILENAMES': expected})
                (root / 'lvp_icd.extra.json').write_text(manifest.read_text())
                with self.assertRaises(ValueError): select_lavapipe.environment(root)
                (root / 'lvp_icd.extra.json').unlink()
                for library in ('libvulkan_gfxstream.so', None):
                    manifest.write_text(json.dumps({'ICD': {'library_path': library}}))
                    with self.assertRaises(ValueError): select_lavapipe.environment(root)
                manifest.write_text('{broken')
                with self.assertRaises(ValueError): select_lavapipe.environment(root)
                manifest.unlink()
            manifest = root / 'lvp_icd.json'
            for configuration in ([], {'ICD': None}):
                manifest.write_text(json.dumps(configuration))
                with self.assertRaises(ValueError): select_lavapipe.environment(root)
            with patch.object(Path, 'resolve', return_value=root / 'lvp_icd\nextra.json'):
                with self.assertRaises(ValueError): select_lavapipe.environment(root)

    def test_linux_gpu_diagnostic_preserves_the_baseline_and_shader_identity(self):
        workflow = (rids.ROOT / '.github/workflows/linux-gpu-diagnostics.yml').read_text()
        row = next(row for row in rids.matrix() if row['rid'] == 'linux-x64')
        self.assertIn('runs-on: ' + row['runner'], workflow)
        self.assertIn('key: shader-tools-' + row['runner'] + '-', workflow)
        self.assertLess(workflow.index('Run the unchanged compound fixture'), workflow.index('Trace candidate and impact boundaries'))
        self.assertIn('cmp obj/Release/net10.0/linux-x64/shaders/manifest.json bin/diagnostics/baseline-shaders.json', workflow)
        self.assertIn("if: steps.baseline.outcome == 'failure'\n        run: exit 1", workflow)
        step = workflow.partition('      - name: Trace candidate and impact boundaries')[2].split('\n      - ', 1)[0]
        inline = step.partition("          python - <<'PY'\n")[2].split('\n          PY', 1)[0]
        code = '\n'.join(line[10:] for line in inline.splitlines())
        restore_step = workflow.partition('      - name: Prepare an untraced Lavapipe control')[2].split('\n      - ', 1)[0]
        inline_restore = restore_step.partition("          python - <<'PY'\n")[2].split('\n          PY', 1)[0]
        restore = '\n'.join(line[10:] for line in inline_restore.splitlines())
        path = Path('src/Servers/Physics/GPUPhysicsBodyStore.Continuous.cs')
        original = (rids.ROOT / path).read_bytes().replace(b'\r\n', b'\n')
        previous = Path.cwd()
        try:
            with tempfile.TemporaryDirectory(prefix='e2d GPU trace ') as directory, patch.dict(os.environ, GITHUB_SHA='self-check'):
                os.chdir(directory)
                path.parent.mkdir(parents=True)
                Path('bin/diagnostics').mkdir(parents=True)
                for newline in (b'\n', b'\r\n'):
                    with self.subTest(newline=newline):
                        before = original.replace(b'\n', newline)
                        path.write_bytes(before)
                        exec(compile(code, 'linux-gpu-workflow', 'exec'), {})
                        after = path.read_bytes()
                        self.assertEqual(after.count(b'CCD candidates delta='), 1)
                        self.assertEqual(after.count(b'CCD result status='), 1)
                        self.assertEqual(b'\r\n' in after, newline == b'\r\n')
                        receipt = json.loads(Path('bin/diagnostics/trace-source.json').read_text())
                        self.assertEqual(receipt['originalSHA256'], hashlib.sha256(before).hexdigest())
                        self.assertEqual(receipt['actualSHA256'], hashlib.sha256(after).hexdigest())
                        exec(compile(restore, 'linux-gpu-restore', 'exec'), {})
                        self.assertEqual(path.read_bytes(), before)
                        Path('bin/diagnostics/continuous-original.cs').write_bytes(b'changed')
                        path.write_bytes(after)
                        with self.assertRaises(AssertionError): exec(compile(restore, 'linux-gpu-restore', 'exec'), {})
                        self.assertEqual(path.read_bytes(), after)
                missing = original.replace(b'result = ((float*)mapped)[1];', b'/* missing marker */')
                path.write_bytes(missing)
                with self.assertRaises(AssertionError): exec(compile(code, 'linux-gpu-workflow', 'exec'), {})
                self.assertEqual(path.read_bytes(), missing)
        finally:
            os.chdir(previous)

    def test_tls_diagnostic_uses_the_registered_arm64_host_and_prerequisites(self):
        workflow = (rids.ROOT / '.github/workflows/linux-tls-diagnostics.yml').read_text()
        row = next(row for row in rids.matrix() if row['rid'] == 'linux-arm64')
        self.assertIn('runs-on: ' + row['runner'], workflow)
        self.assertIn('key: shader-tools-' + row['runner'] + '-', workflow)
        standard = (rids.ROOT / '.github/workflows/rid.yml').read_text().partition('      - name: Install Linux native prerequisites\n')[2].split('\n      - ', 1)[0]
        diagnostic = workflow.partition('      - name: Install runtime prerequisites\n')[2].split('\n      - ', 1)[0]
        required = set(re.search(r'sudo apt-get install -y ([^\n]+)', standard)[1].split())
        actual = set(re.search(r'sudo apt-get install -y ([^\n]+)', diagnostic)[1].split())
        self.assertLessEqual(required, actual)

    def test_linux_driver_verification_runs_the_complete_untraced_command(self):
        workflow = (rids.ROOT / '.github/workflows/linux-tls-diagnostics.yml').read_text()
        step = workflow.partition('      - name: Run the complete untraced suite with Lavapipe\n')[2].split('\n      - ', 1)[0]
        command = '\n'.join(line[10:] for line in step.partition('        run: |\n')[2].splitlines())
        self.assertTrue(command)
        with tempfile.TemporaryDirectory(prefix='e2d untraced Linux ') as directory:
            root = Path(directory)
            runner = root / 'bin/diagnostics/runner/Electron2D.Tests'
            runner.parent.mkdir(parents=True)
            for code, marker in ((0, 'Electron2D checks passed.'), (7, 'Electron2D checks passed.'), (0, 'Missing result')):
                runner.write_text('#!/bin/sh\n[ -z "${LD_PRELOAD+x}${E2D_TLS_TRACE_FILE+x}" ] || exit 92\nprintf "%s\\n" "' + marker + '"\nexit ' + str(code) + '\n')
                runner.chmod(0o755)
                result = subprocess.run(['bash', '-e', '-o', 'pipefail', '-c', 'export LD_PRELOAD=must-be-cleared E2D_TLS_TRACE_FILE=must-be-cleared\n' + command], cwd=root, capture_output=True, text=True)
                self.assertEqual(result.returncode == 0, code == 0 and marker == 'Electron2D checks passed.')

    def test_macos_x64_keeps_its_target_on_a_capable_gpu_host(self):
        workflow = (rids.ROOT / ".github/workflows/ci.yml").read_text()
        self.assertIn("runner: ${{ matrix.runner }}", workflow.partition("  tests:\n")[0])
        self.assertIn("runner: ${{ matrix.rid == 'osx-x64' && 'macos-26' || matrix.runner }}", workflow.partition("  tests:\n")[2])
        row = next(row for row in rids.matrix() if row["rid"] == "osx-x64")
        self.assertEqual((row["runner"], row["suite"]), ("macos-26-intel", "full"))
        target = (rids.ROOT / ".github/workflows/rid.yml").read_text()
        step = target.partition("      - name: Prepare x64 execution on Apple Silicon\n")[2].split("\n      - ", 1)[0]
        self.assertIn("inputs.tests && inputs.rid == 'osx-x64' && runner.arch == 'ARM64'", step)
        command = "\n".join(line[10:] for line in step.partition("        run: |\n")[2].splitlines())
        self.assertTrue(command)
        self.assertIn('lipo -archs "bin/ci-tests/$CI_RID/Electron2D.Tests" | grep -Fx x86_64', target)
        with tempfile.TemporaryDirectory(prefix="e2d Rosetta contract ") as directory:
            root = Path(directory)
            (root / "arch").write_text('#!/bin/sh\n[ "$1" = -x86_64 ] || exit 92\nif [ "$2" = /usr/bin/true ]; then [ "$READY" = 1 ]; else printf "%s\\n" "$REPORTED_ARCH"; fi\n')
            (root / "sudo").write_text('#!/bin/sh\nprintf "%s\\n" "$*" > "$INSTALL_LOG"\nexit "$INSTALL_EXIT"\n')
            for name in ("arch", "sudo"): (root / name).chmod(0o755)
            for ready, install_exit, reported, success in (("1", "0", "x86_64", True), ("0", "0", "x86_64", True),
                                                             ("0", "7", "x86_64", False), ("1", "0", "arm64", False)):
                with self.subTest(ready=ready, install_exit=install_exit, reported=reported):
                    log = root / "install.log"
                    log.unlink(missing_ok=True)
                    environment = os.environ | {"PATH": str(root) + os.pathsep + os.environ["PATH"], "READY": ready,
                                                "INSTALL_EXIT": install_exit, "REPORTED_ARCH": reported, "INSTALL_LOG": str(log)}
                    result = subprocess.run(["bash", "-e", "-o", "pipefail", "-c", command], env=environment)
                    self.assertEqual(result.returncode == 0, success)
                    self.assertEqual(log.exists(), ready == "0")
                    if log.exists(): self.assertEqual(log.read_text(), "softwareupdate --install-rosetta --agree-to-license\n")

    def test_linux_full_suite_selects_offscreen_video_without_changing_other_hosts(self):
        workflow = (rids.ROOT / ".github/workflows/rid.yml").read_text()
        prerequisites = workflow.partition("      - name: Install Linux native prerequisites\n")[2].split("\n      - ", 1)[0]
        packages = re.search(r"sudo apt-get install -y ([^\n]+)", prerequisites)[1].split()
        self.assertTrue({"libvulkan1", "mesa-vulkan-drivers"} <= set(packages))
        with tempfile.TemporaryDirectory(prefix="e2d headless contract ") as directory:
            root = Path(directory)
            for rid in ("linux-x64", "linux-arm64", "win-x64", "osx-x64"):
                with self.subTest(rid=rid):
                    output = root / "bin/ci" / rid
                    output.mkdir(parents=True)
                    executable = root / "bin/ci-tests" / rid / ("Electron2D.Tests.exe" if rid.startswith("win-") else "Electron2D.Tests")
                    executable.parent.mkdir(parents=True)
                    expected = "offscreen:offscreen" if rid.startswith("linux-") else "owner:legacy"
                    for name, log, marker, selector in (
                            ("Run GPU compound control in a fresh process", "compound-runtime.log",
                             "Compound GPU: transformed slots, gaps and disabled indices passed.", "gpu"),
                            ("Run full headless runtime suite", "full-runtime.log", "Electron2D checks passed.", "")):
                        step = workflow.partition("      - name: " + name + "\n")[2].split("\n      - ", 1)[0]
                        command = "\n".join(line[10:] for line in step.partition("        run: |\n")[2].splitlines())
                        self.assertTrue(command)
                        if selector: self.assertIn("ELECTRON2D_TEST_COMPOUND_PHYSICS: gpu", step)
                        executable.write_text('#!/bin/sh\n[ "$SDL_VIDEO_DRIVER:$SDL_VIDEODRIVER" = "' + expected + '" ] || exit 97\n'
                                              '[ "$ELECTRON2D_TEST_COMPOUND_PHYSICS" = "' + selector + '" ] || exit 98\nprintf "' + marker + '\\n"\n')
                        executable.chmod(0o755)
                        environment = os.environ | {"CI_RID": rid, "GITHUB_WORKSPACE": directory,
                                                    "SDL_VIDEO_DRIVER": "owner", "SDL_VIDEODRIVER": "legacy",
                                                    "ELECTRON2D_TEST_COMPOUND_PHYSICS": selector}
                        subprocess.run(["bash", "-e", "-o", "pipefail", "-c", command], cwd=root, env=environment,
                                       check=True, stdout=subprocess.DEVNULL)
                        self.assertEqual((output / log).read_text(), marker + "\n")

    def test_windows_host_tools_preserve_managed_rid_platform_inference(self):
        workflow = (rids.ROOT / ".github/workflows/rid.yml").read_text()
        marker = "      - name: Restore managed SDK platform inference\n"
        step = workflow.partition(marker)[2].split("\n      - ", 1)[0]
        command = re.search(r"(?m)^        run: (.+)$", step)
        self.assertIsNotNone(command, "Clear the host MSVC platform before managed RID commands")
        self.assertGreater(workflow.index(marker), workflow.index("uses: ilammy/msvc-dev-cmd@v1"))
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "environment"
            environment = os.environ | {"Platform": "x64", "GITHUB_ENV": str(output)}
            subprocess.run(["bash", "-c", command[1]], env=environment, check=True)
            environment.update(line.split("=", 1) for line in output.read_text().splitlines())
            for rid in (row["rid"] for row in rids.matrix() if row["platform"] == "Windows"):
                with self.subTest(rid=rid):
                    profile = json.loads(subprocess.check_output(
                        ["dotnet", "msbuild", "Electron2D.csproj", "-nologo", "-p:RuntimeIdentifier=" + rid,
                         "-getProperty:RuntimeIdentifier,PlatformTarget"], cwd=rids.ROOT, env=environment))
                    self.assertEqual(profile["Properties"]["RuntimeIdentifier"], rid)
                    self.assertEqual(profile["Properties"]["PlatformTarget"].lower(), rid.removeprefix("win-"))

    def test_target_font_consumers_embed_the_shared_fixture_inventory(self):
        root = rids.ROOT
        required = set(re.findall(r'Read\(typeof\(FontTestFixtures\)\.Assembly,\s*"([^"]+)"\)',
                                  (root / "tests/Electron2D.Tests/FontTestFixtures.cs").read_text()))
        self.assertTrue(required)
        for target in ("Browser", "Android", "Apple"):
            project = root / f"tests/Electron2D.{target}Tests/Electron2D.{target}Tests.csproj"
            embedded = set()
            for item in ET.parse(project).getroot().findall(".//EmbeddedResource"):
                for pattern in item.get("Include", "").split(";"):
                    if pattern:
                        for path in project.parent.glob(pattern):
                            embedded.add(item.get("LogicalName", "").replace("%(Filename)", path.stem).replace("%(Extension)", path.suffix))
            with self.subTest(target=target):
                self.assertFalse(required - embedded, f"Missing font fixtures: {sorted(required - embedded)}")

    def test_repository_paths_can_be_checked_out_on_windows(self):
        root = Path(__file__).resolve().parents[2]
        paths = subprocess.check_output(["git", "ls-files", "-z", "--cached", "--others", "--exclude-standard"], cwd=root).decode().split("\0")
        invalid = [path for path in paths if path and (root / path).exists() and any(char in path for char in '<>:"\\|?*')]
        self.assertEqual(invalid, [], "Repository filenames must be valid on Windows")

    def test_android_sdl_manifests_keep_exported_launcher_and_assets_contract(self):
        root = Path(__file__).resolve().parents[2]
        android = "{http://schemas.android.com/apk/res/android}"
        for manifest, name in (("tests/Electron2D.AndroidTests/Properties/AndroidManifest.xml", "org.electron2d.tests.MainActivity"),
                               ("tests/Electron2D.AndroidProbe/AndroidManifest.xml", "org.electron2d.probe.MainActivity")):
            with self.subTest(manifest=manifest):
                activity = next(item for item in ET.parse(root / manifest).getroot().findall("application/activity")
                                if item.get(android + "name") == name)
                self.assertEqual(activity.get(android + "exported"), "true")
                self.assertEqual(set(activity.get(android + "configChanges", "").split("|")),
                                 {"keyboard", "keyboardHidden", "navigation", "orientation", "screenSize", "assetsPaths"})
                self.assertTrue(any("android.intent.action.MAIN" in {item.get(android + "name") for item in intent.findall("action")} and
                                    "android.intent.category.LAUNCHER" in {item.get(android + "name") for item in intent.findall("category")}
                                    for intent in activity.findall("intent-filter")))

    def test_runtime_svg_bytes_survive_crlf_checkout(self):
        root = Path(__file__).resolve().parents[2]
        paths = sorted((root / "src/Scene/Theme/Icons").glob("*.svg"))
        self.assertTrue(paths)
        with tempfile.TemporaryDirectory() as directory:
            subprocess.run(["git", "-c", "core.autocrlf=true", "checkout-index", "--prefix=" + directory + "/",
                            *[path.relative_to(root).as_posix() for path in paths]], cwd=root, check=True)
            for path in paths:
                self.assertEqual((Path(directory) / path.relative_to(root)).read_bytes(), path.read_bytes(), str(path))

    def test_native_publish_layout_does_not_relocate_managed_dependencies(self):
        with tempfile.TemporaryDirectory() as directory:
            project = ET.Element("Project")
            group = ET.SubElement(project, "ItemGroup")
            for package in ("Electron2D.Android", "SDL3-CS.Android", "HarfBuzzSharp.NativeAssets.Win32", "MonoGame.Library.FreeType"):
                for asset in ("native", "runtime"):
                    item = ET.SubElement(group, "_ResolvedCopyLocalPublishAssets", Include=f"{package}/{asset}.fixture")
                    ET.SubElement(item, "NuGetPackageId").text = package
                    ET.SubElement(item, "AssetType").text = asset
            ET.SubElement(project, "Import", Project=str(rids.ROOT / "tools/native/Electron2D.targets"))
            fixture = Path(directory) / "fixture.proj"
            ET.ElementTree(project).write(fixture, encoding="unicode")
            result = subprocess.run(["dotnet", "msbuild", str(fixture), "-nologo",
                                     "-p:RuntimeIdentifier=android-x64", "-t:Electron2DNativePublishLayout",
                                     "-getItem:_ResolvedCopyLocalPublishAssets"],
                                    check=True, capture_output=True, text=True)
            for item in json.loads(result.stdout)["Items"]["_ResolvedCopyLocalPublishAssets"]:
                with self.subTest(package=item["NuGetPackageId"], asset=item["AssetType"]):
                    expected = "runtimes/android-x64/native/" if item["AssetType"] == "native" else None
                    self.assertEqual(item.get("DestinationSubDirectory"), expected)

    def test_default_desktop_layout_keeps_universal_macos_native_assets(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            project = ET.Element("Project")
            properties = ET.SubElement(project, "PropertyGroup")
            ET.SubElement(properties, "TargetFramework").text = "net10.0"
            for kind in ("RuntimeTargetsCopyLocalItems", "_ResolvedCopyLocalPublishAssets"):
                group = ET.SubElement(project, "ItemGroup")
                for rid in ("osx", "osx-x64", "osx-arm64", "linux-x64", "win-x64"):
                    item = ET.SubElement(group, kind, Include=f"runtimes/{rid}/native/{rid}.fixture")
                    for name, value in (("RuntimeIdentifier", rid), ("AssetType", "native"),
                                        ("DestinationSubDirectory", f"runtimes/{rid}/native/")):
                        ET.SubElement(item, name).text = value
            ET.SubElement(project, "Import", Project=str(rids.ROOT / "tools/native/Electron2D.targets"))
            fixture = root / "fixture.proj"
            ET.ElementTree(project).write(fixture, encoding="unicode")
            for host in ("osx-x64", "osx-arm64", "linux-x64", "win-x64"):
                with self.subTest(host=host):
                    result = subprocess.run(["dotnet", "msbuild", str(fixture), "-nologo",
                                             "-p:NETCoreSdkRuntimeIdentifier=" + host,
                                             "-t:Electron2DNativeBuildLayout,Electron2DNativePublishLayout",
                                             "-getItem:RuntimeTargetsCopyLocalItems,_ResolvedCopyLocalPublishAssets"],
                                            check=True, capture_output=True, text=True)
                    items = json.loads(result.stdout)["Items"]
                    selected = items["RuntimeTargetsCopyLocalItems"]
                    expected = {host, "osx"} if host.startswith("osx-") else {host}
                    self.assertEqual({item["RuntimeIdentifier"] for item in selected}, expected)
                    self.assertEqual({item["DestinationSubDirectory"] for item in selected}, {f"runtimes/{host}/native/"})
                    universal = next(item for item in items["_ResolvedCopyLocalPublishAssets"] if item["RuntimeIdentifier"] == "osx")
                    self.assertEqual(universal["DestinationSubDirectory"], f"runtimes/{host}/native/" if host.startswith("osx-") else "runtimes/osx/native/")

    def test_browser_static_references_do_not_leak_into_other_rids(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            targets = root / "buildTransitive/Electron2D.Web.targets"
            targets.parent.mkdir()
            shutil.copyfile(rids.ROOT / "tools/native/Electron2D.targets", targets)
            native = root / "runtimes/browser-wasm/native"
            native.mkdir(parents=True)
            for name in check_rid.native_package.LIBRARIES["Web"]:
                (native / name).write_bytes(b"!<arch>\n")
            (root / "licence").mkdir()
            (root / "licence/fixture.txt").write_text("fixture")
            project = ET.Element("Project")
            ET.SubElement(project, "Import", Project=str(targets))
            fixture = root / "fixture.proj"
            ET.ElementTree(project).write(fixture, encoding="unicode")
            for rid in ("browser-wasm", "linux-x64", "ios-arm64"):
                with self.subTest(rid=rid):
                    result = subprocess.run(["dotnet", "msbuild", str(fixture), "-nologo",
                                             "-p:RuntimeIdentifier=" + rid, "-getItem:NativeFileReference,Content"],
                                            check=True, capture_output=True, text=True)
                    references = json.loads(result.stdout)["Items"]["NativeFileReference"]
                    expected = {native / name for name in check_rid.native_package.LIBRARIES["Web"]} if rid == "browser-wasm" else set()
                    self.assertEqual({Path(item["FullPath"]) for item in references}, expected)
                    self.assertEqual(len(references), len(expected))
                    content = json.loads(result.stdout)["Items"]["Content"]
                    self.assertEqual([item["Link"] for item in content], ["wwwroot/licence/fixture.txt"] if rid == "browser-wasm" else [])

    def test_apple_static_references_select_only_the_requested_rid(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            targets = root / "buildTransitive/Electron2D.targets"
            targets.parent.mkdir()
            shutil.copyfile(rids.ROOT / "tools/native/Electron2D.targets", targets)
            rows = [row for row in rids.matrix() if row["platform"] in {"iOS", "tvOS"}]
            for row in rows:
                native = root / "runtimes" / row["rid"] / "native"
                native.mkdir(parents=True)
                for name in check_rid.native_package.ARCHIVES:
                    (native / name).write_bytes(b"!<arch>\n")
            project = ET.Element("Project")
            ET.SubElement(project, "Import", Project=str(targets))
            sdl = root / "sdl-image"
            sdl.mkdir()
            group = ET.SubElement(project, "PropertyGroup")
            ET.SubElement(group, "_SDL3CSNativeImageAppleStaticLibDir").text = str(sdl) + "/"
            group = ET.SubElement(project, "ItemGroup", Condition="'$(TargetPlatformIdentifier)' == 'ios' or '$(TargetPlatformIdentifier)' == 'tvos'")
            for name in ("libSDL3_image.a", "libpng16.a", "libz.a"):
                (sdl / name).write_bytes(b"!<arch>\n")
                ET.SubElement(group, "NativeReference", Include=str(sdl / name))
            foreign = root / "unrelated/libz.a"
            ET.SubElement(group, "NativeReference", Include=str(foreign))
            fixture = root / "fixture.proj"
            ET.ElementTree(project).write(fixture, encoding="unicode")
            for row in rows + [dict(rows[0], platform="Linux")]:
                with self.subTest(rid=row["rid"], platform=row["platform"]):
                    result = subprocess.run(["dotnet", "msbuild", str(fixture), "-nologo",
                                             "-p:RuntimeIdentifier=" + row["rid"],
                                             "-p:TargetPlatformIdentifier=" + row["platform"].lower(),
                                             "-t:Electron2DAppleCodecIdentity", "-getItem:NativeReference,LinkerArgument"],
                                            check=True, capture_output=True, text=True)
                    items = json.loads(result.stdout)["Items"]
                    references = items["NativeReference"]
                    if row["platform"] == "Linux":
                        self.assertEqual(references, [])
                        self.assertEqual(items["LinkerArgument"], [])
                        continue
                    self.assertEqual({Path(item["FullPath"]) for item in references},
                                     {root / "runtimes" / row["rid"] / "native" / name for name in check_rid.native_package.ARCHIVES} | {sdl / "libSDL3_image.a", foreign})
                    self.assertEqual(len(references), len(check_rid.native_package.ARCHIVES) + 2)
                    for item in (item for item in references if Path(item["Identity"]).name in check_rid.native_package.ARCHIVES):
                        self.assertEqual(Path(item["FullPath"]).parent, root / "runtimes" / row["rid"] / "native")
                        self.assertEqual(item["Kind"], "Static")
                        for flag in ("ForceLoad", "SmartLink", "IsCxx"):
                            self.assertEqual(item[flag], "true")
                    self.assertEqual([item["Identity"] for item in items["LinkerArgument"]], ["-framework", "CoreBluetooth"])

    def test_aggregate_status_requires_every_expected_dependency_to_pass(self):
        for group in ("build", "tests"):
            names = ("matrix", "native", group)
            passed = {name: {"result": "success"} for name in names}
            check_status.check(passed, group)
            for name in names:
                for result in ("failure", "cancelled", "skipped", "timed_out", None, "unknown"):
                    failed = dict(passed, **{name: {"result": result}})
                    with self.subTest(group=group, dependency=name, result=result), self.assertRaises(RuntimeError):
                        check_status.check(failed, group)
            for invalid in ({}, None, {group: {"result": "success"}}, dict(passed, extra={"result": "success"})):
                with self.assertRaises(ValueError):
                    check_status.check(invalid, group)

    def test_browser_cleanup_stops_only_its_process_group(self):
        for timeout in (False, True):
            process = Mock(pid=12345)
            process.wait.side_effect = [subprocess.TimeoutExpired("fixture", 10), 0] if timeout else [0, 0]
            with self.subTest(timeout=timeout), patch.object(run_browser.os, "killpg") as kill:
                run_browser.stop(process)
                self.assertEqual([call.args for call in kill.call_args_list],
                                 [(12345, run_browser.signal.SIGTERM), (12345, run_browser.signal.SIGKILL)])
                process.terminate.assert_not_called()

    def test_android_apk_notice_paths(self):
        notices = Path(__file__).resolve().parents[2] / "licence"
        with tempfile.TemporaryDirectory() as directory, redirect_stdout(StringIO()):
            apk = Path(directory) / "fixture.apk"
            with ZipFile(apk, "w"):
                pass
            with self.assertRaises(RuntimeError):
                run_android.check_apk(apk, "android-arm64")
            with ZipFile(apk, "w") as archive:
                for source in notices.iterdir():
                    if source.is_file() and source.name != "ReferenceData-LICENSE.txt":
                        archive.write(source, "assets/licence/" + source.name)
            with self.assertRaises(RuntimeError):
                run_android.check_apk(apk, "android-arm64")
            for rid, abi in run_android.ABIS.items():
                elf_class, machine = {"android-arm": (1, 40), "android-arm64": (2, 183), "android-x86": (1, 3), "android-x64": (2, 62)}[rid]
                header = bytearray(20); header[:6] = b"\x7fELF" + bytes((elf_class, 1)); struct.pack_into("<H", header, 18, machine)
                native = Path(directory) / (rid + ".apk")
                shutil.copyfile(apk, native)
                with ZipFile(native, "a") as archive:
                    for name in (*run_android.native_package.LIBRARIES["Android"], "libSDL3.so"):
                        archive.writestr(f"lib/{abi}/{name}", header)
                run_android.check_apk(native, rid)
                with self.assertRaises(RuntimeError):
                    run_android.check_apk(native, "android-x86" if rid != "android-x86" else "android-x64")
                with self.assertWarns(UserWarning), ZipFile(native, "a") as archive:
                    archive.writestr(f"lib/{abi}/libSDL3.so", b"foreign duplicate")
                with self.assertRaises(RuntimeError):
                    run_android.check_apk(native, rid)

    def test_apple_bundle_requires_notices_and_excludes_source_data(self):
        notices = Path(__file__).resolve().parents[2] / "licence"
        with tempfile.TemporaryDirectory() as directory, redirect_stdout(StringIO()):
            app = Path(directory)
            (app / "licence").mkdir()
            with self.assertRaises(RuntimeError):
                run_apple.check_bundle(app)
            for source in notices.iterdir():
                if source.is_file() and source.name != "ReferenceData-LICENSE.txt":
                    shutil.copyfile(source, app / "licence" / source.name)
            run_apple.check_bundle(app)
            (app / "licence/Electron2D-LICENSE.txt").write_text("changed")
            with self.assertRaises(RuntimeError):
                run_apple.check_bundle(app)
            shutil.copyfile(notices / "Electron2D-LICENSE.txt", app / "licence/Electron2D-LICENSE.txt")
            shutil.copyfile(notices / "ReferenceData-LICENSE.txt", app / "licence/ReferenceData-LICENSE.txt")
            with self.assertRaises(RuntimeError):
                run_apple.check_bundle(app)

    def test_browser_completion_rejects_stale_or_invalid_reports(self):
        for value in (None, {"run": "old", "status": "passed"}, {"run": "current", "status": "unknown"}, {"run": "current", "status": []},
                      {"run": "current", "status": "failed", "error": 7}):
            with self.assertRaises(ValueError):
                run_browser.validate_result(value, "current")
        self.assertEqual(run_browser.validate_result({"run": "current", "status": "passed"}, "current")["status"], "passed")
        self.assertEqual(run_browser.validate_result({"run": "current", "status": "failed", "error": "fixture"}, "current")["status"], "failed")

    def test_browser_rejects_missing_changed_and_source_only_notices_before_launch(self):
        notices = rids.ROOT / "licence"
        with tempfile.TemporaryDirectory() as directory, patch.object(run_browser.subprocess, "Popen") as launch:
            root = Path(directory)
            (root / "index.html").write_text("fixture")
            (root / "licence").mkdir()
            with self.assertRaisesRegex(RuntimeError, "Missing bundle notice"):
                run_browser.run(root, "fixture")
            for source in notices.iterdir():
                if source.is_file() and source.name != "ReferenceData-LICENSE.txt":
                    shutil.copyfile(source, root / "licence" / source.name)
            (root / "licence/Electron2D-LICENSE.txt").write_text("changed")
            with self.assertRaisesRegex(RuntimeError, "Changed bundle notice"):
                run_browser.run(root, "fixture")
            shutil.copyfile(notices / "Electron2D-LICENSE.txt", root / "licence/Electron2D-LICENSE.txt")
            shutil.copyfile(notices / "ReferenceData-LICENSE.txt", root / "licence/ReferenceData-LICENSE.txt")
            with self.assertRaisesRegex(RuntimeError, "Source-only"):
                run_browser.run(root, "fixture")
            launch.assert_not_called()

    def test_mobile_driver_rejects_missing_stale_and_failed_results(self):
        self.assertFalse(run_android.result("RESULT old PASS", "current"))
        self.assertFalse(run_android.result("app crashed", "current"))
        self.assertTrue(run_android.result("I/Electron2DTests: RESULT current PASS", "current"))
        with self.assertRaises(RuntimeError):
            run_android.result("E/Electron2DTests: RESULT current FAIL exception", "current")
        with self.assertRaisesRegex(RuntimeError, "resampling fixture"):
            run_android.result("RESULT old PASS\nE/Electron2DTests: RESULT current FAIL outer exception\n"
                               "E/Electron2DTests: ---> resampling fixture", "current")
        with self.assertRaises(RuntimeError):
            run_apple.select({"runtimes": [], "devicetypes": []}, "iOS")
        profiles = {"runtimes": [{"name": "iOS 26", "version": "26.0", "identifier": "older", "isAvailable": True},
                                  {"name": "iOS 26.1", "version": "26.1", "identifier": "newer", "isAvailable": True,
                                   "supportedDeviceTypes": [{"name": "iPhone 17", "identifier": "phone"}]},
                                  {"name": "iOS 27", "version": "27.0", "identifier": "missing", "isAvailable": False}],
                    "devicetypes": [{"name": "iPhone 17", "identifier": "phone"}, {"name": "iPhone 6s Plus", "identifier": "unsupported"}]}
        self.assertEqual(run_apple.select(profiles, "iOS"), ("newer", "phone"))
        profiles["runtimes"][1]["supportedDeviceTypes"] = []
        with self.assertRaises(RuntimeError):
            run_apple.select(profiles, "iOS")

    def test_android_system_logcat_preserves_all_buffers_and_command_failures(self):
        adb = ["adb", "-s", "emulator-fixture"]
        for outcome in ("success", "denied", "timeout"):
            with self.subTest(outcome=outcome):
                call = subprocess.TimeoutExpired("logcat", 15, b"partial system exit", "timeout fixture") if outcome == "timeout" else \
                    subprocess.CompletedProcess([], int(outcome == "denied"), "ActivityManager kill fixture", "permission fixture")
                with patch.object(run_android.subprocess, "run", side_effect=call if isinstance(call, Exception) else None,
                                  return_value=call) as process:
                    text = run_android.system_logcat(adb)
                self.assertIn("partial system exit" if outcome == "timeout" else "ActivityManager kill fixture", text)
                self.assertIn("timed out" if outcome == "timeout" else "exit " + str(int(outcome == "denied")), text)
                self.assertIn("timeout fixture" if outcome == "timeout" else "permission fixture", text)
                process.assert_called_once_with(adb + ["logcat", "-d", "-b", "all", "-v", "threadtime"],
                                               capture_output=True, text=True, timeout=15)

    def test_android_backtrace_uses_only_the_owned_pid_and_bounded_commands(self):
        adb = ["adb", "-s", "emulator-fixture"]
        for pid in ("", "0", "99999999999", "12 34", "12;reboot", "\u0661\u0662"):
            with self.subTest(pid=pid), patch.object(run_android.subprocess, "run",
                    return_value=subprocess.CompletedProcess([], 0, pid, "")) as process:
                self.assertIn("unavailable", run_android.backtrace(adb))
                process.assert_called_once_with(adb + ["shell", "pidof", run_android.PACKAGE],
                                               capture_output=True, text=True, timeout=15)
        for outcome in ("success", "denied", "timeout"):
            with self.subTest(outcome=outcome):
                calls = [subprocess.CompletedProcess([], 0, "123\n", "")]
                calls.append(subprocess.TimeoutExpired("debuggerd", 15, b"partial native frame", "timeout fixture") if outcome == "timeout" else
                             subprocess.CompletedProcess([], int(outcome == "denied"), "native frame fixture", "permission fixture"))
                with patch.object(run_android.subprocess, "run", side_effect=calls) as process:
                    text = run_android.backtrace(adb)
                self.assertIn("timed out" if outcome == "timeout" else "native frame fixture", text)
                if outcome == "timeout":
                    self.assertIn("partial native frame", text)
                    self.assertIn("timeout fixture", text)
                if outcome == "denied":
                    self.assertIn("exit 1", text)
                    self.assertIn("permission fixture", text)
                self.assertEqual(process.call_args_list[-1].args[0], adb + ["shell", "debuggerd", "-b", "123"])
                self.assertEqual(process.call_args_list[-1].kwargs["timeout"], 15)

    def test_apple_report_checks_completion_and_cleans_up(self):
        profiles = {"runtimes": [{"name": "iOS 26", "version": "26.0", "identifier": "runtime", "isAvailable": True,
                                  "supportedDeviceTypes": [{"name": "iPhone 17", "identifier": "phone"}]}],
                    "devicetypes": [{"name": "iPhone 17", "identifier": "phone"}]}
        cases = [(status, False, False) for status in ("PASS", "FAIL fixture", "missing", "stale")]
        cases += [("PASS", True, False), ("FAIL fixture", True, False), ("PASS", True, True)]
        for status, shutdown_timeout, delete_failure in cases:
            with self.subTest(status=status, shutdown_timeout=shutdown_timeout, delete_failure=delete_failure), \
                    tempfile.TemporaryDirectory() as directory, redirect_stdout(StringIO()) as log:
                container = Path(directory)
                (container / "tmp").mkdir()

                def command(args, **kwargs):
                    if args[2] == "shutdown" and shutdown_timeout:
                        raise subprocess.TimeoutExpired(args, kwargs["timeout"])
                    if args[2] == "delete" and delete_failure:
                        raise subprocess.CalledProcessError(1, args, "", "deletion fixture")
                    output = json.dumps(profiles) if args[2] == "list" else "fixture" if args[2] == "create" else directory if args[2] == "get_app_container" else ""
                    if args[2] == "launch" and status != "missing":
                        env = kwargs["env"]
                        token = "old" if status == "stale" else env["SIMCTL_CHILD_ELECTRON2D_RUN_TOKEN"]
                        Path(env["SIMCTL_CHILD_ELECTRON2D_RESULT_PATH"]).write_text("RESULT " + token + " " + status + "\n")
                    if args[2] == "launch":
                        stderr = next(arg.split("=", 1)[1] for arg in args if arg.startswith("--stderr="))
                        Path(stderr).write_text("native launch exception fixture")
                        self.assertTrue(any(arg.startswith("--stdout=") for arg in args))
                    return subprocess.CompletedProcess(args, 0, output, "")

                with patch.object(run_apple.subprocess, "run", side_effect=command) as process:
                    if status == "PASS" and not delete_failure:
                        run_apple.run(Path("fixture.app"), "iOS")
                    else:
                        with self.assertRaises((RuntimeError, TimeoutError)) as error:
                            run_apple.run(Path("fixture.app"), "iOS", timeout=.01)
                        if status in ("missing", "stale"):
                            self.assertIn("native launch exception fixture", str(error.exception))
                        elif delete_failure:
                            self.assertIn("deletion fixture", str(error.exception))
                        else:
                            self.assertIn("FAIL fixture", str(error.exception))
                self.assertEqual(process.call_args_list[-2].args[0], ["xcrun", "simctl", "shutdown", "fixture"])
                self.assertEqual(process.call_args_list[-1].args[0], ["xcrun", "simctl", "delete", "fixture"])
                if shutdown_timeout:
                    self.assertIn("shutdown timed out; attempting deletion", log.getvalue())

    def test_apple_setup_timeout_preserves_progress_and_cleans_up(self):
        profiles = {"runtimes": [{"name": "iOS 26", "version": "26.0", "identifier": "runtime", "isAvailable": True,
                                  "supportedDeviceTypes": [{"name": "iPhone 17", "identifier": "phone"}]}]}
        for phase in ("bootstatus", "install"):
            with self.subTest(phase=phase), redirect_stdout(StringIO()) as log:
                def command(args, **kwargs):
                    if args[2] == phase:
                        raise subprocess.TimeoutExpired(args, kwargs["timeout"], b"Waiting on system app launch\n", "setup fixture blocked\n")
                    output = json.dumps(profiles) if args[2] == "list" else "fixture" if args[2] == "create" else ""
                    return subprocess.CompletedProcess(args, 0, output, "")

                with patch.object(run_apple.subprocess, "run", side_effect=command) as process:
                    with self.assertRaises(TimeoutError) as error:
                        run_apple.run(Path("fixture.app"), "iOS")
                self.assertIn(phase, str(error.exception))
                self.assertIn("300" if phase == "bootstatus" else "180", str(error.exception))
                self.assertIn("Waiting on system app launch", str(error.exception))
                self.assertIn("setup fixture blocked", str(error.exception))
                self.assertIn("runtime", log.getvalue())
                self.assertIn("phone", log.getvalue())
                self.assertEqual(sum(call.args[0][2] == phase for call in process.call_args_list), 1)
                self.assertEqual(process.call_args_list[-2].args[0], ["xcrun", "simctl", "shutdown", "fixture"])
                self.assertEqual(process.call_args_list[-1].args[0], ["xcrun", "simctl", "delete", "fixture"])

    def test_matrix_rejects_missing_duplicate_and_wrong_platform(self):
        rows = rids.matrix()
        for changed in (rows[:-1], rows + [rows[0]], [dict(rows[0], platform="Linux")] + rows[1:]):
            with self.subTest(rows=changed), patch.object(rids.json, "loads", return_value=changed):
                with self.assertRaises(ValueError):
                    rids.matrix()
        for label in (None, "", "  ", 1, rows[1]["label"]):
            changed = [dict(rows[0], label=label)] + rows[1:]
            with self.subTest(label=label), patch.object(rids.json, "loads", return_value=changed):
                with self.assertRaises(ValueError):
                    rids.matrix()

    def test_artifacts_and_rejections(self):
        for row in rids.matrix():
            rid = row["rid"]
            with self.subTest(rid=rid), tempfile.TemporaryDirectory() as directory:
                output = Path(directory)
                profile = {"Properties": {"RuntimeIdentifier": rid, "TargetFramework": row["framework"],
                                         "Electron2DNativePlatform": row["platform"]},
                           "Items": {"PackageReference": []}}
                if row["platform"] == "Android":
                    profile["Items"]["PackageReference"].append({
                        "Identity": "SDL3-CS.Android", "Version": check_rid.native_package.configuration()["sdlVersion"],
                        "PrivateAssets": "all", "ExcludeAssets": "compile;runtime;native", "GeneratePathProperty": "true"})
                profile_file = output / "profile.json"
                profile_file.write_text(json.dumps(profile))
                (output / "Electron2D.dll").write_bytes(b"MZ")
                (output / "Electron2D.xml").write_text("<doc><assembly><name>Electron2D</name></assembly></doc>")
                check_rid.check(rid, output)
                if row["platform"] == "Android":
                    bootstrap = profile["Items"]["PackageReference"][0]
                    for field, wrong in (("Version", "0.0.0"), ("PrivateAssets", ""), ("ExcludeAssets", "compile"), ("GeneratePathProperty", "false")):
                        previous = bootstrap[field]
                        bootstrap[field] = wrong
                        profile_file.write_text(json.dumps(profile))
                        with self.subTest(field=field), self.assertRaises(ValueError):
                            check_rid.check(rid, output)
                        bootstrap[field] = previous
                profile["Properties"]["RuntimeIdentifier"] = "wrong-rid"
                profile_file.write_text(json.dumps(profile))
                with self.assertRaises(ValueError):
                    check_rid.check(rid, output)
                profile["Properties"]["RuntimeIdentifier"] = rid
                profile["Items"]["PackageReference"].append({"Identity": "SDL3-CS.Wrong"})
                profile_file.write_text(json.dumps(profile))
                with self.assertRaises(ValueError):
                    check_rid.check(rid, output)
                profile["Items"]["PackageReference"].pop()
                profile_file.write_text(json.dumps(profile))
                flat = output / "libFAudio.so.0"
                flat.write_bytes(b"native payload in a managed-only product")
                with self.assertRaises(ValueError):
                    check_rid.check(rid, output)
                flat.unlink()
                native = output / "runtimes" / rid / "native"
                native.mkdir(parents=True)
                (native / "foreign.so").write_bytes(b"native payload in a managed-only product")
                with self.assertRaises(ValueError):
                    check_rid.check(rid, output)


if __name__ == "__main__":
    unittest.main()
