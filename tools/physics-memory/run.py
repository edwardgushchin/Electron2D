#!/usr/bin/env python3
"""Build and measure public CPU/lavapipe worlds; raw reports stay in the selected output directory."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("output", type=Path)
parser.add_argument("--icd", type=Path, help="Optional lavapipe ICD; without it only the no-GPU CPU fixture runs")
parser.add_argument("--bodies", type=int, default=1024)
parser.add_argument("--history", type=int, default=4)
args = parser.parse_args()
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
for name, extra in [("Probe", []), ("Fixture", ["-p:Electron2DBuildNativeFromSource=true"])]:
    command = ["dotnet", "build", str(repo / f"tools/physics-memory/{name}/{name}.csproj"), "-c", "Release", "-o", str(output / name), *extra]
    with (output / f"build-{name}.log").open("w") as log:
        subprocess.run(command, cwd=repo, env=env, stdout=log, stderr=subprocess.STDOUT, check=True)
for backend in ["cpu", *(["gpu"] if driver else [])]:
    icd = args.icd if backend == "gpu" else output / "missing-icd.json"
    env.update(VK_DRIVER_FILES=str(icd), VK_ICD_FILENAMES=str(icd))
    command = ["dotnet", str(output / "Probe/Probe.dll"), str(output / "Fixture/Fixture.dll"), backend, str(args.bodies), str(args.history)]
    with (output / f"{backend}.json").open("w") as log, (output / f"{backend}.err").open("w") as error:
        subprocess.run(command, cwd=repo, env=env, stdout=log, stderr=error, check=True, timeout=300)
    report = json.loads((output / f"{backend}.json").read_text())
    print(backend, [(p["Measurement"]["Metadata"]["Phase"], p["Measurement"]["ManagedObjectBytes"]) for p in report["Phases"]], flush=True)
(output / "environment.json").write_text(json.dumps({"driver": driver, "bodies": args.bodies, "history": args.history}, indent=2) + "\n")
