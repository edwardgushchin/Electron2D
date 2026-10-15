# Physics retained object measurement

Run from the repository root on Linux x64 with .NET 10 and the normal source-native
build prerequisites:

```bash
python3 -B tools/physics-memory/run.py bin/physics-memory-new
```

This builds a public-API fixture and a separate CLRMD probe. CPU runs with a missing
Vulkan ICD, no display and no renderer. To add the full resident GPU path on a
software device, supply an explicit lavapipe ICD:

```bash
python3 -B tools/physics-memory/run.py bin/physics-memory-new-software \
  --icd /absolute/path/to/lavapipe.json --bodies 1024 --history 4
```

The output directory must be new. Defaults are 1024 bodies and four checkpoints;
body counts must be multiples of four from 4 through 65536, histories 1 through 32.
The GPU path checks the selected implementation without startup fallback and
requires a mapped lavapipe library while the world is live; NVIDIA mappings reject.
No window is opened. Source-native build logs, per-type JSON reports, assembly
hashes and the software driver identity stay in the selected output directory.

The fixture creates raw circles in isolated pairs and one origin pin, with zero
gravity and sleeping disabled. It exercises full steps, private capture/restore
and portable capture/apply before each collection. Five phases cover the original
world, added history/map/packet, a quarter-sized world after retirement and
recapture, disposed histories whose public wrappers remain held, and disposed
world. The portable packet deliberately survives to expose its peak capacity.

The parent launches and suspends only its own child using
[CLRMD attachment](https://github.com/microsoft/clrmd/blob/main/doc/GettingStarted.md).
It disposes each attachment before advancing the fixture and terminates its child
on failure. This needs the ordinary parent-to-child ptrace permission; it changes
no system ptrace configuration and writes no process dump. The package lock
applies only to the probe, never the engine or game consumers.

Each managed object is counted once by its CLR-reported `Size`. The probe cuts
other owner roots, attributes the world first, then map, packet, and each common
checkpoint/selected solver. It cuts both common and resident checkpoint registries;
source-world and backend-owner references are borrowed. It verifies the resulting
partition equals the unrestricted forward union of all owner roots. A known shared
65536-byte array and a separately kept-alive weak target check duplicate and weak
reference handling at every phase. JSON includes every type's count and bytes.

This is an explicit graph partition for this controlled fixture, not a dominator
analysis or total process retained size. Shared empty arrays/comparers go to the
first owner that reaches them. The fixture has no user callbacks or scene tree;
do not apply this ownership convention to arbitrary game graphs. Weak targets and
conditional dependent handles are not traversed. The fixture anchors and RID array,
static service tables, unreachable/static pool shells after world disposal, native
heaps, thread stacks, CLR bookkeeping, GPU pipelines, driver heaps and allocator
padding beyond CLR-reported sizes are outside the managed result. Requested SDL
device/transfer buffer bytes are reported separately and include private history;
they do not measure physical VRAM. CoreCLR heap sizes do not establish NativeAOT
sizes, allocations per step, hardware timing or window FPS.

Recorded scope and measurements: [retained physics memory](../../docs/components/physics-retained-memory.md).
