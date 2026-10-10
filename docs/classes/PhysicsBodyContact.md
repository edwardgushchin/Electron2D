# PhysicsBodyContact

Last updated: 2026-10-10

**Declaration:** `public readonly struct PhysicsBodyContact` · **Source:** [PhysicsBodyContact.cs](../../src/Servers/Physics/PhysicsBodyContact.cs) · **Component:** [Body-state extensions](../components/physics-body-extensions.md)

## Description and example

Immutable complete contact values captured for a body-state implementation. Construction requires two distinct live body RIDs and logical shape slots in one attached world, samples the collider object association weakly and qualifies the observed body's attachment generation. It owns no world, shape or object. Values/IDs/indices remain sampled after later edits or collider release; weak object access expires on disposal/collection. The default value is invalid as extension contact output.

The [complete consumer capture](../../tests/PhysicsBodyExtension.Consumer/ControlledState.cs) constructs this value from a finished public body view:

```csharp
var contact = new PhysicsBodyContact(body, state.GetContactLocalShape(0),
    state.GetContactCollider(0), state.GetContactColliderShape(0),
    state.GetContactLocalPosition(0), state.GetContactColliderPosition(0),
    state.GetContactLocalNormal(0), state.GetContactLocalVelocityAtPosition(0),
    state.GetContactColliderVelocityAtPosition(0), state.GetContactImpulse(0));
```

This snippet requires a current observed body and at least one solved contact. Caller implementations must supply actual geometry/full-step impulse/selection semantics, not invent a collision.

## API summary

| Declaration | Contract |
| --- | --- |
| `public PhysicsBodyContact(RID body, int localShape, RID collider, int colliderShape, Vector2 localPosition, Vector2 colliderPosition, Vector2 normal, Vector2 localVelocity, Vector2 colliderVelocity, Vector2 impulse)` | Validate/sample current body/slot identity and finite contact values. |
| `public RID BodyRID { get; }` | Observed physical body. |
| `public int LocalShape { get; }` | Observed body's sampled logical slot. |
| `public RID ColliderRID { get; }` | Other body's sampled physical identity. |
| `public int ColliderShape { get; }` | Other body's sampled logical slot. |
| `public ulong ColliderID { get; }` | Sampled object association ID, zero when unassigned. |
| `public ElectronObject? ColliderObject { get; }` | Sampled weak association target, null after expiry. |
| `public Vector2 LocalPosition { get; }` | Observed body's global contact point. |
| `public Vector2 ColliderPosition { get; }` | Other body's global contact point. |
| `public Vector2 Normal { get; }` | Global normal pointing away from the collider. |
| `public Vector2 LocalVelocity { get; }` | Observed body's global contact-point velocity. |
| `public Vector2 ColliderVelocity { get; }` | Other body's global contact-point velocity. |
| `public Vector2 Impulse { get; }` | Full completed-step impulse applied to the observed body. |

## Constructor and member descriptions

BodyRID/ColliderRID are engine identities, never solver handles or network identities. LocalShape/ColliderShape describe the logical slots at capture and may become stale indices after structural reindexing; they are not owner-group IDs. ColliderID and ColliderObject share the sampled weak association, which is not replaced by a later object rebind.

LocalPosition and ColliderPosition use global scene coordinates. Normal, LocalVelocity, ColliderVelocity and Impulse use global axes. Local identifies the observed body; it does not name a coordinate frame. Positions use scene units, velocities use scene units per second, and impulse uses scene units times kilograms per second, summed across the complete step.

All vectors must be finite. Invalid/non-body/wrong-world/detached IDs, invalid slots, off-owner/solver/failed-world access and disposed geometry reject construction before a value is returned. Identity sampling uses the common server result guard. An extension validates that BodyRID and the privately sampled generation still match its current attachment; the collider may have expired after capture.

## Verification and limits

The [separate consumer](../../tests/PhysicsBodyExtension.Consumer/Program.cs) checks real scene contact projection, all sampled values, invalid/default/wrong-world data, stale observed generation, collider release and weak target expiry on CPU/GPU/no-device CPU profiles. Warmed capture/projection through retained provider state allocates no managed bytes; user code and structural work may allocate. This result provides no backend registration or new wire representation. See [ADR 0103](../decisions/physics-extensions.md#adr-0103).
