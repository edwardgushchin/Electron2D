# DampedSpringJoint

Last updated: 2026-09-30

**Inherits:** [Joint](Joint.md), Entity, CanvasItem, Node, ElectronObject

- **Source:** [DampedSpringJoint.cs](../../src/Scene/2D/DampedSpringJoint.cs)
- **Declaration:** `public sealed class DampedSpringJoint : Joint`
- **Component:** [Physics joints](../components/physics-joints.md)

## Description

An elastic connection between two body-local anchors. At connection, the first anchor is sampled at the node's global origin and the second at its transformed local `(0, Length)` point. Subsequent body motion carries those anchors. `Stiffness` multiplies the error from relaxed separation to produce Hooke force; `Damping` reduces relative velocity along the spring axis. Forces apply at both anchors, so an off-center anchor also produces torque. Tangential motion is not directly damped and `Length` is not a maximum stretch limit.

The native filter joint owns collision filtering and body-island linkage. Before each native world interval, the engine evaluates anchor forces using current backend geometry, mass and rotational inertia, then applies equal opposite native impulses. All spring responses in that interval are checked before any are applied. Kinematic path subdivisions use their own elapsed durations, preserving one outer scene callback/event cycle. No second rigid-body/contact solver is introduced.

## Example

```csharp
var spring = new DampedSpringJoint
{
    NodeA = "../Base", NodeB = "../Bob",
    Length = 100, RestLength = 60,
    Stiffness = 20, Damping = 4
};
root.AddChild(spring);
```

This partial snippet assumes `root` has two distinct PhysicsBody children named `Base` and `Bob`, and a SceneTree advances fixed frames. Place the spring before connection to choose its anchors. Both body shapes and their resources follow the usual borrowed-resource lifetime.

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `public DampedSpringJoint()` | — | Detached spring with no connected bodies. |
| [`public float Length { get; set; }`](#length) | `50` | Signed local-Y offset used to place the second anchor. |
| [`public float RestLength { get; set; }`](#restlength) | `0` | Nonnegative relaxed separation; zero uses `abs(Length)`. |
| [`public float Stiffness { get; set; }`](#stiffness) | `20` | Nonnegative force coefficient in kg/s². |
| [`public float Damping { get; set; }`](#damping) | `1` | Nonnegative axial drag coefficient in kg/s. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Stores all four properties in PackedScene. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | — | Recreates the exact spring node role. |

## Property descriptions

<a id="length"></a>
### `Length`

Sets anchor placement in scene units. Negative values reverse the second anchor's local-Y direction. A changed value rebuilds the connection before the next nonzero fixed step and also changes the automatic relaxed length when RestLength is zero. Moving the node alone does not retune an existing connection. Finite signed values through ten million scene units in magnitude are accepted; over-range and nonfinite input rejects before mutation.

<a id="restlength"></a>
### `RestLength`

The relaxed anchor separation in scene units. Zero consistently selects the magnitude of Length, including after live edits or reentry. Explicit values are finite and nonnegative through ten million scene units. A live edit changes the next force evaluation without replacing the local anchors. Spring separation may exceed both Length and RestLength.

<a id="stiffness"></a>
### `Stiffness`

The Hooke coefficient: force in scene units is `(relaxed separation - current separation) * Stiffness`. Zero disables elastic force while preserving damping. Finite nonnegative values are accepted. Excessive derived impulse or velocity fails the interval before any spring response is applied; correct the setting and advance again. Suitable stiffness and fixed-step duration determine the explicit force-integration stability profile.

<a id="damping"></a>
### `Damping`

The axial damping coefficient, rather than a dimensionless frequency-normalized ratio. With effective axial inverse mass `K` and elapsed time `dt`, relative axial velocity after the spring impulse is multiplied by `exp(-Damping * dt * K)`. `K` includes both bodies' inverse masses and anchor lever arms weighted by inverse rotational inertia. Zero disables drag; positive values work with zero Stiffness. Finite nonnegative input is required.

## Lifecycle, errors and verification

The inherited [Joint](Joint.md) paths, collision policy, warnings and scene lifetime apply. Properties are stored by PackedScene. Attached reads and writes require the scene owner thread; writes during the solver step reject. Invalid scaled/skewed or unrepresentable anchor geometry preserves the prior native connection. Near-coincident anchors below the backend normalization epsilon have no force direction. Frozen/static endpoints remain immovable; a relaxed sleeping dynamic endpoint stays asleep, while a nonzero spring impulse wakes it. CustomIntegrator omits automatic body forces but preserves this external joint response.

[DampedSpringJointTests](../../tests/Electron2D.Tests/DampedSpringJointTests.cs) checks analytic force/mass and exponential drag, off-center torque, pair momentum and equilibrium, stretching beyond Length, rotated/reversed/coincident geometry, live changes, sleep/freeze/custom integration, collision filtering, packing, lifecycle, thread/numeric rollback, multiple-spring preflight and kinematic interval duration. Sixty-four warmed active frames allocate zero managed bytes on Linux/.NET 10. Native allocation, broad-scene performance/stability, other platforms and owner visual acceptance remain unverified. Inherited joint RID/bias and physics debug drawing retain exact [coverage gaps](../coverage/classes/DampedSpringJoint2D.md) under [ADR 0086](../decisions/physics-joints.md#adr-0086).
