# PhysicsBodyForceConfiguration

Last updated: 2026-10-11

**Declaration:** `internal readonly record struct PhysicsBodyForceConfiguration` · **Source:** [PhysicsBodyForceConfiguration.cs](../../src/Servers/Physics/PhysicsBodyForceConfiguration.cs) · **Component:** [Value-only integration inputs](../components/physics-backends.md#value-only-body-integration-inputs)

A complete temporary command value for selected default-force preparation. It contains no runtime, scene object, native handle or mutable owner reference. PhysicsBodyRuntime samples it after role/activity guards. The selected collider processes it; the engine clears pending force/torque after successful return. A hook rejected before applying its input leaves those pending values available without committing a tick.

| Field | Meaning |
| --- | --- |
| PendingForce, PendingTorque | One outer-step force and torque in engine units. |
| ConstantForce, ConstantTorque | Authored persistent values retained independently of whether this role currently applies them. |
| GravityScale | Authored unitless gravity multiplier. |
| OmitForces | Disable ordinary force/gravity integration. |
| RotationLocked | Current attached numerical rotation lock; pending torque is ignored when locked. |
| ApplyPersistentForces | Common eligibility for scene freeze/static/activity policy; CPU respects it before constants. GPU keeps persistent application in resident kernels. |
| RefreshGravityScale | Raw/non-rigid CPU preparation refreshes native gravity policy; scene rigid setters retain their existing timing. |

[Force-input checks](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.ForceInputs.cs) inspect exact sampled scene/raw values, change real transient response through selected hooks, check once-only consumption/persistence, omission, frozen and stable-sleep eligibility, pre-solve rejection/recovery, rotation lock and reattachment. Numeric force results use .001 scene-units/s or radians/s slack for four float integration intervals at 1/60 s; identity/flags/queue/ticks remain exact. CPU/GPU run 64 warmup/64 measured full force frames with zero owner/all-thread managed bytes. This internal input is not public backend registration or a third-party algorithm.
