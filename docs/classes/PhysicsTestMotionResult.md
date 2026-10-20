# PhysicsTestMotionResult

[Motion source selection and contact velocity](../components/physics-queries.md#motion-source-selection-and-contact-velocity) verifies the winning logical source, recovery-only selection and rotated mass-center velocity through scene/raw callers.

Last updated: 2026-10-20

[Retained slot history](../components/physics-snapshots.md#capture-after-topology-edits)
keeps sampled public values while rejecting portable capture of retired/reindexed
slots before replacing packet bytes. In-slot replacement and disabled history stay
valid; portable apply remaps private slot lifetimes to destination authoring.

**Inherits:** ElectronObject · **Source:** [PhysicsTestMotion.cs](../../src/Servers/Physics/PhysicsTestMotion.cs) · **Component:** [Physics server and direct queries](../components/physics-queries.md)

## Description

Caller-owned output updated by [PhysicsServer.BodyTestMotion](PhysicsServer.md). The result copies collider identity, shape-owner indices, contact geometry, fractions and travel. A completed no-hit test clears the preceding collider and records full requested travel. The object does not own the bodies or their fixtures.

## Example

Partial snippet with a registered `bodyRID` and live `parameters`:

```csharp
using var result = new PhysicsTestMotionResult();
if (PhysicsServer.BodyTestMotion(bodyRID, parameters, result))
    Console.WriteLine(result.GetCollisionNormal());
```

## API summary

| Member | Contract |
| --- | --- |
| `public PhysicsTestMotionResult()` | Empty caller-owned output. |
| `public void SetMotion(Vector2 travel)` | Replaces output with completed unobstructed travel. |
| `public void SetCollision(RID body, int localShape, RID collider, int colliderShape, Vector2 point, Vector2 normal, float depth, Vector2 colliderVelocity, Vector2 travel, Vector2 remainder, float safeFraction, float unsafeFraction)` | Validates and replaces a supplied motion collision. |
| `public ElectronObject? GetCollider()` | Live sampled object association, or null when unassigned/disposed/collected. |
| `public ulong GetColliderID()` / `public RID GetColliderRID()` | Sampled collider identities. |
| `public int GetColliderShape()` / `GetCollisionLocalShape()` | Direct collider/moving shape-owner indices. |
| `public Vector2 GetColliderVelocity()` | Collider point velocity, scene units per second. |
| `public float GetCollisionDepth()` | Contact penetration depth in scene units. |
| `public Vector2 GetCollisionNormal()` / `GetCollisionPoint()` | Global outward normal and point on collider. |
| `public float GetCollisionSafeFraction()` / `GetCollisionUnsafeFraction()` | First impact bracket from zero through one, with [adaptive endpoint refinement](../components/physics-queries.md#adaptive-sweep-refinement) and an accurate geometric unsafe sample where available. |
| `public Vector2 GetTravel()` / `GetRemainder()` | Travel including recovery and untraveled requested motion. |

## Method descriptions

<a id="setmotion"></a>
### SetMotion

Sets finite global `travel` in scene units, clears collision identity, indices, point, normal, depth and collider velocity, zeroes remainder and sets both fractions to one. Nonfinite travel rejects with ArgumentOutOfRangeException; disposed results reject with ObjectDisposedException. This fills existing storage and performs no motion test.

<a id="setcollision"></a>
### SetCollision

`body` and `collider` must identify different live bodies attached to the same usable physics space. Areas, wrong/stale RIDs and self collisions reject with ArgumentException; detached or different-space bodies and wrong-thread/solver/failed-world access reject with InvalidOperationException. `localShape` and `colliderShape` select logical shape-owner indices; indices, nonfinite vectors, negative/nonfinite depth and fractions outside `0 <= safeFraction <= unsafeFraction <= 1` reject with ArgumentOutOfRangeException. Disposed shapes or this result reject with ObjectDisposedException. All validation occurs before replacing the previous result.

`point` and `normal` are global; depth, travel and remainder use scene units; `colliderVelocity` is the global contact-point velocity in scene units per second. Supplied finite geometry is preserved. The method performs no motion test: an implementation computes these values, while the library samples the collider association and protects its identity/lifetime. Calls reuse storage without warmed managed allocation. [The separate public consumer](../../examples/PhysicsResultConstruction/ResultConstructionChecks.cs) exercises construction, validation, atomic replacement, hit-to-miss reuse and ordinary BodyTestMotion on both built-ins. Full numerical backend extension acceptance remains open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).


`GetCollider` resolves the sampled weak object association. `GetColliderID` and `GetColliderRID` retain their sampled values after disposal; unassigned bodies have ID zero. Shape indices refer to direct owners, not compound backend fixtures. The point and normal are global, depth and travel use scene units, and velocity uses scene units per second. On a completed miss, fractions are `(1, 1)`, collider identity and contact values clear, and remainder is zero. A newly constructed result yields zero/default values until the first test. Disposed result access rejects.

## Verification and limits

Selected motion dispatch now borrows world lifetime for the whole query and validates its completed output before caller replacement or scene movement. Finite travel/contact values, ordered fractions, the requested moving RID, live enabled logical slots, same-space bodies, moving-mask/target-layer filtering, symmetric body/joint exceptions and explicit RID/object exclusions are checked. Collider object identity is sampled by the engine; supplied identity cannot retarget a result. Misses clear collision payload and must report completed motion. Same-space recursive motion rejects because the selected solver owns reusable query scratch; query failures retire borrowing without advancing or poisoning the world. Named registration executes under ADR 0103; complete numerical extension acceptance remains open.


[PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) checks server-only and scene identities, shape indices, point/normal, fractions, travel/remainder, exclusions and no-hit reset. TileMapLayerTests now verifies generated tile-layer owners on public CPU/GPU worlds, including individual body RIDs and logical shape slots; Native allocation, other platforms and owner acceptance are unverified. See [ADR 0063](../decisions/physics.md#adr-0063).


Assigned object identity is sampled with each query result, including raw server objects.
Rebinding does not retarget earlier results; disposal/collection makes object resolution null
without erasing the sampled ID. See [object associations](../components/physics-object-bindings.md).

[Registered numerical adapter](../components/physics-registered-adapter.md) now connects a public value/RID-only implementation to real scene/raw motion, fields/forces, joints, queries, contact/Area publication, private/portable history and terminal failure. Its independent circle/spring test solver owns its own state and does not wrap a built-in. Public registered factories and the typed family now execute in a separate consumer; arbitrary custom geometry and unexercised operation acceptance remain open.


[Motion result receiver identity](../components/physics-registered-adapter.md#motion-result-receiver-identity)
preserves the moving RID authored by SetCollision through selected-query validation.
A same-world result for another moving body rejects before caller publication;
no-result calls enforce the same rule. SetMotion misses retain normal requested-body
binding, and rejection restores scope for the next valid query. The numerical
motion/geometry acceptance remains broader than this identity guarantee.

The complete PhysicsMotionTests, PhysicsQueryTests and PhysicsShapeQueryTests now use explicitly selected public CPU/GPU worlds. The common suites retain scene/raw identities, view replacement and last-resource retirement, filters/exclusions, compound origin-inside and initial-overlap rules, hollow/segment/convex contact families, one-way recovery, motion fractions/travel and scene shape-owner payloads. Existing numerical bounds are unchanged. Warm gates include both owner/all-thread managed counters: 64 scene-motion, compound recovery/ray/cast/rest queries and 128 alternating span-query cycles require 0/0 bytes. Caller-owned World aliases may outlive the SceneTree; its colliders retire immediately and the view expires after the last World resource is released. Native allocations, foreign platforms, visual and final performance acceptance remain open.


[Borrowed motion output](../components/physics-registered-adapter.md#borrowed-motion-output-2026-10-14) preserves selected scratch lifetime and owner scope while retaining caller-owned snapshots, error continuation and retirement failures. The inherited disposal preflight is a reviewed C# adaptation; full numerical/foreign/hardware acceptance stays open.

[Final body-motion contact sampling](../components/physics-registered-adapter.md#final-body-motion-contact-sampling) resamples completed recovery and unsafe motion. Contact geometry is sampled after the completed recovery at the unsafe motion pose, including the query margin. The deepest eligible final contact supplies point, normal, depth, logical identities and point velocity. When requested recovery is followed by an unobstructed departure with no final contact, the result clears collision data and retains full requested motion plus recovery. Safe travel and remainder still use the safe fraction.

[Last-recovery one-way pair exclusions](../components/physics-registered-adapter.md#last-recovery-one-way-pair-exclusions) retain rejected source-resource/target-slot pairs through the remaining sweep and final contact sample. The last recovery attempt replaces the set, accepted pieces keep their logical pair eligible, and the next query starts empty.
