# RTX physics baseline, 2026-10-20

Last updated: 2026-10-20

## Scope and provenance

The user authorized physical GPU runs. These measurements execute complete CPU and independent GPU paths on the same runtime at `d5b5d7d717288150e918068349db671c71a9655b`. They do not replace the [open contract ledger](../coverage/physics-status.md). API coverage remains 1303/1369 Implemented (95.18%), with 66 open declarations (52 Partial, 11 Blocked, 3 Unimplemented), 92 Excluded.

The host is Linux x64, Ryzen 7 5700X (16 logical processors), RTX 3090 Ti, NVIDIA 615.71.09, kernel 7.2.9-1-cachyos, .NET 10.0.101 Release. Hardware runs pin the NVIDIA Vulkan ICD; the no-GPU CPU control uses an unavailable ICD and SDL dummy. Per-process map sampling rejects lavapipe/SwiftShader. The observed GPU reached 41 C and 160.42 W; each accepted process retained NVML access and its kernel interval contained no Xid/fallen-off entry. This bounded observation establishes no general hardware-health claim. No power, clock or host configuration was changed.

All baseline cohorts share runtime DLL SHA-256 `5e2260f968e7c9206b66ac485ef2a64d119fac4c6a1983b69fd76c4bb1a3c703`. The later cohort changes only the two benchmark fixtures to fix their render resolution and record actual dimensions. Its test DLL is `68f6d3dc778bb5fbabf0b9c474fd20548f69e01a6491475b843c819440f4c2bb`. Source/PDB checks against the pinned revision cover 2414 runtime and 901 test compilation inputs, allowing exactly those two fixture files.

While measurements were running, main gained `0db48ca32615fb93bce4f2a9f0faef9d630a823a` rendering-thread changes. The baseline below remains qualified to its frozen runtime. Fresh current-main builds and small native CPU/GPU window checks establish integration of the fixture correction; they do not promote the old performance numbers to the newer rendering implementation.

The final formatted fixture build repeats both small window pairs at that revision, with 384 measured physical ticks and zero owner/all-thread physics/frame bytes. Its PDB checks match all 2415 runtime and 903 test compilation inputs. Earlier measured fixture bytes are retained separately; the final source differs only in whitespace from those fixture copies.

An initial ordinary package build omitted mesh/WebP native assets and failed NativeLibraryTests before the mass test. Rebuilding with the existing supported Electron2DMeshNativeLibrary/Electron2DWebPNativeLibrary development overrides supplied both files without changing any preceding DLL/PDB/native payload byte. Mesh SHA-256 is `ad5e246577a494d0e566a6e4b858c12dcfc5b29ebf9e3db57d4456a74e58e99f`; WebP is `211d381859e12a4bf5ca972e252a450338109cc976ef1a47a8345504b4deb0ac`. NativeLibraryTests then ran normally. This does not certify a newly packaged/published native bundle.

## Raw server bodies

The existing PhysicsMassPerformance fixture uses awake dynamic circles (radius 4, mass 1, friction .3, bounce 0), gravity 980, damping .05, three walls, four substeps and sixteen iterations at fixed 1/60 s. Every body is simulated; sleeping is disabled and no stage or publication diagnostic control is enabled. Each pair runs CPU then GPU in one process. Except the 128-body startup controls, rows use 240 warmup and 240 measured ticks.

| Trial | Backend | Bodies + walls | Mean / p50 / p95 / p99 / max step, ms | Managed owner / all threads |
| --- | --- | ---: | --- | --- |
| Headless 4096 | CPU | 4096 + 3 | 12.0473 / 11.7269 / 13.6283 / 14.3780 / 14.8333 | 0 / 0 |
| Headless 4096 | GPU | 4096 + 3 | 8.6587 / 8.5477 / 9.2073 / 10.0695 / 24.4721 | 0 / 0 |
| Headless 16384 A | CPU | 16384 + 3 | 54.1320 / 53.9171 / 57.5478 / 60.4965 / 64.7134 | 0 / 0 |
| Headless 16384 A | GPU | 16384 + 3 | 13.7826 / 13.7663 / 15.2785 / 16.1753 / 17.9246 | 0 / 0 |
| Headless 16384 B | CPU | 16384 + 3 | 75.4146 / 56.6275 / 195.2160 / 389.3410 / 462.1025 | 0 / 0 |
| Headless 16384 B | GPU | 16384 + 3 | 14.4809 / 14.3104 / 15.9488 / 17.2555 / 19.1436 | 0 / 0 |
| Window 16384 A | CPU | 16384 + 3 | 91.9312 / 58.5019 / 266.4205 / 382.3948 / 485.4264 | 0 / 0 |
| Window 16384 A | GPU | 16384 + 3 | 23.9462 / 21.5884 / 39.2666 / 42.2089 / 56.4826 | 0 / 0 |
| Window 16384 B | CPU | 16384 + 3 | 53.4338 / 53.0914 / 56.4001 / 57.7161 / 58.7638 | 0 / 0 |
| Window 16384 B | GPU | 16384 + 3 | 13.4520 / 13.3758 / 14.1709 / 14.5845 / 16.4195 | 0 / 0 |

Headless complete-step medians show a 3.92x advantage in A and 3.96x in B. The CPU B tail is much noisier; neither trial is discarded. These are sequential desktop trials rather than isolated statistical repetitions. The circle workload does not establish a joint/CCD/monitoring/shape-family performance advantage.

### Actual windows

Both numerical backends use the same GPU renderer, all 16384 MultiMesh instances, fixed 1152x800 internal pixels, MaxFPS=0 and at most one fixed physical tick per rendered frame. Disabled VSync was requested; actual VSync was Enabled. Both hosts observed a 1153x800 client. Frame times span FramePostDraw boundaries and include pose/instance publication and presentation; FPS is counted over wall time, independently of step medians.

| Trial | Backend | FPS / physics ticks per second | Frame mean / p50 / p95 / p99 / max, ms | Pose publication mean / p50, ms | Frame owner / all threads bytes |
| --- | --- | --- | --- | --- | --- |
| A | CPU | 9.8021 / 9.8021 | 102.0187 / 65.7850 / 294.2506 / 405.6898 / 524.4266 | 7.9198 / 5.7699 | 0 / 0 |
| A | GPU | 29.5454 / 29.5454 | 33.8462 / 27.9873 / 54.6173 / 62.8623 / 79.2162 | 6.9963 / 4.3940 | 0 / 0 |
| B | CPU | 16.6309 / 16.6309 | 60.1290 / 59.7517 / 62.9788 / 64.2982 / 65.9016 | 4.7261 / 4.6714 | 0 / 0 |
| B | GPU | 54.3506 / 54.3506 | 18.3990 / 18.2372 / 19.5879 / 19.9672 / 30.2021 | 3.2015 / 3.0891 | 0 / 0 |

Actual window throughput is 3.01x higher in A and 3.27x in B. Both PNG pairs were inspected and show the complete contained pile. Runtime checks validate finite/awake body state, energy growth below 2%, penetration below the unchanged .601-unit limit and bounded escape/speed. These invariants do not require identical trajectories.

GPU physics alone uploads/reads 55,680/180,480 bytes across 240 ticks. Window pose demand raises frame totals to 62,972,160/63,096,960 bytes, while the separate visual instance buffer carries 188,743,680 bytes. Frame GPU wait totals are 2828.1846 ms in A and 1793.7225 ms in B; these waits are already included in measured times. Reading every visible pose is an explicit presentation consumer, not a network snapshot.

## Complete monitored scene

PhysicsPipelinePerformance includes actual RigidBody scene nodes, four walls, entry/exit handlers, eight-point contact caps, interpolation, mass .2, radius 6, friction .25, bounce .35, fixed 144 Hz, four substeps, sixteen iterations and disabled sleeping. Both paths use identical inputs. The following non-rendered pairs have 1600 warmup ticks and 512/256 samples respectively.

| Scene bodies | Backend | Mean / p50 / p95 / p99 / max complete frame, ms | GPU wait p50 / p95 / p99 / max, ms | Managed owner / all threads |
| ---: | --- | --- | --- | --- |
| 1536 | CPU | 7.4603 / 7.1816 / 8.8291 / 10.0619 / 14.4406 | 0.0000 / 0.0000 / 0.0000 / 0.0000 | 0 / 0 |
| 1536 | GPU | 11.4840 / 11.2186 / 13.3331 / 15.3901 / 18.7885 | 4.4924 / 5.3959 / 6.5916 / 10.1525 | 0 / 0 |
| 16384 | CPU | 110.4520 / 110.1519 / 115.5513 / 118.4605 / 119.8551 | 0.0000 / 0.0000 / 0.0000 / 0.0000 | 0 / 0 |
| 16384 | GPU | 105.3905 / 107.2471 / 119.6810 / 132.8077 / 146.8422 | 36.0302 / 44.2362 / 59.7854 / 74.4526 | 0 / 0 |

The 1536-body GPU scene is slower. At 16384, the small median difference (110.1519 versus 107.2471 ms) and worse GPU tails do not establish a repeatable scene advantage. Do not generalize the raw-server speedup to this path.

At 16384 bodies the GPU uploads/reads 262,424/11,863,152.875 bytes per tick, with 19.1171875 submissions. Public scene pose/contact consumers require this state. Completion costs 46.1601 ms versus CPU 33.5181 ms: GPU pose/motion/fields take 11.0467 ms and contact capture/monitoring 33.6734 ms. The GPU wait median is 36.0302 ms. These costs identify publication/contact handling as required optimization work while preserving complete data and event semantics.

CPU/GPU setup costs are 4474.6882/3936.264 ms; retirement costs are 58377.2532/51144.2927 ms. The expensive scene teardown remains unresolved. It is outside warmed tick timing and is not hidden as solver cost.

## Native allocation request traffic

The calibrated Linux/glibc probe runs separate instrumented 1536-body scenes, 512 warmup and 128 measured frames. Calibration checks 4 allocations/131 requested bytes/3 frees plus worker/nested/stopped/failure/zero-size-reallocation behavior. Both managed counters remain zero. These instrumented timings and different warmup are not used to estimate overhead against ordinary timing trials.

| Backend | Successful requests | Requested bytes | Frees | Reallocations | Failures |
| --- | ---: | ---: | ---: | ---: | ---: |
| CPU | 0 | 0 | 0 | 0 | 0 |
| GPU | 34289 | 133904520 | 34292 | 0 | 0 |

The GPU records about 1.05 MB of libc requests per complete frame here. These are request totals across all process threads, not live/peak/retained memory. Direct mappings, device memory and private/non-interposed allocators are excluded; driver/CLR activity may contribute. Zero managed allocation must not be presented as zero native allocation.

## Fixed render-resolution regression

The old window fixture requested 1152x800, then asserted that the observed image must have exactly that size. Actual Wayland output was 1153x800 and the strict capture failed, including in a small 64-body reproduction. Both native benchmark fixtures now use ContentScaleMode.Viewport, ContentScaleSize=(1152,800) and Keep aspect. The native client stays compositor-owned; internal pixels and the number of rendered bodies stay fixed. The exact 1152x800 capture assertion remains. Raw reports additionally record ClientSize and RenderSize.

The corrected 64-body CPU/GPU window pilot and 128-body monitored window pair execute actual captures with strict zero managed bytes, along with both full 16384-body window trials. Current-main integration repeats the small pairs after the independent rendering-thread change. Ordinary package checks remain active; no NativeLibraryTests bypass, numerical feature reduction, fallback, sleep enablement or budget allowance was added.

## Reproduction and evidence

Build tests in Release with the ordinary native bundle or the supported development native overrides, then select the existing fixtures. Use a fresh process for each profile and pin the intended physical Vulkan ICD; do not carry software-driver or diagnostic controls into hardware timing.

All accepted processes set `DOTNET_TieredCompilation=0`, `SDL_GPU_DRIVER=vulkan`
and both Vulkan ICD variables to `/usr/share/vulkan/icd.d/nvidia_icd.json`.
Headless GPU runs use `SDL_VIDEODRIVER=offscreen`; native windows use `wayland`.
The CPU no-device control uses `dummy` and a missing ICD. Monitored profiles
explicitly use `ELECTRON2D_PHYSICS_PERFORMANCE_THREADED=0`, with `WINDOW=0/1`
under the same prefix. Neither `LD_PRELOAD` nor `LD_LIBRARY_PATH` is set in
ordinary timing processes; both are supplied only for the calibrated native probe.

| Fixture | Selector / settings |
| --- | --- |
| Raw headless/window | ELECTRON2D_TEST_PHYSICS_MASS=headless/window; ELECTRON2D_MASS_BACKEND=both; COUNT=16384, WARMUP=240, SAMPLES=240, OUTPUT under ELECTRON2D_MASS_ |
| Monitored scene | ELECTRON2D_TEST_PHYSICS_PERFORMANCE=1; ELECTRON2D_PHYSICS_PERFORMANCE_BACKEND=both; COUNT=1536/16384, WARMUP=1600, SAMPLES=512/256, OUTPUT under ELECTRON2D_PHYSICS_PERFORMANCE_ |
| Native request traffic | Same monitored selector; WARMUP=512, SAMPLES=128; calibrated LD_PRELOAD plus ELECTRON2D_PHYSICS_PERFORMANCE_NATIVE_ALLOCATIONS=1; see tools/physics/README.md |

Evidence: `bin/physics-rtx-current/2026-10-20/validation.json`, `pinned-source-proof.json`, per-profile reports/process maps/NVML/kernel intervals, PNG captures and current-main integration receipts. Thirteen accepted baseline processes contain 24 backend results and 4960 measured physical ticks at zero owner/all-thread managed bytes. Failed native-bundle and unfitted-window attempts remain separate evidence and are not accepted timings.

Full hardware shape/joint/CCD/extension/authoritative-network acceptance, new current-main mass requalification, all native allocator paths and the complete current test suite remain open. Priorities are scene publication/contact cost and scene retirement, preserving geometry, population, contact caps, event guarantees and strict warmed zero-managed-allocation gates.
