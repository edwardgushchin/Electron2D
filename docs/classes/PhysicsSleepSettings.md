# PhysicsSleepSettings

Last updated: 2026-10-09

**Declaration:** `internal readonly record struct PhysicsSleepSettings(float LinearThreshold, float AngularThreshold, float TimeToSleep)`

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

PhysicsSleepPolicyTests verifies both consumers; public GPU world selection remains
open. See [ADR 0089](../decisions/physics-activity.md#adr-0089).
