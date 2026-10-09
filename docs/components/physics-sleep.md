# World sleep policy

Last updated: 2026-10-09

## Contract and ownership

PhysicsServer exposes concrete getter/setter pairs for a world's linear quiet
threshold, angular quiet threshold and quiet duration. [PhysicsSleepSettings](../classes/PhysicsSleepSettings.md)
is the shared internal policy used by CPU PhysicsSpace and independent GPUPhysicsBodyStore.
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
