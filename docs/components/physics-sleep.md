# World sleep policy

Last updated: 2026-10-21

[World policy publication recovery](physics-registered-adapter.md#failed-world-policy-publication) retains complete current profiles after partial selected errors, retries before numerical preparation and preserves skipped-interval and cleanup behavior.

## Contract and ownership

PhysicsServer exposes concrete getter/setter pairs for a world's linear quiet
threshold, angular quiet threshold and quiet duration. [PhysicsSleepSettings](../classes/PhysicsSleepSettings.md)
is the shared authored policy used by CPU PhysicsSpace and independent GPUPhysicsBodyStore.
Defaults come from three typed ProjectSettings keys when the world is created:
2 scene units/s, 0.13962634 rad/s and 0.5 s. Later project edits affect new worlds.

Both speeds must remain strictly below their thresholds for longer than the duration.
Angular qualification is independent of shape size. A zero speed threshold prevents
automatic sleep, including at rest. A zero duration permits sleep after the first
eligible positive interval. Zero-time reads/steps and inactive CPU worlds do not age
quiet time. CanSleep=false prevents automatic sleep; explicit sleep remains usable.
Current contact/joint islands must be quiet together. Each backend also keeps bodies
awake while its positional correction remains significant.

World access requires the owner thread outside solver execution. Equal writes leave
sleep and timers intact. Changed settings wake dynamics and reset timers, including
in inactive worlds; new bodies receive current values. A post-solver callback can
commit a change for the next interval even if that callback subsequently throws.
Scene SleepingStateChanged observes automatic sleep and policy wake once each;
borrowed body views and world/body RIDs keep their identity.

## Backend execution and traffic

CPU bodies retain linear thresholds in backend length units; the world supplies
independent angular and duration values. Finalization tests translation and rotation
separately. The old CPU-hosted GPU finalizer receives the same values in its existing
64-byte input and 32-byte uniform layouts.

The independent GPU sleep kernel keeps component discovery, timer reduction and
sleep/wake publication on device. Settings edits queue ordered per-body wake commands,
so an explicit sleep written afterward wins and synchronous reads see policy wakes.
Only authoring metadata is retained on the host. An unchanged policy generates no
such commands. Mass settings, shape storage and physical body poses are unaffected.

## Verification and limits

PhysicsSleepPolicyTests runs the same threshold/duration/change scenarios through
public CPU operations and the independent GPU store, plus the older GPU-stage path.
Binary-exact 0.03125 s steps and 0.125/0.25 s durations check exact boundary decisions.
One- and 100-unit-radius circles rotating at 0.125 rad/s qualify identically against
0.25 rad/s. Linear speed 2 at threshold 2 and angular speed -0.25 at threshold 0.25
stay awake. Scene callbacks exercise solver guards and one-time sleep/wake events.
The existing connected sleep and collider suites cover contacts, joints and lifecycle.

Linux/.NET 10 Release, 64 isolated active circles moving at 4 units/s; alternate the
linear threshold between 1 and 2 before every full 1/60 s step. 256 warmup, 128 samples:

| Path | p50 / p95 / p99, ms | All-thread managed bytes over 128 steps |
| --- | --- | --- |
| Public CPU, dummy video | 0.0127 / 0.0148 / 0.0184 | 0 |
| CPU host/GPU stages | 0.1094 / 0.2188 / 0.3443 | 0 |
| Independent resident GPU | 1.0525 / 1.4255 / 2.6183 | 0 |

The same population at rest, with zero quiet duration, wakes on every policy edit
and sleeps again during the following step. After identical warmup/sampling:

| Path | Wake/sleep p50 / p95 / p99, ms | Managed bytes |
| --- | --- | --- |
| Public CPU | 0.0175 / 0.0177 / 0.0207 | 0 |
| CPU host/GPU stages | 0.1097 / 0.1352 / 0.1472 | 0 |
| Independent resident GPU | 1.0403 / 1.3521 / 2.6030 | 0 |

Resident wake/sleep traffic equals the active case; included waits average 0.6613 ms.

Vulkan/NVIDIA GeForce RTX 3090 Ti: independent GPU uploads 11,392 B of buffers and
4,116 B of uniforms, reads 128 B of status per edited frame; included waits average
0.6772 ms. No pose readback occurs in the measured loop. The deliberately changing
policy measures its O(body count) ordered wake journal, not steady unchanged frames.
Logs: `/tmp/electron2d-sleep-cpu.log`, `/tmp/electron2d-sleep-stages.log`,
`/tmp/electron2d-sleep-resident.log`.

These small workloads establish policy behavior and managed allocation, not GPU
speedup on large worlds. Native allocation, other devices/platforms, real-window FPS,
networking and public independent-GPU binding remain outside this acceptance.


[Independent automatic sleep](physics-registered-adapter.md#independent-automatic-sleep)
now consumes actual world thresholds and duration in the public-only numerical
fixture. Dynamic contact/joint groups sleep together; static supports do not
merge groups. Shared registered/CPU/GPU --sleep checks cover strict boundaries,
zero thresholds/duration, CanSleep, policy/impulse wake, inactive intervals and
quiet-clock rollback. Each path measures 1920 replay frames and 896 actual group
sleep/wake frames at strict 0/0 owner/all-thread managed bytes. General geometry,
CCD, portable networking, native totals and final performance remain open.


## Shared public sleep policy, 2026-10-14

`ELECTRON2D_TEST_PUBLIC_SLEEP_POLICY=cpu|gpu` runs the complete threshold/lifetime/
scene suite through explicitly selected PhysicsServer spaces and scene Worlds.
The early entry captures `ELECTRON2D_TEST_THREADED_COLLIDER=1` before world creation;
the ordinary full runner includes both public backends. Resident and CPU-hosted
stage controls remain separate. All six profiles pass on software Vulkan.

Binary-exact duration and strict speed-boundary cases remain unchanged, including
radius-independent angular qualification, zero thresholds/duration, CanSleep,
invalid/owner/phase access, equal/change ordering and inactive intervals. The public
GPU path also checks the post-solver policy edit followed by a throwing user
callback: the live view remains usable, and the next step uses the committed policy.
Scene automatic sleep and policy wake each publish their promised single transition.

The unchanged 64-body workload runs both moving and wake/sleep-cycling populations,
256 warmup/128 measured edited whole frames each. Every profile/population now
requires 0/0 owner/all-thread managed bytes. Public GPU traffic is recorded from its
actual selected store: 11,400 B upload, 4,372 B uniforms and 152 B readback per
changing-policy frame in this fixture. No new host state mirror is introduced.

The existing public-only PhysicsBackendExtension.Consumer `--sleep --cpu`,
`--sleep --gpu` and `--sleep` registered profiles also pass from the same frozen
runtime. These serial consumer runs check pin/contact group sleep, impulse waking,
one CanSleep veto, separated static supports and quiet-clock checkpoint restoration.
Each retains 128 measured replay cycles (1,920 frames) and group sleep/wake cycles
(896 frames) with 0/0 managed bytes. They are distinct from the primary separate
numerical-thread profiles, not a new threaded-group acceptance claim.

CPU/registered processes load no graphics driver; GPU/stage/resident processes
load CPU-only llvmpipe without NVIDIA. The retained numerical thread is observed
only in separate public profiles. Evidence is retained under
`bin/physics-public-sleep/2026-10-14/`. Software timing is diagnostic; arbitrary
registered geometry, native allocation, foreign platforms, networking, massive
hardware advantage and real-window FPS remain separate acceptance gates.


## Pooled dormant island storage

CPU natural sleep previously reserved only 16 body, 32 contact and 4 joint records
for each dormant set. A 16-body/15-pin chain warmed for 128 active frames then
allocated 3624/3624 owner/all-thread managed bytes during its first natural sleep.
The original failing process and source/PDB proof are retained. Increasing every
sleeping set to whole-world capacity would multiply record storage by the number
of possible islands.

[CPUPhysicsSleepStorage](../classes/CPUPhysicsSleepStorage.md) instead prepares
rounded array buckets and a separate reusable record supply for bodies, contacts,
joints and islands. It uses the current sleep-capable body capacity, existing
contact graph budget and joint identity budget. Natural sleep and sleeping-set
merge acquire capacity; wake returns cleared dormant storage. Growing a live
array moves its existing references and returns only spare objects, preserving
physical records. Array reference storage grows with the budget times its
logarithm, while prepared record objects grow linearly. Worlds that never enable
dynamic sleep do not create this pool. Disposal clears both retained storage and
native callback references.

Checkpoint restore prepares the saved live counts before copying numerical
state and releases inactive destination arrays afterwards. The current and saved
partitions can coexist during preparation; pool bookkeeping is not snapshot or
wire state. Existing body/joint identity and sleep equations are unchanged.

An additional 6408-byte partition failure was traced to two 1176-byte island-split
scratch arrays and a 4056-byte arena growth, independently of the new pool.
Sleep enabling now also prepares the existing integer split-arena budget for
small worlds; it had previously been prepared only at 256 body-capacity slots.
The allocation-profiler build and temporary pool-miss assertions are diagnostic
artifacts and are absent from the production candidate.

`PhysicsDormantCapacityTests`, also linked into the public-only consumer as
`--dormant-capacity`, verifies first sleep for 16/17/33/65 bodies, real impulse wake
and linear momentum, eleven successive fresh partitions of the same 65 bodies,
a touching contact/pin chain and 128 measured complete wake/checkpoint-restore
cycles after 128 warm cycles. New partitions remove old joints before resetting
poses and create the desired constraints at rest. All eight CPU/GPU JIT/strict NativeAOT
owner/separate profiles pass with strict 0/0 managed bytes. CPU processes disable
GPU discovery; NVIDIA mappings exclude a software driver in the GPU profiles. Cold capacity preparation is measured outside
the warmed step interval; it remains actual construction/configuration work.
With the identical final public consumer DLL/PDB, the old runtime fails the
16-body case at 3624/3624 warmed bytes and the new runtime passes at 0/0. Cold
preparation allocates 210968/210968 versus 134696/134696 bytes in that pair.
These are allocation counters for preparation, not retained heap/native size.
PDB proofs match 2436 runtime, 928 root-test and 263 consumer authored sources,
with a separately matched AOT input cohort, including the concurrent PCRE2 main
integration. Adjacent CPU kernel checkpoint and
public sleep/compound joint-contact owner/separate regressions also pass.

A further public 128-body case changes contact groups while preserving all body
and shape identities, then restores the saved sleeping partition. Each natural
sleep interval allows 256 steps for at most 128 pending splits and the quiet
timer; the original 16-step cases retain their bound. A one-partition array
reserve allocated 376 bytes on this first different restore. Preparing arrays
for both current and saved partitions removed 352 bytes. Exact allocation stacks
attributed the remaining 24 bytes to an empty `Array.Resize` in checkpoint copy;
zero-size copies now retain nullable storage and valid empty bitset capacity.
The resulting CPU owner/separate case restores all 128 poses and sleep flags at
strict 0/0 bytes. Record object supply remains four per budget unit; no numerical
iterations or tolerances change.

An earlier fixture retained warm-started joints while teleporting all bodies to
reset a preceding impulse. At that dormant-storage checkpoint, both runtimes produced the same
failed group-65 observation: body 0 position .04293163, velocity -3.0948648 and no
sleep after the tested window. That convergence scenario was not accepted as a capacity regression or
a convergence proof at that checkpoint; its failed baseline behavioral control is
preserved. The control disables allocation assertions only in an ignored probe
copy, and is not zero-allocation or functional conformance acceptance.
[The hard-row feedback follow-up](physics-joint-policies.md#retained-hard-row-guesses)
now restores the retained-joint partition scenario in the final fixture with its
original step count, pose/momentum bounds and zero-allocation assertions. The
historical failed control remains retained.

Receipts, source proofs, exact allocation stacks and failed controls are under
`bin/physics-dormant/2026-10-21/`. [Observed dense graph preparation](#observed-dense-contact-capacity)
now grows beyond the former four-contacts-per-prepared-body budget.
Full numerical, allocation/native/platform/network and hardware performance
acceptance remain open, including the separate 64-byte full-suite failure and
the 65,536-body GPU device-loss/Xid gate.


## Observed dense contact capacity

The initial CPU reservation remains four contacts per rounded body-capacity slot.
Before each numerical interval, the owner compares the native contact identity
high-water mark with the prepared contact capacity. A new peak grows that capacity
to the next power of two, including the pair hash table, live contact records,
solver-set arrays, task bitsets, overflow graph, solver scratch and dormant record
pool. Joint preparation retains the same contact budget; full-interval impulse
buffers allow two manifold points per prepared contact. This is capacity state,
so checkpoint restore does not rewind the observed peak. Existing contact keys,
shape slots, sleep clocks, numerical equations and report caps are unchanged.

`PhysicsDenseContactCapacityTests` uses only public API and is linked into the
external consumer as `--dense-capacity`. Two compound bodies with 16, 17 or 32
coincident circle/floor slots produce exactly 256, 289 or 1024 real contact pairs.
Every wake checks unique local/collider slots, floor RID, unit support normals,
the full gravity-balancing impulse sum, and direct-view identity. Natural sleep
checks resting pose and velocity. Each topology measures 128 complete natural
sleep/wake cycles and 128 wake/private-checkpoint-restore cycles after 128 warm
cycles for each operation, with strict zero owner/all-thread managed bytes.

On the original runtime, the identical final consumer fails its first 256-contact
natural-sleep case at 4,984,032/4,984,032 bytes on the owner lane and
0/4,984,032 bytes with a separate numerical lane. The corrected CPU owner/separate
profiles pass every topology and both operations at 0/0 bytes. All eight public
CPU/GPU owner/separate JIT/strict NativeAOT profiles pass. CPU discovery is disabled;
GPU process mappings confirm NVIDIA and exclude a software driver. The root CPU
entry and four adjacent kernel-checkpoint, dormant owner/separate and dense
portable-contact profiles also pass (13 successful profiles plus two expected
baseline failures). PDB proofs match all 2436 runtime, 929 root-test and 264
consumer authored sources, with a separately matched AOT input cohort. Frozen
payloads, source proofs and process receipts are under
`bin/physics-dense-capacity/2026-10-21/`.

A newly observed topology may allocate while reaching its first peak. These
bounded workloads do not certify arbitrary graph mutations, native allocation,
retained-impulse convergence, complete numerical/network/platform acceptance or
hardware performance. The separate full-suite 64-byte allocation and massive GPU
Xid/device-loss gates remain open. Declaration coverage is unchanged.


## Kinematic dormant capacity

CPU dormant capacity includes every sleep-enabled non-static body. Kinematic
islands also enter the native dormant sets; counting only dynamic bodies left
mixed scene roles with too few pooled arrays. Repeated filter-wake/sleep cycles
then allocated 256 owner/all-thread bytes in PhysicsFilterTests. The calibrated
object trace places 32-byte array creation in CPUPhysicsSleepStorage.Buffers.Ensure
under the actual sleep path. Attachment, motion-mode changes and enabling sleep
now prepare the complete eligible population; static and sleep-disabled bodies
still do not consume that population budget.

A separate first-wake trace identifies the 48-byte shared native body state in its
lazy type initializer. CPU-world creation now completes that initializer before
simulation. Numerical sleep equations, body roles and ownership remain unchanged.

PhysicsDormantCapacityTests adds 17 kinematic bodies through direct attachment,
Static-to-Kinematic mode changes and CanSleep enabling after active preparation.
A long quiet-time policy prevents hidden sleep during 128 authored-motion/query
warm frames. The first short-policy sleep and 64 actual filter-wake/sleep/query
cycles each require strict zero owner/all-thread managed bytes and preserve body
identities and query geometry. The final fixture fails at 9160/9160 bytes on the
previous runtime; the intermediate candidate exposes the separate 48-byte wake.
The corrected CPU owner/separate profiles pass through the public-only consumer
in CLR and strict NativeAOT with the same assertions and unavailable GPU drivers.
These CPU storage checks do not impose native solver-set representation on GPU.
The existing shared PhysicsFilterTests passes on CPU and independent GPU.

Local receipts, calibrated traces and frozen payloads are under
`bin/physics-full-integration/2026-10-21/`. The historical off-owner 64-byte
script-factory failure retains its separate unproven cause. Full-goal, wider
numerical/network/native/platform and performance acceptance remain open.


The uninstrumented full root suite on the corrected frozen source cohort passes
in 611.154 seconds, including the CPU mixed-role filter interval and all six
CPU/GPU/registered owner/separate script-factory intervals at 0/0 bytes. A separate
final filter selector also exercises both CPU and GPU. The 900-tick owner/separate
network checks each run an unavailable-GPU CPU authority, an independent GPU client
and a late unavailable-GPU CPU client. All three processes converge to 30 actors;
late clients enter at ticks 342/341. Positive warmed forward/replay samples retain
zero owner/all-thread managed bytes. These are functional regressions on this
cohort, not a causal resolution of the historical 64-byte interval or a renewed
mass-performance/FPS measurement. Final integrity checks use
`bin/physics-full-integration/2026-10-21/validate.py` and `validation.json`.
