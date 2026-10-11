# PhysicsBodyIntegrationConfiguration

Last updated: 2026-10-11

**Declaration:** `internal readonly record struct PhysicsBodyIntegrationConfiguration` · **Source:** [PhysicsBodyIntegrationConfiguration.cs](../../src/Servers/Physics/PhysicsBodyIntegrationConfiguration.cs) · **Component:** [Value-only integration inputs](../components/physics-backends.md#value-only-body-integration-inputs)

Captures complete authored integration policy without passing the internal body runtime or scene node into numerical methods. PhysicsBodyRuntime captures GravityScale, LinearDamp, AngularDamp, both DampMode values, effective LockRotation, OmitForces, ConstantForce/ConstantTorque and CCDMode from the current scene/raw owner. Values use existing engine units and preserve signed damping/gravity semantics. The immutable value owns no resources or binding lifetime.

CPU field preparation combines this value with Area/default fields and previous PhysicsBodyFields, validates the complete finite result before motion, then returns new PhysicsBodyFields. The CPU world binding assigns the observed values after successful return. GPU numerical policy preparation receives this same value and dirty/forced controls; its world binding owns common/scene dirty-flag retirement. The warm short circuit occurs before capturing unchanged policy, preserving demand and avoiding new transfers. Step, checkpoint preparation and explicit sleep use the same GPU binding function; forced diagnostic refresh does not retire ordinary dirty flags.

This is actual CPU/GPU integration data, not a public factory or plugin API. Existing parameter/field/sleep/CCD/checkpoint/snapshot suites establish their exercised paths. Native allocation, other platforms and complete public backend registration remain separate gates.
