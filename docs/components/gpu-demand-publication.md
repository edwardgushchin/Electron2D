# GPU publication for current consumers

Last updated: 2026-10-10

## Scope and runtime flow

[PhysicsSpace](../classes/PhysicsSpace.md) keeps the independent GPU solver resident.
After a completed interval, it advances the body-cache epoch. An unobserved raw
server body stays on the device; an explicit pose/velocity/state getter selects that
body once and reuses the snapshot within the same epoch. Detachment captures its
latest device state before ending the attachment.

Pure scene worlds retain the device comparison/history stream and download only
changed bodies. Scene transforms, velocities, fields and sleep notifications are
real completion consumers. When a world mixes scene and raw objects, one selected
batch includes scene colliders plus raw kinematic bodies, configured contact
receivers and retained live body views. Its reusable staging arrays grow with the
consumer count. Raw bodies without those consumers do not acquire a current host
mirror. Disposed views leave the completion set on the next interval.

Force/integration and synchronization callbacks preserve the existing shared
pre-step/complete publication. Their common ordered delivery lets an earlier
callback enable a later receiver in the same interval, including bodies that were
active before becoming asleep. Queued force eligibility also requires the existing
pre-step activity snapshot. The callback paths still use the global change stream;
they are measured consumers and remain an optimization boundary.

Acquiring a GPU direct-body view refreshes that body's resolved field cache. Live
raw views participate in subsequent selected completion, so fields and velocities
remain fresh. Public queries continue to search resident geometry. Shape/contact
reports and statistics keep their existing explicit reads. A started submission
failure keeps the world failed and never replays it on CPU.

Portable capture already batches the bound GPU identities. Apply preparation now
also batches the local state needed for validation and rollback, avoiding a series
of individual getters after an unobserved interval. Portable runtime field capture
uses the current accepted device snapshot. These are explicit snapshot consumers,
not mandatory per-step transfers.

## Explicit pose batches

`PhysicsServer.BodyGetTransform(space, bodies, transforms)` reads only the caller's selected body poses. All RIDs must be live bodies attached to that space. Complete validation precedes projection; results are staged before committing to the caller's span. Input order and duplicates are retained, unused destination elements stay unchanged, and empty input still validates space access. Scene bodies use presentation transforms and static raw bodies use authored transforms, matching the scalar getter exactly.

Dynamic raw GPU bodies use one gather, a 16-byte position/basis record per request, eight status bytes and one fence. Pending commands/mass preparation retain their own explicit costs. The existing 64-byte full-state read remains available to velocity, field, sleep and snapshot consumers. An independent epoch-qualified compact cache serves subsequent scalar poses without making those other fields current. Checkpoints preserve its epoch/state; portable motion application invalidates it. Retained space scratch grows only with requested capacity, and body references are cleared after every attempt.

[PhysicsBodyTransformTests](../../tests/Electron2D.Tests/PhysicsBodyTransformTests.cs) exercises the same CPU/GPU API on 128 raw bodies and one scene body: duplicates/order/tails, wrong/stale/non-body RIDs, wrong world, short output, empty/off-owner/solver guards, authored edits, local checkpoint restore and portable application. A three-dynamic-body request downloads exactly 56 bytes with one submission. A 513-entry duplicate sequence also works when it exceeds resident population. After warming both the operation and allocation counters, 64 complete ticks plus pose batches allocate 0/0 owner/all-thread managed bytes. CPU execution is also included in the child process with graphics unavailable. Terminal GPU failure still rejects subsequent batches and leaves their destination intact.

The real-window mass consumer now uses this public batch once per fixed tick, then updates every MultiMesh instance. Physics workload, body count, solver settings and invariant checks remain identical between implementations. Whole physics, pose/instance publication and whole-frame time are separate measurements.

## Executable checks and limits

[PhysicsGPUDemandPublicationTests](../../tests/Electron2D.Tests/PhysicsGPUDemandPublicationTests.cs)
checks 512 raw bodies and one scene body mixed with 512 raw bodies, using 64 warmup
and 64 measured complete steps. Both intervals allocate 0/0 owner/all-thread managed
bytes. The raw interval downloads 72 status bytes per step; the mixed interval
requires 216 bytes per step. One raw-body getter downloads at most 128 bytes and
reuses its cached state. Tests exercise fields, live/disposed views, kinematic
completion, latest-state detachment, intermediate authored/getter caches and
transitions into callbacks that register later receivers. Prescribed constant
motion/fields use a .02 scene-unit tolerance for float integration; byte/counter
limits are exact bounds independent of the unobserved body count.

The existing public-world fixture uses 1024/4096 active circles plus a floor, four
substeps, 768 warmup steps and 128 measured steps. The current worktree run reports:

| Bodies | Backend | Full step p50/p95/p99, ms | Managed owner/all bytes | GPU upload/readback bytes | Mean wait, ms |
| ---: | --- | --- | --- | --- | ---: |
| 1024 | CPU | 2.9647 / 4.6665 / 5.3726 | 0 / 0 | 0 / 0 | 0 |
| 1024 | GPU | 7.4544 / 8.5092 / 9.6916 | 0 / 0 | 232 / 752 | 3.4576 |
| 4096 | CPU | 7.3726 / 9.0628 / 9.6320 | 0 / 0 | 0 / 0 | 0 |
| 4096 | GPU | 10.0409 / 11.4136 / 11.7325 | 0 / 0 | 232 / 752 | 4.3770 |

GPU uniforms are 61449/63290 bytes per sample at 1024/4096 bodies. The prior ownership
run downloaded 82680/328440 bytes per GPU sample; current byte reductions remove
unobserved state traffic. Different runs have timing variation, so these numbers do
not establish a speed improvement. GPU advantage remains unachieved in this fixture.
Logs and hashes stay in `bin/physics-demand-publication/2026-10-10/`.

Pure-scene publication, force callbacks, reports, kinematic motion, joints and
checkpoint/portable/network scenarios retain separate executable checks. Complete
registered extensions, native allocation, foreign platforms, broader target-scene
performance and real-window FPS remain open. See [ADR 0054](../decisions/physics-backends.md#adr-0054).

## Massive independent contacts

The same compiled worktree also ran the existing full public API fixture at
4096/16384/65536 bodies, with respectively 2048/8192/32768 independent colliding
pairs. Every dynamic pose and linear velocity is reset before a complete 1/60 s
step; both implementations use four substeps, 16 iterations, disabled sleeping,
64 warmup and 64 measured samples. Profiling was enabled in both paths. Every
measured interval allocated 0/0 owner/all-thread managed bytes.

| Bodies/run | Backend | Whole reset + step p50/p95/p99, ms | Reset / full step p50, ms |
| --- | --- | --- | --- |
| 4096 | CPU | 4.9217 / 6.1415 / 7.7590 | .9594 / 3.9943 |
| 4096 | GPU | 4.2792 / 5.2147 / 6.6654 | .5825 / 3.6916 |
| 16384 | CPU | 16.1128 / 18.6156 / 18.7934 | 3.4445 / 12.6008 |
| 16384 | GPU | 20.9738 / 69.8052 / 135.9828 | 5.7080 / 12.9461 |
| 65536 first | CPU | 97.4419 / 459.0203 / 524.2340 | 15.4291 / 61.6800 |
| 65536 first | GPU | 36.4979 / 49.0793 / 55.4215 | 9.9637 / 25.9112 |
| 65536 repeat | CPU | 71.4259 / 74.2408 / 76.4159 | 16.2338 / 55.1572 |
| 65536 repeat | GPU | 27.4601 / 32.5173 / 33.1880 | 9.0768 / 18.2922 |

The isolated 65536 repeat confirms a measurable advantage in this actual contact
fixture: full GPU step p50 is 18.2922 ms versus CPU 55.1572 ms; whole reset+step p50
is 27.4601 versus 71.4259 ms. GPU uploads/downloads are 5767400/760 bytes per tick,
with 18 submissions, zero change-stream publications and 5.3042 ms mean wait.
Uploads contain the explicitly reset 32768 dynamic bodies; no object-count or
solver-feature reduction supplies the speed difference. The first run's CPU tail
and the 16384 GPU tail remain recorded rather than replaced by the faster repeat.
This is workload-specific evidence, not general GPU superiority or window FPS.
Logs are `massive-contact-profile.log` and `massive-contact-repeat.log` in the
current evidence directory. Remaining GPU preparation and wait costs still need
work alongside complete backend extension and platform acceptance.

Portable GPU-to-GPU capture/encode/decode/apply of 17 objects retains a 6428-byte
payload and 0/0 warmed managed bytes, with 9 submissions and 3584/3600 upload/readback
bytes per cycle. CPU-to-GPU uses 8 submissions and 3304/2504 bytes. The temporary
implementation refreshed fields during internal view construction and produced
one read per contact receiver; moving that refresh to the guarded public acquisition
boundary restored batched internal restore behavior. Local state capture before
apply adds one explicit validation batch rather than individual body reads.

## Real-window pose batch measurements

The same Release assembly ran the dense circle pile on CPU then GPU at 4096 and
65536 dynamic circles plus three static walls. Every body remained active and
sleeping was disabled. Both paths use four substeps, 16 solver iterations,
240 warmup and 64 measured fixed 1/60 s ticks. GPU rendering runs in a real
1152 x 800 window; requested disabled VSync reports actual `Enabled`. Engine
render-frame cap is zero and catch-up budget is one physics tick per frame.
FPS counts actual rendered frames, including frames with no physics interval;
it is not the reciprocal of physics p50.

Host: Linux x64, .NET SDK 10.0.101, NVIDIA GeForce RTX 3090 Ti, driver 615.71.09.
Both implementations share the exact compiled source and complete invariant checks.

| Dynamic bodies | Backend | Whole physics p50/p95/p99, ms | Actual FPS | Whole frame p50/p95/p99, ms | Pose + instance publication p50/p95/p99, ms |
| ---: | --- | --- | ---: | --- | --- |
| 4096 | CPU | 13.9061 / 37.8099 / 45.6219 | 91.3209 | 13.8938 / 33.7702 / 43.4383 | 0.7706 / 1.4239 / 1.5183 |
| 4096 | GPU | 17.8361 / 23.2938 / 25.4798 | 56.3675 | 19.0872 / 27.3357 / 30.2950 | 1.0629 / 2.5651 / 3.9838 |
| 65536 | CPU | 250.9162 / 263.3012 / 266.4266 | 3.6823 | 270.9593 / 283.1005 / 287.3510 | 13.8081 / 14.4904 / 15.2072 |
| 65536 | GPU | 49.1659 / 51.7791 / 55.4664 | 15.0486 | 65.6778 / 70.1347 / 72.0850 | 10.6967 / 11.6735 / 13.4517 |

At 65536 bodies GPU whole-step p50 is 5.10 times faster and actual window FPS
is 4.09 times higher. The 4096 case remains unfavorable to GPU; the table retains
its worse p50 and FPS. Every warmed physics interval and complete rendered sample
allocated 0/0 owner/all-thread managed bytes. Final penetration is
.1793/.1613 scene units at 4096 and .2703/.2965 at 65536 for CPU/GPU, below
the .601 configured limit. No body escaped, all states stayed finite and energy
remained below initial energy plus the permitted 2% numerical bound.

GPU physics alone uploads/downloads 232/752 bytes per measured tick. Including
pose publication, total GPU-store frame upload/readback is 65776/66296 bytes
at 4096 and 1048816/1049336 bytes at 65536, with 18 submissions per measured tick.
The compact pose request contributes 8 + 16N bytes in each direction and one
submission; pending edits retain their separate costs. Mean GPU physics wait is
7.3348/31.2167 ms at 4096/65536; complete-frame GPU-store wait is 7.6616/31.5166 ms.
Visual MultiMesh buffer writes are separate renderer traffic. Final whole-world
invariant validation happens after frame/time counters stop and explicitly reads
complete motion through public getters; those diagnostic reads are not render costs.

Raw reports/logs and actual PNG captures are in
`bin/physics-body-transforms/2026-10-10/window-4096/` and `window-65536/`.
GPU captures were visually inspected: every instance is drawn as the dense pile.
This establishes this host/workload's full-step and real-window advantage; native
allocations, other device/platform profiles and complete registered extensions
remain open.

The same dense 65536-body pile also completed separate CPU/GPU headless processes,
with identical 240/64 warmup/sample counts and 4/16 substeps/iterations:

| Backend | Full headless physics p50/p95/p99, ms | Managed owner/all bytes | Mean GPU wait, ms |
| --- | --- | --- | ---: |
| CPU | 268.3706 / 413.3898 / 904.3978 | 0 / 0 | 0 |
| GPU | 43.2887 / 45.4560 / 46.0632 | 0 / 0 | 22.1958 |

The CPU tail is retained as measured; these separate runs do not establish a cause
for its variation. GPU traffic remains 232/752 upload/readback bytes per physics
tick, without render pose demand. Headless invariant limits also pass. The first
combined headless attempt ended with process status 143 before writing a report;
only the completed separate-process reports are used. Artifacts are in
`headless-65536/`, `headless-cpu-65536.log` and `headless-gpu-65536.log` beneath the
same evidence directory.
