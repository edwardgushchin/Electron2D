# PhysicsSleepSettings

Last updated: 2026-10-13

**Declaration:** `public readonly record struct PhysicsSleepSettings(float LinearThreshold, float AngularThreshold, float TimeToSleep)`

**Source:** [PhysicsSleepSettings.cs](../../src/Servers/Physics/PhysicsSleepSettings.cs)
**Component:** [World sleep policy](../components/physics-sleep.md)

Shared authored world policy in scene units/s, radians/s and seconds. FromProject
samples current typed project values with feature overrides. Validate rejects
negative/nonfinite values and positive linear thresholds that underflow the CPU
unit conversion. Values are not poses, body state or a GPU mirror.

PhysicsSpace publishes the settings to existing/new CPU bodies and native world
angular/time fields. GPUPhysicsBodyStore retains the same record and uploads it
through existing sleep uniforms. Changed policy wakes bodies through each backend's
ordinary ordered authoring path. Equal/invalid writes preserve the old state.

PhysicsSleepPolicyTests verifies both built-in consumers through public world selection. See [ADR 0089](../decisions/physics-activity.md#adr-0089).

This immutable value is now public and crosses the [registered implementation boundary](../components/physics-registered-adapter.md); complete sampled fields retain the contracts above. No backend storage or native handles are exposed.


The independent public-only numerical fixture also consumes all three fields.
Shared --sleep checks verify actual strict sleep boundaries, policy and impulse
wake, dynamic groups, static-support separation, inactive intervals and quiet-clock
rollback on registered/CPU/GPU worlds. Two 128-cycle full-frame brackets measure
0/0 owner/all-thread managed bytes. This bounded fixture does not establish
complete geometry, CCD, network or native/performance acceptance.

[Shared public sleep verification](../components/physics-sleep.md#shared-public-sleep-policy-2026-10-14)
now checks the complete strict threshold/duration, callback/lifetime and scene event
suite on explicit CPU/GPU worlds in both numerical-thread modes. All six profiles
retain 64 active or wake/sleep-cycling bodies at 0/0 warm owner/all-thread bytes.
The public-only CPU/GPU/registered consumer additionally verifies connected groups
and quiet-clock restoration. Hardware and broader acceptance remain open.
