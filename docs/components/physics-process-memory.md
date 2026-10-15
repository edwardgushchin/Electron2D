# Physics process and allocator memory

Last updated: 2026-10-15

## Scope and measurement

[The development runner](../../tools/physics-memory/README.md#process-and-allocator-observations)
now adds process-wide observations to its [owned managed graph](physics-retained-memory.md)
and [scene/network history](physics-network-memory.md) reports. The engine and
shipped example are unchanged. Linux x64/CoreCLR uses CPU with an unavailable
Vulkan ICD and GPU on the pinned lavapipe library, without fallback, display,
window, renderer or NVIDIA mapping. Requested SDL bytes remain reservations;
software-device storage can occupy host memory.

The raw fixture pauses before creating a world, then at the five existing world,
history, shrink and disposal stages. The network host pauses each real child
before startup and at its four existing completion/history/disposal stages.
Collection and finalizer drains precede each pause. The fixture samples the last
full blocking GC and glibc before serializing metadata; the parent reads procfs
after publication and before CLRMD attachment. These are nearby quiescent samples,
not one atomic snapshot. Baselines include the measurement host; later lazy JIT,
serializer, transport and driver initialization also contribute to growth.

The reports retain three different scopes:

- `GC` records heap size, fragmentation and total committed managed heap bytes
  at the latest full blocking collection. This includes more than selected physics
  owners and is not current allocation traffic. See [GCMemoryInfo](https://learn.microsoft.com/en-us/dotnet/api/system.gcmemoryinfo?view=net-10.0).
- `MallInfo2` preserves the host glibc's named fields. `Arena`, `Uordblks`,
  `Fordblks` and `Hblkhd` describe allocator storage, reported used/free arena
  chunks and mmap-backed malloc regions. They include runtime/library allocations
  and allocator caching; they do not identify physical owners, every native
  allocator, arbitrary direct mappings or driver memory. See [glibc statistics](https://snapshots.sourceware.org/glibc/trunk/latest/manual/html_node/Statistics-of-Malloc.html).
- `Process` records procfs status fields and page-table `smaps_rollup` RSS/PSS,
  shared/private and anonymous/file-backed storage, swap and thread count. Kernel
  kB values convert to bytes. Virtual size is an address-space reservation, not
  resident memory. Status RSS/high water uses asynchronous kernel accounting;
  keep its raw value rather than assuming exact monotonic peaks. Rollup measures
  current mapped pages. See [kernel procfs documentation](https://cdn.kernel.org/doc/html/latest/filesystems/proc.html).

These scopes overlap. Do not add GC, malloc, RSS and requested SDL bytes, or call
RSS minus selected managed objects a native-memory total. Sampled maxima and
kernel high-water values do not prove every transient allocation peak.

## Calibration

The host C compiler checks `mallinfo2` against the installed header: size 80,
pointer size 8, `uordblks` offset 56 and `hblkhd` offset 32. The managed declaration
and libc version must agree. A separate process allocates and touches 8,388,608
native bytes, observes at least that increase in reported used-plus-mapped storage,
frees it and requires the corresponding decrease. This process never runs the
game fixture, so its test block cannot inflate that fixture's high water.
No malloc tuning, trimming or preload interposer is enabled by the runner.

The procfs self-check verifies 1024-byte unit conversion and missing, invalid,
negative, duplicate and overflow rejection, then reads its own live process.
Existing shared-array/weak-target, owner-union and idle CPU-slot assertions still
run at every attachment. This calibration establishes the exercised collector,
not full native ownership or general live-attachment reliability.

## Verification limits

Collection/reporting perturb the process. The fixed scenes use paired circles
with one pin; the network example retains its real geometry, contacts, sleep,
authority correction, lifecycle and warmed managed-allocation checks. Neither
profile is a new speed benchmark or a representative upper bound for games.
The public-only network host selects serial/separate scheduling but does not
enable internal numerical-thread/transfer telemetry. Other reports establish
those separately.

Per-world native/driver attribution, longer and wider creation/failure retirement,
continuous peaks and transport queue high water remain open. So do other global
roots, NativeAOT/platform memory, wider loads, full physics/extension/inherited
acceptance and target-hardware speed/window FPS. Real RTX loading remains deferred
while its power cable is unverified.

## Observed raw process bytes

All values below are bytes from the accepted current-source cohorts. RSS is the
current `smaps_rollup` value; used/free/mapped columns retain glibc field meanings.
All phases and PSS/fragmentation/virtual/high-water values remain in JSON.

### 1024 original bodies, four local checkpoints

| Backend / phase | RSS | GC committed | Uordblks | Fordblks | Hblkhd |
| --- | ---: | ---: | ---: | ---: | ---: |
| CPU / baseline | 48,394,240 | 614,400 | 2,720,784 | 805,872 | 417,792 |
| CPU / world | 111,190,016 | 44,462,080 | 11,320,496 | 1,246,032 | 417,792 |
| CPU / history | 134,791,168 | 71,634,944 | 11,359,584 | 1,206,944 | 417,792 |
| CPU / released-world | 122,130,432 | 55,287,808 | 5,439,808 | 7,126,720 | 417,792 |
| GPU / baseline | 48,562,176 | 614,400 | 2,721,248 | 805,408 | 417,792 |
| GPU / world | 253,042,688 | 9,457,664 | 83,678,800 | 65,608,112 | 417,792 |
| GPU / history | 262,737,920 | 16,732,160 | 84,432,384 | 64,854,528 | 417,792 |
| GPU / released-world | 236,638,208 | 14,774,272 | 14,472,352 | 134,814,560 | 417,792 |

### 4096 original bodies, four local checkpoints

| Backend / phase | RSS | GC committed | Uordblks | Fordblks | Hblkhd |
| --- | ---: | ---: | ---: | ---: | ---: |
| CPU / baseline | 48,414,720 | 811,008 | 2,720,160 | 802,400 | 417,792 |
| CPU / world | 250,294,272 | 181,112,832 | 11,232,256 | 1,588,224 | 417,792 |
| CPU / history | 325,689,344 | 255,664,128 | 11,271,376 | 1,549,104 | 417,792 |
| CPU / released-world | 262,066,176 | 191,463,424 | 2,907,104 | 9,913,376 | 417,792 |
| GPU / baseline | 48,562,176 | 811,008 | 2,719,664 | 802,896 | 417,792 |
| GPU / world | 261,754,880 | 15,286,272 | 83,682,624 | 65,661,632 | 417,792 |
| GPU / history | 301,555,712 | 50,921,472 | 84,306,560 | 65,037,696 | 417,792 |
| GPU / released-world | 256,507,904 | 37,797,888 | 3,266,016 | 146,078,240 | 417,792 |

The 4096-body CPU process retains 191,463,424 GC-committed bytes at release,
although its selected caller-held graph is only 1,770,768 bytes plus the separately
counted CPU pool. Committed capacity therefore cannot be read as live selected
object storage. The 1024-body GPU process reports 134,814,560 free arena bytes at
release and RSS 236,638,208 bytes with requested device/transfer buffers at zero.
This separates freed/reserved allocator storage and overall residency; it does
not prove a leak or attribute every remaining page.

## Network process RSS

All four cohorts use the full 900-tick/late-310 check. Every row is an independent
process, with the same 28-object physical/network workload and original assertions.
The maximum below is only the largest of its five phase RSS observations.

| Primary / scheduling | Role | Baseline RSS | Completed RSS | Released RSS | Max observed RSS |
| --- | --- | ---: | ---: | ---: | ---: |
| cpu / serial | server | 42,008,576 | 94,519,296 | 102,830,080 | 102,830,080 |
| cpu / serial | cpu | 42,164,224 | 100,900,864 | 109,322,240 | 109,322,240 |
| cpu / serial | late | 41,897,984 | 100,384,768 | 108,941,312 | 108,941,312 |
| cpu / threaded | server | 42,090,496 | 94,752,768 | 103,030,784 | 103,030,784 |
| cpu / threaded | cpu | 42,086,400 | 101,064,704 | 109,424,640 | 109,424,640 |
| cpu / threaded | late | 42,094,592 | 100,913,152 | 109,408,256 | 109,408,256 |
| gpu / serial | server | 41,947,136 | 94,609,408 | 102,940,672 | 102,940,672 |
| gpu / serial | gpu | 42,090,496 | 621,981,696 | 600,047,616 | 622,751,744 |
| gpu / serial | late | 41,971,712 | 100,487,168 | 109,125,632 | 109,125,632 |
| gpu / threaded | server | 42,156,032 | 95,051,776 | 103,337,984 | 103,337,984 |
| gpu / threaded | gpu | 42,057,728 | 749,821,952 | 719,093,760 | 750,596,096 |
| gpu / threaded | late | 42,004,480 | 100,823,040 | 109,375,488 | 109,375,488 |

There are 16 measured application children and 84 calibrated heap/process
observations: 24 raw observations and 60 network observations. The native ABI
and 8 MiB calibration run separately for each of the six cohorts. All twelve
network children retain zero warmed owner/all-thread step/replay allocations;
CPU children exercise unavailable GPU startup. Released owner graphs and requested
GPU bytes satisfy the earlier disposal gates. This does not certify native
allocator or driver lifetime across repeated cycles.

## Evidence and reproduction

Run `python3 -B tools/physics-memory/run.py NEW_OUTPUT --bodies 1024 --history 4`
for CPU only. Add an explicit `--icd /absolute/path/to/lavapipe.json` for software
GPU; use 4096 bodies for the larger raw profile. Network mode uses `--network --client-backend cpu|gpu` and optional `--threaded`;
GPU still requires that ICD.

Local evidence: `bin/physics-process-memory/2026-10-15/accepted-1024/`,
`final-4096/` and the four `final-network-{cpu,gpu}-{serial,threaded}/` directories.
Each includes build logs, ABI and native-block calibration, procfs self-test,
JSON reports and environment hashes. The host is Linux x64/glibc 2.44 with
4096-byte pages; exact kernel identity is in each `environment.json`.

- Engine SHA-256: `c1a28b17f678f1885bfc1502381cfcc2656ff79f3cbcddf4bd865ed8f12cd052`.
- Raw fixture SHA-256: `1cff0612d2633ca277e4efea65579febd91abd928b9172a46adc9609c02ba90a`.
- Network fixture SHA-256: `a7ce07f145ad366b3c7d681144f9033c72397524ee166009d2e2e86a5284799a`.
- Probe SHA-256: `a4d6ab24cf8624e97eb2e8eaeaa64e24d182d478a56eeb1a944a31a3c185f940`.
- Native ABI check SHA-256: `5ea8abe7262272a49077a67511cb5e6cac29a1b5be82fb99b0221a2227f72194`.

The initial candidates passed their measurements but preceded a shared-helper
formatting correction. Only the six final cohorts above form these tables.
The source/PDB audit verifies their actual Compile inputs against current files;
this change adds no public runtime declaration or coverage-state promotion.

[Repeated raw-world retirement](physics-retirement-memory.md) subsequently measures
eight cycles with ordinary and failed GPU intervals. It exposes substantial default
separate-lane glibc arena retention even when selected owners collect; the issue
remains open. The one-arena diagnostic control is not a runtime policy change.
