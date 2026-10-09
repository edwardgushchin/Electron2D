# Resident GPU collision exceptions

Last updated: 2026-10-09

## Implemented boundary

GPUPhysicsBodyStore now retains directed live-body exceptions and combines them
with joint collision vetoes in its device pair filter. This implements the live
solver part of [ADR 0063](../decisions/physics.md#adr-0063) and the independent
joint-contribution rule of [ADR 0087](../decisions/physics-joints.md#adr-0087).
Public PhysicsServer worlds still use CPU. Mapping their RID lists, detached or
stale targets, body motion queries and event publication to this independent store
remains part of the unfinished public-world adapter. Public declarations and
their coverage states are unchanged by this internal implementation.

`SetCollisionException(owner, target, enabled)` validates both live generation-
qualified handles before mutation. Each directed entry is independent; either
direction suppresses solid contact. Duplicate additions and absent removals do
nothing. `HasCollisionException` inspects only that directed authored entry.
Several joints and both directed exceptions may veto the same pair. Removing one
contribution never removes another. Sensors bypass body vetoes and preserve their
own receiver masks. Self entries are valid authored records but create no pair.

Each body retains an incident list. Removing either endpoint drops its incoming,
outgoing and self device entries in time proportional to its incident edges,
before the slot can be reused. This live attachment cleanup does not replace the
public RID registry's retained stale-target entries. The future adapter must
resolve that registry again on attachment and retain its insertion order.

Actual edits wake both endpoints and invalidate the pair cache. The device sleep
graph also wakes prior connected neighbours after an edge is removed. The next
contact solve retires filtered contact rows/history; restoring the pair starts
with a fresh contact impulse. Discrete and swept broad phase use the same filter,
so both ray and full-shape CCD skip excluded obstacles. There is no separate CPU
collision filter or CCD exception scan.

## Device work and transfers

Exception changes coalesce in retained CPU staging. The existing joint-edit
submission scatters changed joints and exceptions, generates canonical
generation-qualified source pairs, clears the hash slots and builds the shared
device hash table. Parallel insertions reference immutable pair records from the
previous dispatch, avoiding partially published multiword keys. The spatial
kernel checks the pair and both current body generations. Unchanged steps neither
upload exception records nor rebuild the table.

| Storage or transfer | Payload and purpose |
| --- | --- |
| CPU body metadata | 88 bytes per slot after live integration policies, including one incident-exception list head; no solved poses or velocities. |
| CPU exception slot | 40 bytes for authored endpoint indices/generations, incident/free links and journal flags, plus the directed-key dictionary and retained dirty-index storage. |
| Exception scatter | 32 bytes per changed slot; body wake edits share their existing coalesced 144-byte command. |
| Device authored exception | 16 bytes per retained exception slot. Growth preserves records GPU-to-GPU. |
| Device canonical filter source | 16 bytes per retained joint or exception contribution, including both body generations. Rebuilt on the GPU. |
| Device hash table | 4 bytes per hash slot, power-of-two capacity at least twice combined joint/exception high-water count. Duplicate sources collapse at lookup. |
| Changed batch status | 8-byte reset upload and 8-byte error readback, shared with joint edits at one fence. |
| Uniforms | 32 bytes per nonempty edit/build dispatch. A batch runs joint scatter and/or exception scatter, source generation/clear, then hash insertion. |

The status fence provides the synchronous mutation/failure boundary; a failed
submitted edit invalidates the store and never replays CPU physics. No filter,
pose, manifold or joint-history table is downloaded. `ExceptionUploadBytes`
separates exception payloads from joint payloads. `FilterSubmissionCount` counts
combined batches; `FilterMS` includes preparation, growth, dispatch and status
validation, while `FilterWaitMS` measures their included fence waits. Whole-store
traffic and waits keep their existing counters. Native allocator and driver
protocol overhead are outside those payload counters.

## Verification

`ELECTRON2D_TEST_GPU_RESIDENT_EXCEPTIONS=1` runs
[GPUPhysicsExceptionStoreTests](../../tests/Electron2D.Tests/GPUPhysicsExceptionStoreTests.cs)
and the existing public CPU PhysicsCollisionExceptionTests. The full GPU runner
also includes the new resident suite. Checks cover directional/idempotent edits,
multiple joint contributions, sensor masks, self entries, incoming/outgoing
cleanup, pending delete/reuse, foreign/stale handles, wrong-thread/disposed access,
ordinary impulse response, history retirement, connected sleep/wake and both CCD
modes. Generation, lifecycle, filtering and transfer assertions are exact.

Numeric checks use a 0.01-second contact solve with zero correction speed, initial
normal velocity 10 and a fixed obstacle; stopped/unchanged velocity tolerance is
0.001 scene-unit/s. CCD uses speed 3,000, duration 0.02 seconds and a 0.2-unit wall:
an excluded obstacle permits travel 60 ± 0.002 units; a restored obstacle stops the
body before its center at x=20 with speed below 0.1. Sleep uses three overlapping
quiet circles and zero speculative margin, a 0.02-second sleep threshold and a
0.001-second wake step. The initial sleep fixture used the ordinary margin 2,
which also admitted a non-touching speculative neighbour; the fixture was
corrected to test only touching connectivity, without changing engine behavior.

The residency workload contains 8,192 bodies, 8,192 circles and 4,096 directed
exceptions in separate pairs. Removing half produces exactly 2,048 manifolds;
restoring all removes every manifold. Each timed tick then toggles one exception,
resets its body's pose/velocity and simulates one substep with four iterations.
After 128 warmup ticks, 128 measured ticks allocate zero owner-thread managed
bytes and upload exactly one 32-byte exception edit per tick. Additional unchanged
active ticks produce no exception upload or filter rebuild.

On Linux/.NET 10, Vulkan / NVIDIA GeForce RTX 3090 Ti, the focused run recorded
p50 0.6582 ms, p95 6.3225 ms and p99 6.6181 ms for those complete internal ticks;
mean total wait 2.1237 ms, combined filter batch 0.4139 ms including 0.3909 ms wait.
Average tick payload was 332 bytes uploaded, 44 downloaded and 1,914 uniform bytes.
Evidence: `/tmp/electron2d-resident-exceptions-test2.log`. This is a regression
workload on a shared desktop, with only zero or one responding pair during timing.
It is not a full public CPU/GPU comparison, network measurement, dense-contact
performance claim or window FPS result.


The final focused run after the concurrent main update also passed public CPU
exceptions and the resident GPU checks, with the same exact traffic and zero
managed allocations (`/tmp/electron2d-resident-exceptions-final-focused.log`).
Its timings overlap other regression work and are not a controlled comparison.
At this stage the 65,536-body integration check reported authored body/command
capacity 13,107,200 bytes for then-72-byte metadata and 128-byte staging. The later
[live body policy stage](gpu-resident-parameters.md) raises metadata to 88 bytes.

Source-generated shader SHA-256 values for this change:

| Resource | SHA-256 |
| --- | --- |
| PhysicsResidentJointEdits.comp.spv | `7c9aa0d6446aae236ff76de56ac877f0221abf3b21897c37363452edc269842f` |
| PhysicsResidentShapes.comp.spv | `cd455dcc7a6b615156ac239cb20d1c0f30bd1dfd0df47da9d56e3f8dea0d2ac9` |

Generated bytecode, wiki output and temporary logs remain outside the engine commit.

The final Release source-native build, focused exception runner, full GPU runner,
runtime/touched-test formatting, compiled API coverage, wiki tests/generation/check
and runtime shader delivery check passed. Final GPU evidence is in
`/tmp/electron2d-resident-exceptions-final-gpu.log`; the complete runner also verifies
compute lifetime with both renderers. Coverage on the concurrent updated main
accounts for 11,348 declarations with zero unmapped; this internal change adds no
public declarations. Other-device execution, native allocation, public independent
world selection and networking remain unverified or unfinished as listed above.
