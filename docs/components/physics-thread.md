# Separate numerical physics thread

Last updated: 2026-10-21

## Contract and runtime flow

ProjectSettings.Physics2DRunOnSeparateThread projects physics/2d/run_on_separate_thread,
default false. Each WorldRuntime captures the active feature override before lazy
physics creation; aliases retain it. Raw SpaceCreate captures it at creation.
Later edits affect new worlds only. Browser hosts reject true before runtime/canvas
or space registration because this profile has no separate-thread capability.

PhysicsSpace prepares bodies, fields, joints and queries on its original owner.
On the first positive active interval it starts [PhysicsSolverThread](../classes/PhysicsSolverThread.md).
Only the complete selected numerical Solve moves to that retained thread. The owner
waits for completion before synchronization, scene poses, contact/Area observations,
debug publication and gameplay callbacks. These synchronous library barriers retain
ordinary explicit Step/query workflows and prevent observing an in-flight world.
Inactive and zero-duration intervals create no numerical work or thread.

The worker may borrow GPUPhysicsBodyStore privately while the owner is waiting.
Cold numerical pipelines now prepare on the original owner before that borrow;
the retained per-store handles are reused on the worker. Feature edits prepare
newly needed kernels before dispatch, and cold preparation errors remain before
numerical entry. See [ownership and retirement evidence](physics-retirement-memory.md#owner-pipeline-preparation).
GPU commands are acquired, recorded and submitted on that same worker; device
creation/retirement, public reads and authoring keep the original owner. No CPU
numerical replacement, full GPU mirror or new device fence is introduced.
Registered Solve and its immediate failure read use the same numerical lane.
Prepared mass/geometry/exclusion inputs remain readable there; writes, live public
queries, recursive steps and scene/lifetime mutation remain forbidden. All other
provider hooks and game callbacks keep the original owner.

## Failure and lifetime

Worker errors are rethrown on the owner and numerical borrowing always ends.
Started GPU/provider failures retain their terminal no-replay policy. A CPU failure
after entering native solving also becomes terminal: retained jobs drain, solver
locks/arenas/context are released, and further queries or steps reject. Validation
before native solving retains its existing correction/retry semantics. Disposal
joins the numerical thread before retiring solver attachments, even after failure.

## Verification and remaining gates

PhysicsSeparateThreadTests verifies genuine CPU numerical work off-owner, completed
owner callbacks, pre-start capture/aliases, serial later spaces, 128 warmup/128
measured full frames at 0/0 managed bytes, raw-space feature capture and worker retirement. It injects a CPU
numerical failure and requires exactly one attempt, no successful tick/replay and
clean worker retirement. The same threaded profile executes the complete shared
CCD, joint policy, boundary, body, force, contact and shape-event suites with their
existing numerical and strict allocation bounds.

The separate public-only consumer uses its independent circle/annulus solver. It
checks factory/synchronization/retirement on owner, numerical/status work off-owner,
prepared input access, mutation/query/lifetime rejection, real body travel and
128 warmed/128 measured complete frames at 0/0 managed bytes. A failed numerical
hook is terminal and cleanup retires both worker and provider.

The complete collider suite passes with ELECTRON2D_TEST_COLLIDER_BACKEND=1 and
ELECTRON2D_TEST_THREADED_COLLIDER=1 in separate processes. It preserves the existing
body/Area, geometry, query, material, motion, sleep, joint and physical-bone bounds.
The physical-bone child selects its fresh-scene entry before the suite entry and
retains threaded policy; inherited suite selection cannot recursively rerun the suite.

Run ELECTRON2D_TEST_PHYSICS_THREAD=1 against the Release test executable, or run
PhysicsBackendExtension.Consumer with --separate-thread. The checked profiles deny
graphics through dummy video, invalid GPU driver, missing Vulkan driver files and
absent display/session paths. Evidence is retained under bin/physics-thread/2026-10-13.
GPU CCD, boundary and directed-ray numerical execution now passes on software
Vulkan in both policies; physical GPU execution, real rendered output, native
allocations, other platforms and a measured scheduling advantage remain open. The setting stays Partial in
coverage until the GPU/registered/numerical/native boundaries are fully verified;
CPU workers alone are not its acceptance proof.

[The network example](physics-network-example.md#cpu-only-and-separate-numerical-profiles)
now runs the complete impaired three-process CPU scenario in serial/separate modes.
The instrumented host verifies actual numerical thread identity while authority
correction/replay, topology, sleep and exact-once confirmed effects execute. Every
process rejects GPU startup first; positive warm prediction/replay samples retain
0/0 managed owner/all-thread bytes after 128 completed steps in each process.
Late admission after tick 300 is explicitly exercised with a longer run rather
than labelling its initial frames warm. This extends CPU integration evidence without
closing hardware GPU/native or scheduling-performance gates.

## Decisions

[ADR 0054](../decisions/physics-backends.md#adr-0054) owns captured scheduling,
GPU numerical ownership and failure policy; [ADR 0103](../decisions/physics-extensions.md#adr-0103)
owns authorized provider inputs and hook lifetimes.

The continuous-contract entry now accepts separate numerical execution without
being redirected to the general CPU suite. Both CPU/GPU profiles pass the same
CCD, infinite-boundary and directed-ray cases on software Vulkan, with explicit
0/0 owner/all-thread warm allocation gates. See [current software conformance](cpu-continuous-collision.md#software-vulkan-continuous-conformance-2026-10-14).
Hardware performance, native allocation and foreign-platform acceptance remain open.

[Shared public contact correction](physics-contact-policy.md#shared-public-correction-2026-10-14)
now runs the complete shape/world bias, slack, resource/lifetime and scene sleep/event
suite on explicitly selected CPU/GPU worlds in serial/separate numerical modes.
Seven forms include both bias-one endpoints; 128 measured edited full steps require
0/0 owner/all-thread managed bytes. Software Vulkan execution and historical direct
resident/stage controls remain distinct from hardware/performance acceptance.

[Shared public priority verification](physics-contact-policy.md#shared-public-priority-2026-10-14)
now exercises weighted recovery, scene/server/CharacterBody motion, packing and
lifetime through explicit CPU/GPU worlds in both numerical-thread modes. Changing
priority preserves sleep and fixture identity over 128 measured full frames at
0/0 owner/all-thread managed bytes; query brackets retain the same zero budget.
Software Vulkan execution does not establish hardware or broader acceptance.

[Shared public contact history](physics-contact-policy.md#shared-public-history-2026-10-14)
now checks seven geometries, live reuse limits, actual support/impulses and scene
sleep/event ordering through explicit CPU/GPU worlds in both numerical-thread modes.
All six public/resident/stage profiles retain 128 edited full-step samples at
0/0 owner/all-thread managed bytes. Software Vulkan evidence leaves hardware,
registered geometry and broader network/platform/performance acceptance open.

[Shared public iteration verification](physics-contact-policy.md#shared-public-iterations-2026-10-14)
now exercises captured/live counts, contact/pin convergence, momentum, fixed-time
integration and sleep/phase guards on explicit CPU/GPU worlds in both numerical
thread modes. All six profiles preserve 256 active bodies and 1/16/32-sweep full
step brackets with 0/0 owner/all-thread managed bytes. Hardware and broader
registered/network/platform/performance acceptance remain open.

[Shared public sleep verification](physics-sleep.md#shared-public-sleep-policy-2026-10-14)
now checks the complete strict threshold/duration, callback/lifetime and scene event
suite on explicit CPU/GPU worlds in both numerical-thread modes. All six profiles
retain 64 active or wake/sleep-cycling bodies at 0/0 warm owner/all-thread bytes.
The public-only CPU/GPU/registered consumer additionally verifies connected groups
and quiet-clock restoration. Hardware and broader acceptance remain open.

[Repeated world-retirement memory](physics-retirement-memory.md) now shows
substantial free glibc arena/RSS growth on the separate-lane software GPU despite
joined threads and collected selected owners. Default native retention remains
open; a diagnostic one-arena process control is not a production fix.

[Script-factory allocation diagnostics](gpu-resident-reports.md#script-factory-allocation-investigation)
retain the full-suite GPU/separate failure at 0 owner / 64 all-thread bytes.
Calibrated per-object recording, three full diagnostic suites and the prepared
worker sequence do not reproduce it. Successful frozen-checkpoint retries do not
attribute or fix the failed interval; complete-suite allocation acceptance stays
open and the strict zero-byte assertions remain unchanged.
