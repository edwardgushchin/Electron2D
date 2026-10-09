# Changed GPU body publication

Last updated: 2026-10-09

## Contract and consumer

The independent GPU store's `ReadChanges` compares current body snapshots against
its last publication on the device. Its single consumer receives the latest pose,
linear/angular velocity including surface motion, resolved fields, mode and exposed
sleep/integration policy. The first call returns every live body. A retained caller
span must cover `BodySlotCount`; validation occurs before flushing pending edits.
Only the returned prefix is written, and ordering is unspecified.

Each 80-byte record includes slot, generation, previous live generation, an alive
flag and a 64-byte snapshot. Removal produces a tombstone once. Replacing a body in
the same slot reports both generations even when the pose is unchanged. Unpublished
intermediate edits are coalesced: this is latest-state publication, not a lifecycle
event log, network identity or replay checkpoint. Internal sleep clocks and solver
bookkeeping flags alone do not trigger a record. Explicit `Read` remains available
for fresh complete selected snapshots and does not consume the change stream.

The [Smash developer window](gpu-smash-preview.md) consumes this stream, updates
retained CPU display/picking records and rewrites only changed instance records.
An empty visible update does not upload another MultiMesh buffer. Its retained
display state is needed by CPU pointer picking and ordinary MultiMesh rendering;
it is not fed back into the solver. [Public PhysicsSpace selection and scene-body publication](physics-backends.md) now also consume this stream; networking remains open.

## Storage and synchronization

No CPU comparison history or body-request list is required. Optional device history
and compact-output capacity each cost 80 bytes per reserved slot, plus eight status
bytes. Download scratch reserves `8 + 80 * BodySlotCount`, rounded by the existing
buffer allocator; upload scratch is eight bytes. History grows through device copy.
At 65,536 slots device payload is 10 MiB plus eight bytes; transfer capacity is
reported by the executable check. These buffers are created only for this consumer.

Each publication uploads eight reset bytes and a 16-byte uniform. One wait reads
the count and error status (eight bytes). Nonempty publication needs a second wait
to download exactly `80 * changedCount` bytes. The count determines the copy length;
there is no full-state readback hidden in the first wait. A dense stream can cost
more than explicit selected reads (64 bytes/body and one wait). Both paths remain
available, and measurements must state their changed population. History advances
on submission; any subsequent failure invalidates the store rather than silently
consuming an undelivered batch or replaying a physics step.

## Verification

`ELECTRON2D_TEST_GPU_CHANGES=1` selects `GPUPhysicsChangeTests`. It checks initial,
unchanged, selected-read interleaving, sleep/wake, static surface velocity, removal,
identical-pose replacement, growth, thread/disposal/failure boundaries and untouched
destination tails. Over 180 full solved ticks it reconstructs a consumer's state
and compares it exactly with an explicit read of the same device state; this is
publication validation, not a CPU/GPU bitwise physics requirement.

The benchmark compares both publication paths after the same integration step,
alternates reader order, and measures 0, 1, 256 and 65,536 changed bodies out of
65,536. It uses 128 warmups and 128 samples per population; reports p50/p95/p99,
included fence waits, upload/readback, managed allocation and scratch capacities.
Simulation time is outside the reader timings.

Linux/.NET 10 Release, Vulkan/NVIDIA GeForce RTX 3090 Ti, 2026-10-09;
`/tmp/e2d-changes-focused.log` (source based on `835edafa` plus this change):

| Changed / 65,536 | Full read p50/p95/p99 ms | Changes p50/p95/p99 ms | Full / changes mean wait ms | Changes download |
| ---: | --- | --- | --- | ---: |
| 0 | 1.2377 / 1.4126 / 1.5101 | 0.0741 / 0.3169 / 0.3337 | 0.5593 / 0.0980 | 8 B |
| 1 | 1.0757 / 1.2523 / 1.2905 | 0.0976 / 0.1968 / 0.4131 | 0.5172 / 0.0966 | 88 B |
| 256 | 1.0663 / 1.2358 / 1.2870 | 0.0944 / 0.1337 / 0.4120 | 0.5156 / 0.0871 | 20,488 B |
| 65,536 | 1.4061 / 1.6128 / 1.6974 | 0.9827 / 1.3932 / 1.4811 | 0.5652 / 0.6330 | 5,242,888 B |

Full reads upload 1,048,584 and download 4,194,312 bytes per call. Changed publication
uploads eight bytes regardless of population. Device/transfer payload capacities
are 10,485,768 / 8,388,616 bytes. All four warmed workloads measured zero owner-thread
and all-thread managed bytes for integration plus both readers. Dense publication
downloads 25% more record data, but avoiding the request upload/validation helped
in this run; it is not a universal dense-workload advantage. These are same-revision
publication measurements, not full CPU/GPU solver or window-FPS comparisons. Native
allocations and other platforms remain unmeasured.

The 9,600-fragment rendered Smash smoke run passed impact propagation, pause/step,
selection/clear, drag, kick, rebuild, shockwave and population replacement using
the new stream (`/tmp/e2d-changes-smash.log`). It observed 27,109 peak contacts and
9,600 moved fragments. Impact and final captures were inspected: retained sleeping
wall regions and updated moving fragments render together without missing instances.
The impact capture shows 29 FPS and a 53.68 ms sampled simulation step, so this does
not establish the overall frame-rate target or a solver speedup. Readback in that
capture was 0.19 ms/234 KiB; sustained window-FPS comparison remains open.

The complete `ELECTRON2D_TEST_GPU_PHYSICS=1` suite passed on the final implementation
(`/tmp/e2d-changes-full-gpu.log`), including the policy-only change checks and both
renderer lifetime modes. Release build, unchanged compiled API coverage, wiki
tests/generation/check, runtime shader generation checks and scoped whitespace
checks also passed. No new public backend-selection capability is claimed.
