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
