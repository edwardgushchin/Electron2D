#!/usr/bin/env python3
"""Build and measure public CPU/lavapipe worlds; raw reports stay in the selected output directory."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import platform
import signal
import subprocess


def run_probe(command, *, cwd, env, stdout, stderr, timeout):
    # A stuck live attachment may leave its fixture suspended; retire the whole owned process group.
    with subprocess.Popen(command, cwd=cwd, env=env, stdout=stdout, stderr=stderr, start_new_session=True) as child:
        try:
            code = child.wait(timeout=timeout)
        except BaseException:
            try:
                os.killpg(child.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
            child.wait()
            raise
        if code:
            raise subprocess.CalledProcessError(code, command)


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("output", type=Path)
parser.add_argument("--icd", type=Path, help="Optional lavapipe ICD; without it only the no-GPU CPU fixture runs")
parser.add_argument("--bodies", type=int, default=1024)
parser.add_argument("--history", type=int, default=4)
parser.add_argument("--network", action="store_true", help="Measure the full three-process network example instead of the raw world")
parser.add_argument("--native-phases", action="store_true", help="Compare cold compiler/device controls and both public GPU retirement policies without heap attachment")
parser.add_argument("--heaptrack-preload", type=Path, help="Explicit libheaptrack_preload.so for native-phase/network child allocation stacks; changes measurement overhead")
parser.add_argument("--client-backend", choices=["cpu", "gpu"], default="gpu", help="Primary network client backend")
parser.add_argument("--threaded", action="store_true", help="Use a separate numerical thread in network or retirement fixtures")
parser.add_argument("--unpaced", action="store_true", help="Use ordinary network sessions without authority phase/input barriers")
parser.add_argument("--client-poll-delay-ms", type=int, default=0, help="Add 0 through 250 ms between primary network client polls")
parser.add_argument("--cycles", type=int, default=0, help="Measure 1 through 32 retirement cycles; native phases default to 32")
args = parser.parse_args()
if args.native_phases and (args.network or args.threaded or not args.icd):
    parser.error("Native phases require an explicit lavapipe ICD, include both policies, and cannot combine with --network or --threaded")
if args.native_phases and not args.cycles:
    args.cycles = 32
if args.network and (args.bodies != 1024 or args.history != 4):
    parser.error("Body/history options apply only to the raw fixture")
if args.cycles < 0 or args.cycles > 32 or args.network and args.cycles:
    parser.error("Retirement cycles must be 1 through 32 and apply only to the raw fixture")
if args.threaded and not (args.network or args.cycles):
    parser.error("Separate-thread selection applies to network or retirement fixtures")
if args.network and args.client_backend == "gpu" and not args.icd:
    parser.error("The GPU network client requires an explicit lavapipe ICD")
if not 0 <= args.client_poll_delay_ms <= 250:
    parser.error("Client poll delay must be between 0 and 250 milliseconds")
if (args.unpaced or args.client_poll_delay_ms) and not args.network:
    parser.error("Unpaced sessions and client poll delay require --network")
heaptrack = None
if args.heaptrack_preload:
    if not (args.native_phases or args.network):
        parser.error("Heaptrack allocation stacks require --native-phases or --network")
    args.heaptrack_preload = args.heaptrack_preload.resolve()
    if args.heaptrack_preload.name != "libheaptrack_preload.so" or not args.heaptrack_preload.is_file():
        parser.error("Heaptrack requires an existing libheaptrack_preload.so")
    heaptrack = {"preload": str(args.heaptrack_preload), "sha256": hashlib.file_digest(args.heaptrack_preload.open("rb"), "sha256").hexdigest()}
repo = Path(__file__).resolve().parents[2]
output = args.output.resolve()
output.mkdir(parents=True, exist_ok=False)
env = {k: v for k, v in os.environ.items() if k not in {
    "DISPLAY", "WAYLAND_DISPLAY", "XDG_RUNTIME_DIR", "LD_LIBRARY_PATH", "LD_PRELOAD", "VK_INSTANCE_LAYERS", "VK_DEVICE_LAYERS"
} and not k.startswith(("ELECTRON2D_TEST_", "ELECTRON2D_SANDBOX_"))}
env.update(SDL_VIDEODRIVER="offscreen", SDL_GPU_DRIVER="vulkan", VK_LOADER_LAYERS_DISABLE="~all~", LP_NUM_THREADS="4")
driver = None
if args.icd:
    args.icd = args.icd.resolve()
    library = Path(json.loads(args.icd.read_text())["ICD"]["library_path"])
    if not library.is_absolute():
        library = args.icd.parent / library
    if library.name != "libvulkan_lvp.so" or not library.is_file():
        raise ValueError("GPU memory profiling requires an explicit existing lavapipe library")
    driver = {"icd": str(args.icd), "library": str(library), "sha256": hashlib.file_digest(library.open("rb"), "sha256").hexdigest()}
fixture = "NetworkFixture" if args.network else "Fixture"
projects = [("Probe", repo / "tools/physics-memory/Probe/Probe.csproj", []),
            (fixture, repo / f"tools/physics-memory/{fixture}/{fixture}.csproj", ["-p:Electron2DBuildNativeFromSource=true"])]
if args.native_phases:
    projects.append(("Tests", repo / "tests/Electron2D.Tests/Electron2D.Tests.csproj", ["-p:Electron2DBuildNativeFromSource=true"]))
for name, project, extra in projects:
    command = ["dotnet", "build", str(project), "-c", "Release", "-o", str(output / name), *extra]
    with (output / f"build-{name}.log").open("w") as log:
        subprocess.run(command, cwd=repo, env=env, stdout=log, stderr=subprocess.STDOUT, check=True)
abi_program = output / "process-memory-abi"
subprocess.run(["cc", "-std=c11", "-Wall", "-Wextra", "-Werror", str(repo / "tools/physics-memory/test_process_memory.c"), "-o", str(abi_program)], check=True)
abi = json.loads(subprocess.check_output([str(abi_program)], text=True))
with (output / "process-memory-calibration.json").open("w") as log, (output / "process-memory-calibration.err").open("w") as error:
    run_probe(["dotnet", str(output / fixture / (fixture + ".dll")), "--process-memory-calibration"], cwd=repo, env=env,
              stdout=log, stderr=error, timeout=30)
calibration = json.loads((output / "process-memory-calibration.json").read_text())
if any(calibration[key] != value for key, value in abi.items()):
    raise ValueError("Managed mallinfo2 declaration does not match the host C ABI")
(output / "process-memory-abi.json").write_text(json.dumps(abi, indent=2) + "\n")
with (output / "process-memory-self-test.log").open("w") as log:
    run_probe(["dotnet", str(output / "Probe/Probe.dll"), "--process-memory-self-test"], cwd=repo, env=env,
              stdout=log, stderr=subprocess.STDOUT, timeout=30)
phase_markers = None
if args.network and heaptrack:
    marker_library = output / "libElectron2DMemoryPhases.so"
    marker_calibration = output / "phase-marker-calibration"
    marker_source = repo / "tools/physics-memory/phase_markers.c"
    common = ["cc", "-std=c11", "-g", "-O0", "-fno-builtin", "-Wall", "-Wextra", "-Werror", str(marker_source)]
    with (output / "build-phase-markers.log").open("w") as log:
        subprocess.run([*common, "-shared", "-fPIC", "-o", str(marker_library)], stdout=log, stderr=subprocess.STDOUT, check=True)
        subprocess.run([*common, "-DMEMORY_PHASE_CALIBRATION", "-o", str(marker_calibration)], stdout=log, stderr=subprocess.STDOUT, check=True)
    trace = output / "phase-marker-calibration.raw"
    with (output / "phase-marker-calibration.json").open("w") as log, (output / "phase-marker-calibration.err").open("w") as error:
        run_probe([str(marker_calibration)], cwd=repo, env=env | {"LD_PRELOAD": heaptrack["preload"], "DUMP_HEAPTRACK_OUTPUT": str(trace)},
                  stdout=log, stderr=error, timeout=30)
    phase_markers = {"library": str(marker_library), "sha256": hashlib.file_digest(marker_library.open("rb"), "sha256").hexdigest(),
                     "calibrationTraceSHA256": hashlib.file_digest(trace.open("rb"), "sha256").hexdigest()}
if args.native_phases:
    env.update(VK_DRIVER_FILES=str(args.icd), VK_ICD_FILENAMES=str(args.icd))
    command = ["dotnet", str(output / "Probe/Probe.dll"), "--native-phases", str(output / "Tests/Electron2D.Tests.dll"),
               str(output / "Fixture/Fixture.dll"), str(args.bodies), str(args.history), str(args.cycles)]
    if heaptrack:
        command.extend([heaptrack["preload"], str(output)])
    with (output / "native-phases.json").open("w") as log, (output / "native-phases.err").open("w") as error:
        run_probe(command, cwd=repo, env=env, stdout=log, stderr=error, timeout=300)
elif args.network:
    if heaptrack:
        env["ELECTRON2D_TEST_PHYSICS_HEAPTRACK_PRELOAD"] = heaptrack["preload"]
        env["ELECTRON2D_TEST_PHYSICS_PHASE_MARKERS"] = phase_markers["library"]
    icd = args.icd if args.client_backend == "gpu" else output / "missing-icd.json"
    env.update(VK_DRIVER_FILES=str(icd), VK_ICD_FILENAMES=str(icd))
    command = ["dotnet", str(output / "Probe/Probe.dll"), "--network", str(output / fixture / (fixture + ".dll")), str(output / "network"),
               "--client-backend", args.client_backend, "--ticks", "900", "--late-join-tick", "310"]
    if args.threaded:
        command.append("--threaded")
    if args.unpaced:
        command.append("--unpaced")
    command.extend(["--client-poll-delay-ms", str(args.client_poll_delay_ms)])
    with (output / "network.log").open("w") as log, (output / "network.err").open("w") as error:
        run_probe(command, cwd=repo, env=env, stdout=log, stderr=error, timeout=120)
for backend in [] if args.network or args.native_phases else ["cpu", *(["gpu"] if driver else [])]:
    icd = args.icd if backend == "gpu" else output / "missing-icd.json"
    env.update(VK_DRIVER_FILES=str(icd), VK_ICD_FILENAMES=str(icd))
    command = ["dotnet", str(output / "Probe/Probe.dll"), str(output / "Fixture/Fixture.dll"), backend, str(args.bodies), str(args.history)]
    if args.cycles:
        command.insert(2, "--retirement")
        command.extend([str(args.cycles), str(args.threaded).lower()])
    with (output / f"{backend}.json").open("w") as log, (output / f"{backend}.err").open("w") as error:
        run_probe(command, cwd=repo, env=env, stdout=log, stderr=error, timeout=300)
    report = json.loads((output / f"{backend}.json").read_text())
    print(backend, [(p["Measurement"]["Metadata"]["Phase"], p["Measurement"]["ManagedObjectBytes"]) for p in report["Phases"]], flush=True)
artifact_paths = [output / "Probe/Probe.dll", output / fixture / (fixture + ".dll"), output / fixture / "Electron2D.dll", abi_program]
if phase_markers:
    artifact_paths.extend([marker_library, marker_calibration])
if args.native_phases:
    artifact_paths.extend([output / "Tests/Electron2D.Tests.dll", output / "Tests/Electron2D.dll"])
artifacts = {str(p.relative_to(output)): hashlib.file_digest(p.open("rb"), "sha256").hexdigest() for p in artifact_paths}
(output / "environment.json").write_text(json.dumps({"driver": driver, "heaptrack": heaptrack, "phaseMarkers": phase_markers, "bodies": args.bodies, "history": args.history,
    "network": args.network, "unpaced": args.unpaced, "clientPollDelayMS": args.client_poll_delay_ms, "nativePhases": args.native_phases, "clientBackend": args.client_backend, "threaded": args.threaded, "cycles": args.cycles, "artifacts": artifacts,
    "host": {"kernel": platform.release(), "architecture": platform.machine(), "pageSize": os.sysconf("SC_PAGESIZE"),
             "libcVersion": abi["LibcVersion"], "mallocArenaMax": env.get("MALLOC_ARENA_MAX")}}, indent=2) + "\n")
