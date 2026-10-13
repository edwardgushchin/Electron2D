# Separate numerical physics thread

Last updated: 2026-10-13

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
GPU numerical execution, real rendered output, native allocations, other platforms
and a measured scheduling advantage remain open. The setting stays Partial in
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
closing GPU/native or scheduling-performance gates.

## Decisions

[ADR 0054](../decisions/physics-backends.md#adr-0054) owns captured scheduling,
GPU numerical ownership and failure policy; [ADR 0103](../decisions/physics-extensions.md#adr-0103)
owns authorized provider inputs and hook lifetimes.
