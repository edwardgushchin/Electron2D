# PhysicsSandbox performance

Last updated: 2026-10-07

The sandbox has no collision-debug mode. The current profiler runs one native trial per scene. Historical results below retain the normal/debug modes that existed when those reports were produced.

## Measurement contract

The test-only [profiler](../../tests/Electron2D.Tests/PhysicsSandboxTests.Profile.cs) measures actual public-API stories and a native 1152×800 Wayland GPU window on Linux x64/.NET 10. `DOTNET_TieredCompilation=0` stabilizes the allocation checks. Each story has 1,600 fixed warmup ticks and 256 measured ticks; every native trial has 768 warmup frames and 192 measured frames. A moving scene pointer and periodic impulses keep queries and contacts active. Motorcycle demo drive and a bird shot start each corresponding trial. The final full run uses 1,024 stress bodies. The 60 FPS cap, presentation and scheduling waits remain enabled; this is observed window cadence rather than uncapped renderer throughput.

`GC.GetAllocatedBytesForCurrentThread()` brackets every measured fixed step and full scene/render-owner frame. Render callbacks are bracketed separately. The maximum on every frame, rather than a rounded mean, must be exactly zero. Report serialization, test instrumentation and scene transitions are outside those intervals. The test-only profile window requests no activation when shown; native mouse/keyboard events are disabled during measurement and their previous states are restored afterwards. Native GUI hover is cleared with queued native motion before warmup; pointer queries are moved through the scene API. Construction, new bodies, configuration edits, fresh native input-event construction, tooltips, readback and native/GPU allocations are not covered by the zero-byte result. Mouse-event creation remains an allocating runtime path. This result is a prepared simulation/UI budget, not a global zero-allocation guarantee for all interactions.

## Smash allocation fixes

Smash creates 9,600 fragments by default and permits 65,536 real bodies plus its block. World preparation previously reserved whole-world buffers in each dormant solver slot; it now uses bounded 16-body/32-contact/four-joint initial capacities and retains each island's high-water storage. Uninstrumented membership additions avoid repeated monitor scans, and departure cleanup visits configured contact subjects. Fragment parents, shapes and materials share 128-body groups, bounding sibling checks and resource event fan-out.

A native trace identified a 5,088-byte growth when a 16-body/20-contact island first slept; the small dormant budget covers that observed topology. The large diagnostic then exposed about 3.5 MB per step/frame. EventPipe allocation sampling identified B2BodyMoveEvent arrays: exact-size growth followed each increase in awake body count. World construction now reserves those events for the rounded body capacity. A separate dense-query regression exceeds the former 16-pairs-per-moving-proxy estimate; broad phase also retains peak requested/overflow demand for arena reuse. Cold larger topologies still require explicit preparation/warmup outside the measurement interval.

The sandbox temporarily permits at most one fixed interval per rendered frame and restores the previous engine budget on disposal. Overload slows simulation rather than monopolizing frames with several catch-up intervals; nominal 60 Hz does not guarantee real-time simulation under overload.

## Current Smash measurements

Release/Linux x64/Wayland GPU, tiered compilation disabled, a 60 FPS cap and one maximum physics step per frame. The 9,600-fragment fixed trial uses 1,600 warmup ticks/256 samples; its separate native trial uses 768 warmup frames/192 samples. The 65,536-fragment fixed trial uses 384 warmup ticks/256 samples. A 128-tick large warmup was insufficient: it included a later scratch/cache growth of 3,468,184 bytes across its measured interval. Those cold results are not a zero-byte pass.

| Fragments + block | Fixed mean / p95 ms | Native FPS | Native render mean ms | Fixed bytes / maximum owner-frame bytes |
| --- | ---: | ---: | ---: | ---: |
| 9,600 + 1 | 29.947 / 34.181 | 20.0 | 10.348 | 0 / 0 |
| 65,536 + 1 | 303.356 / 337.070 | 2.1 | 127.058 | 0 / 0 |

Evidence is split into `profile-Release-smash-acceptance.json` (fixed default), `profile-Release-smash-window-acceptance.json` (native default), `profile-Release-smash-max-prepared.json` (fixed maximum), and `profile-Release-smash-max-window.json` (native maximum). Native and fixed trials have independent scene lifetimes/warmup. The maximum is an overload test, not a usable 60 FPS promise. The large fixed trial ran alongside functional verification, so its timing is descriptive rather than an isolated throughput result. Rendering/readback, fresh input, configuration and larger unprepared topologies keep the limits described above.

Reproduce with the existing profile switch, `ELECTRON2D_SANDBOX_PROFILE_SCENE=11`, `ELECTRON2D_SANDBOX_SMASH_COUNT=9600` or `65536`, and a distinct profile tag. `ELECTRON2D_SANDBOX_PROFILE_HEADLESS=1` selects fixed-only; `ELECTRON2D_SANDBOX_PROFILE_NATIVE_ONLY=1` selects native-only. The maximum fixed acceptance uses `ELECTRON2D_SANDBOX_PROFILE_WARMUP=384`; native maximum uses `ELECTRON2D_SANDBOX_PROFILE_NATIVE_WARMUP=128`.

## Previous scene drawing

After removing the diagnostic layer, a focused Release run on runtime `92dab95f` measured the warehouse at 0.109 ms per fixed tick and 58.8 FPS, and 1,024 active stress particles at 1.801 ms per fixed tick and 49.5 FPS. Both cases passed 256 fixed ticks and 192 native frames with exactly zero maximum managed frame and render bytes after the standard warmup. Evidence: `bin/physics-sandbox/profile-Release-no-debug.json`. This checks the revised single-trial profiler and the current scene/UI drawing; the other nine native performance profiles and the long settling gate were not rerun for this removal. All eleven scenes passed interactive capture checks in both renderers.

## Historical interface revision result

The revised field, inspector, selection labels and clipped scene/debug drawing passed all eleven fixed profiles and all 22 normal/debug native trials, with exactly zero maximum prepared owner-thread frame bytes. This full run uses tracked runtime `39f16075`, Release, tiered compilation disabled, 1,024 stress particles and the standard warmup/sample counts above. Evidence: `bin/physics-sandbox/profile-Release-ui-acceptance.json`. Native mouse/keyboard input is suppressed only by the profiler; the interactive native suite still exercises dropdown, tabs, sliders, dragging, pause/step and slingshot release in both renderers.

| Story | Bodies | Fixed mean ms | Normal / debug FPS | Normal / debug render ms | Fixed / maximum frame bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| Collision warehouse | 72 | 0.204 | 57.8 / 57.5 | 1.64 / 2.03 | 0 / 0 |
| Marble delivery | 8 | 0.027 | 57.0 / 57.7 | 4.51 / 2.29 | 0 / 0 |
| Clockwork playground | 4 | 0.010 | 58.2 / 57.8 | 2.31 / 1.92 | 0 / 0 |
| Gravity garden | 18 | 0.038 | 58.1 / 57.7 | 1.74 / 2.27 | 0 / 0 |
| Rooftop courier | 2 | 0.029 | 57.4 / 58.4 | 3.81 / 1.69 | 0 / 0 |
| Radar rescue | 1 | 0.044 | 58.3 / 58.4 | 1.62 / 1.56 | 0 / 0 |
| Orbital tug | 2 | 0.010 | 58.1 / 58.3 | 1.58 / 1.51 | 0 / 0 |
| Shape atelier | 7 | 0.016 | 58.7 / 56.2 | 1.61 / 5.35 | 0 / 0 |
| Physics stress test | 1024 | 5.902 | 9.5 / 10.6 | 46.72 / 63.95 | 0 / 0 |
| Gravity Defied | 3 | 0.018 | 11.6 / 11.1 | 80.72 / 84.74 | 0 / 0 |
| Angry birds | 22 | 0.065 | 12.3 / 11.7 | 75.75 / 79.71 | 0 / 0 |

The last six native trials dropped to roughly 9–12 FPS across the stress tank, three-body motorcycle and bird scene, while render durations included 47–85 ms presentation waits. Desktop load and window visibility were uncontrolled; these cadence results do not isolate a physics regression. The allocation gate passed, and the fixed-step column measures simulation separately. No guaranteed 60 FPS is claimed from this run.

After integrating tracked runtime `458a0f74`, a focused 1,024-body follow-up measured 2.013 ms fixed mean, 57.8 / 58.1 normal/debug FPS and 3.99 / 7.60 ms render mean. Fixed ticks and both native modes again passed the exact zero-byte gate. Evidence: `bin/physics-sandbox/profile-Release-ui-latest.json`. This follow-up supersedes the older stress cadence for the integrated runtime; host load and presentation remain outside an absolute FPS guarantee.

Numeric readouts prepare the same integer-aligned glyph path used by their changing values. The selection label counter-scales only its draw commands to retain a clear 13-pixel font; physical transforms retain unit scale. An earlier run was rejected after real input added four garden bodies during measurement. The profiler now suppresses that external input and restores its prior event states.

## Historical Release result

All 11 fixed-step cases and all 22 normal/debug trials passed the zero-byte gate. Raw local evidence: `bin/physics-sandbox/profile-Release-final.json` (ignored/generated, not shipped in the runtime).

| Story | Bodies | Fixed mean / p95 ms | Normal / debug FPS | Normal / debug render ms | Fixed / maximum frame bytes | Construction MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Collision warehouse | 72 | 0.196 / 0.238 | 57.1 / 58.0 | 3.49 / 2.64 | 0 / 0 | 20.18 |
| Marble delivery | 8 | 0.032 / 0.043 | 58.6 / 58.1 | 1.69 / 1.84 | 0 / 0 | 1.14 |
| Clockwork playground | 4 | 0.010 / 0.011 | 58.8 / 58.2 | 1.62 / 1.84 | 0 / 0 | 0.63 |
| Gravity garden | 18 | 0.055 / 0.092 | 58.0 / 57.3 | 1.79 / 2.75 | 0 / 0 | 2.34 |
| Rooftop courier | 2 | 0.029 / 0.035 | 58.5 / 57.4 | 1.69 / 3.00 | 0 / 0 | 0.66 |
| Radar rescue | 1 | 0.044 / 0.052 | 57.7 / 58.8 | 1.97 / 1.62 | 0 / 0 | 0.57 |
| Orbital tug | 2 | 0.012 / 0.013 | 58.5 / 56.6 | 1.72 / 2.42 | 0 / 0 | 0.39 |
| Shape atelier | 7 | 0.038 / 0.053 | 58.2 / 57.6 | 1.82 / 2.05 | 0 / 0 | 0.90 |
| Physics stress test | 1024 | 11.568 / 21.657 | 24.0 / 41.9 | 5.69 / 9.17 | 0 / 0 | 61.96 |
| Gravity Defied | 3 | 0.022 / 0.026 | 58.3 / 58.2 | 1.79 / 1.78 | 0 / 0 | 0.65 |
| Angry birds | 22 | 0.081 / 0.105 | 58.5 / 57.7 | 1.60 / 1.87 | 0 / 0 | 2.22 |

The final stress row uses 1,024 always-awake real circles. The preceding 512-body full run measured 3.689 ms/tick and 57.9 FPS with debug off/on, also at zero frame bytes. Construction allocates deliberately: contact caps, graph/overlap storage and dormant solver sets are prepared and retained until world disposal. Four contacts per rounded body-capacity slot is the prepared graph ceiling; denser unprepared topology can grow buffers. Dormant storage follows bodies that can sleep; the stress particles disable automatic sleeping. Larger sleepable populations increase startup/storage cost, so this table also reports construction bytes.

## Historical low FPS investigation

The user's 1,024-body screenshot reported roughly 2–6 FPS with debug off. Ordinary unoptimized Debug measured 54.4 ms per fixed tick even after the first arithmetic changes. The example now opts into compiler/JIT optimization for its consumer and runtime reference in both configurations; Debug symbols and backend assertions remain enabled. Other projects retain their own build configuration.

A controlled early Release 1,024-body run measured 25.0 FPS normally and 11.6 FPS with debug, with about 14 ms fixed ticks. That run still had late contact-object and render-buffer growth. The first optimized iteration reached 55.5 / 33.9 FPS and 7.5 ms/tick; these are intermediate measurements with a shorter 240-tick fixed warmup, not the final acceptance sample.

A later full-warmup maximum-load attempt regressed to 9.8 debug FPS despite zero managed frame bytes. Thus the allocation gate alone did not establish performance. The final scene-owner runtime retains its existing weak registration, preserving release/attachment guards while avoiding repeated locked registry lookups in state access. Acquiring a direct body view now prepares its requested body rather than the entire space; whole-space queries still prepare all fixtures separately. Collecting N initial body views therefore avoids N whole-world scans. Debug draws each dynamic contact pair once and removes redundant transform commands from the batched stress path. A focused 1,024-body Release run with default tiered compilation after the weak-owner/pair change measured 55.4 / 56.6 FPS, 7.564 ms fixed mean (8.896 ms p95), and 3.575 / 6.269 ms render mean. All 256 fixed ticks and 192 normal plus 192 debug sampled frames had exactly zero owner-thread managed bytes (`profile-Release-stress1024-cache.json`).

The final three-minute Release settling run with tiered compilation disabled recorded 33.5–49.3 FPS in fifteen-second windows. Settled mean was 40.6 FPS at 60–90 seconds and final mean 43.1 FPS at 150–180 seconds, so the cadence-retention gate passed. After the initial window, physics stayed approximately 60 Hz and the impact counter stayed at 8,561. Evidence: `bin/physics-sandbox/long-stress-Release.json`. An optimized Debug run retained its relative cadence too, but still had lower throughput and host-load spikes; 60 render FPS at 1,024 bodies is not guaranteed in Debug.

The original warehouse Debug run measured 39.9 / 22.7 FPS and 15,304 managed bytes per debug frame. The final steady budget is exactly zero in the table above. Renderer readout/static-grid costs, contact-owner scans and redundant empty integration synchronization were removed. Query/slide results fill caller-owned buffers, direct views are reused, numeric HUD formatting stays on the stack, and four-lane solver arithmetic uses Vector128 operations. Contact/island slots, sleeping storage and contact/body bit sets are retained/prepared. Debug normals/markers use batched lines and prepare their configured contact ceiling once using clipped-off segments; a new maximum contact count cannot then force late growth. Temporary allocation listeners and per-node timing probes were removed.

## Reproduce and limits

```bash
DOTNET_TieredCompilation=0 ELECTRON2D_TEST_PHYSICS_SANDBOX_PROFILE=1 ELECTRON2D_SANDBOX_PROFILE_TAG=verified SDL_VIDEODRIVER=wayland dotnet run --project tests/Electron2D.Tests -c Release
```

Use `ELECTRON2D_SANDBOX_PROFILE_SCENE=8 ELECTRON2D_SANDBOX_STRESS_COUNT=1024` for the maximum-load case. Use `-c Debug -p:Optimize=true` for the optimized Debug runtime used by this example. JSON records optimization state, sample counts, body count, mean/p95 durations and exact maximum managed bytes.

`ELECTRON2D_SANDBOX_PROFILE_LONG=1` selects a three-minute native settling run at 1,024 always-awake bodies with debug enabled. Fifteen-second windows record cadence, render time, physics frequency, contact-event count and managed heap. The final 150–180-second mean must retain at least 80% of the settled 60–90-second mean; initial filling/cold work is excluded from that relative comparison. The long-run instrumentation itself allocates at interval boundaries and is not the zero-byte profiler. Concurrent desktop/compiler work affects these real window measurements; no absolute FPS assertion is imposed on arbitrary hosts.

The focused sandbox checks cover 11 finite stories, all 33 actions, world defaults, native sliders, pause/step, grabbing, deferred parcel collection, motorcycle joints and native slingshot release. The physical runtime audit exercised 38 existing suites. GPU and compatibility capture runs cover all 11 normal/debug views and native controls on Linux Wayland. Other platforms, sustained native input allocation, native/GPU memory and human game-feel acceptance remain separate gates. The game cap and incomplete physics capabilities are listed in the [sandbox map](physics-sandbox.md).
