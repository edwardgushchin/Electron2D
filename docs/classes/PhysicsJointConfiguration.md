# PhysicsJointConfiguration

Last updated: 2026-10-11

**Declaration:** `internal readonly record struct PhysicsJointConfiguration` · **Source:** [PhysicsJointConfiguration.cs](../../src/Servers/Physics/PhysicsJointConfiguration.cs) · **Component:** [Physics backends](../components/physics-backends.md#shared-joint-inputs-and-native-ownership)

## Description

One complete immutable value sampled from PhysicsJointRuntime for selected joint attachment, solver-policy updates and CPU spring preparation. It owns no resource, endpoint, RID, mutable runtime or native handle. Frames, translations, rest length, bias and force settings keep their engine units until the implementation compiles them. The runtime resolves scene zero-rest fallback before creating this value; server rest remains literal.

## Internal values

| Values | Meaning |
| --- | --- |
| `JointType Type`, `Transform FrameA`, `Transform FrameB` | Concrete role and preserved engine-local frames; a one-body pin's second frame is world-relative. |
| `bool DisableCollision`, `float LowerTranslation`, `float UpperTranslation` | Connected collision policy and groove bounds in scene units. |
| `float Bias`, `float MaxBias`, `float MaxForce` | Authored correction policy; zero bias inherits the live world default in the concrete implementation. |
| `float PinSoftness`, `bool PinLimitEnabled`, `float PinLimitLower`, `float PinLimitUpper` | Positional compliance and angular limits in radians. |
| `bool PinMotorEnabled`, `float PinMotorVelocity`, `float PinMotorMaxTorque` | Motor activation, angular speed and torque cap. |
| `float EffectiveSpringRestLength`, `float SpringStiffness`, `float SpringDamping` | Resolved rest length and the current elastic/axial-drag coefficients. |

Internal flow: `joint.Configuration` is passed by readonly reference to the selected `Attach` or `ApplySolverPolicy`; CPU spring preparation samples the same property each interval. This is an implementation input, not a public authoring type or registration API.

## Ownership and verification

The runtime validates authored setters and retains membership/lifetime. CPU/GPU implementations compile values independently; CPU spring scheduling resolves the actual native attachment from its world owner. [Selected-view tests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.SelectedViews.cs) wrap real pin/groove/spring implementations, check refreshed bias/force values and execute physical response. Shared joint conformance retains numerical, lifecycle and allocation boundaries. Public extension registration and the complete operation bridge remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).
