# World sleep policy

Last updated: 2026-10-18

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
