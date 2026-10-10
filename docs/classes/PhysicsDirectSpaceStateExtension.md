# PhysicsDirectSpaceStateExtension

Last updated: 2026-10-10

**Declaration:** `public abstract partial class PhysicsDirectSpaceStateExtension : PhysicsDirectSpaceState` · **Inherits:** [PhysicsDirectSpaceState](PhysicsDirectSpaceState.md) · **Component:** [Space-query extensions](../components/physics-space-extensions.md)

**Source:** [context/dispatch](../../src/Servers/Physics/PhysicsDirectSpaceStateExtension.cs), [protected hooks](../../src/Servers/Physics/PhysicsDirectSpaceStateExtension.Hooks.cs)

## Description

Implements the complete direct-space query family in consumer code for one borrowed live engine space. The protected constructor validates space identity, owner thread, solver phase and failure before binding. The view owns no body, shape or world and cannot retarget. Inherited public methods prepare current authored geometry and invoke the hooks; the server's cached built-in view remains separate.

Input values are sampled before user code. Output spans are temporary library scratch; never retain them. The library validates complete output before changing caller storage, including current active RID/shape membership, filters, point canvas identity, ordered unique hits, counts, finite points and safe/unsafe fractions. Object associations are sampled again before publication. Hooks compute actual nearest/deepest/intersection semantics and must honor synchronous exclusions.

## Example

The [complete independent circle implementation](../../tests/PhysicsSpaceExtension.Consumer/CircleQueries.cs) and [consumer checks](../../tests/PhysicsSpaceExtension.Consumer/Program.cs) compile as a separate public-only assembly. With that implementation and its authored actors/query shape in scope:

```csharp
using PhysicsDirectSpaceState view = new CircleQueries(world.Space, actors, queryShape);
using var ray = PhysicsRayQueryParameters.Create(new(-20, 0), new(80, 0));
PhysicsRayResult? nearest = view.IntersectRay(ray);
```

The fixture implements circle geometry; that is a limit of the example algorithm, not an engine extension restriction. All inherited direct-space operations remain available through the base type.

## API summary

| Declaration | Contract |
| --- | --- |
| `protected PhysicsDirectSpaceStateExtension(RID space)` | Borrow and bind one live engine space. |
| `public bool IsBodyExcludedFromQuery(RID body)` | Use the innermost synchronous exclusions; false outside queries. |
| `protected abstract PhysicsRayResult? IntersectRayCore(Vector2 from, Vector2 to, uint collisionMask, bool collideWithBodies, bool collideWithAreas, bool hitFromInside)` | Compute the nearest ray hit from finite global endpoints and layer/body/Area/inside policy; return a typed hit or null. The normal uses global axes; an accepted inside hit has a zero normal. |
| `protected abstract int IntersectPointCore(Vector2 position, ulong canvasInstanceID, uint collisionMask, bool collideWithBodies, bool collideWithAreas, Span<PhysicsPointResult> results)` | Compute filled shapes containing the global point. Canvas identity is exact; zero selects the default canvas. Fill at most the span length with the first unique RID/shape-ordered hits, then return the written count. |
| `protected abstract int IntersectShapeCore(RID shape, Transform transform, Vector2 motion, float margin, uint collisionMask, bool collideWithBodies, bool collideWithAreas, Span<PhysicsShapeResult> results)` | Compute intersected logical shape owners across the live shape RID, unit-scale global pose and swept motion, expanded by the nonnegative margin. Fill at most the span length with first unique RID/shape-ordered hits; return the written count. |
| `protected abstract (float SafeFraction, float UnsafeFraction) CastMotionCore(RID shape, Transform transform, Vector2 motion, float margin, uint collisionMask, bool collideWithBodies, bool collideWithAreas)` | Compute the first new collision bracket, ignoring initial overlaps. Return finite ordered fractions in [0,1], with (1,1) as the no-new-hit sentinel. The tuple replaces borrowed raw pointer outputs. |
| `protected abstract int CollideShapeCore(RID shape, Transform transform, Vector2 motion, float margin, uint collisionMask, bool collideWithBodies, bool collideWithAreas, Span<Vector2> results)` | Compute contact-point pairs over the live query shape and sweep. The borrowed span is even: query point then collider point per contact. Return a complete pair count at most half the span length. All supplied points must be finite. |
| `protected abstract PhysicsRestInfo? GetRestInfoCore(RID shape, Transform transform, Vector2 motion, float margin, uint collisionMask, bool collideWithBodies, bool collideWithAreas)` | Compute the deepest eligible contact over the live query shape and sweep. Return a typed collider/slot, global point/normal and collider point velocity, or null when no contact qualifies. |
| `protected sealed override void ValidateDisposal()` | Reject disposal while a hook is borrowed. |
| `protected override void Dispose(bool disposing)` | Release retained query scratch. |

## Constructor and member descriptions

### PhysicsDirectSpaceStateExtension

`protected PhysicsDirectSpaceStateExtension(RID space)` binds an existing space RID from World.Space or PhysicsServer.SpaceCreate. The constructor requires its current owner outside solving; freed or wrong resource identities and failed worlds reject. It does not allocate a new physical world or forge resource identity. Binding cannot retarget.

### IsBodyExcludedFromQuery

`public bool IsBodyExcludedFromQuery(RID body)` tests a physical identity against the innermost invocation's exclusions. Nesting replaces then restores the set in finally, including inner errors. Outside queries it returns false after normal lifetime/owner guards. It does not allocate or manufacture a RID.

### IntersectRayCore

`protected abstract PhysicsRayResult? IntersectRayCore(Vector2 from, Vector2 to, uint collisionMask, bool collideWithBodies, bool collideWithAreas, bool hitFromInside)`

Compute the nearest ray hit from finite global endpoints and layer/body/Area/inside policy; return a typed hit or null. The normal uses global axes; an accepted inside hit has a zero normal. Called synchronously on the prepared world owner. Use IsBodyExcludedFromQuery for the current exclusions. Inputs are engine values; shapes are borrowed live identities and native handles never enter the signature. Hook exceptions leave caller output unchanged.

### IntersectPointCore

`protected abstract int IntersectPointCore(Vector2 position, ulong canvasInstanceID, uint collisionMask, bool collideWithBodies, bool collideWithAreas, Span<PhysicsPointResult> results)`

Compute filled shapes containing the global point. Canvas identity is exact; zero selects the default canvas. Fill at most the span length with the first unique RID/shape-ordered hits, then return the written count. Called synchronously on the prepared world owner. Use IsBodyExcludedFromQuery for the current exclusions. Inputs are engine values; shapes are borrowed live identities and native handles never enter the signature. Hook exceptions leave caller output unchanged.

### IntersectShapeCore

`protected abstract int IntersectShapeCore(RID shape, Transform transform, Vector2 motion, float margin, uint collisionMask, bool collideWithBodies, bool collideWithAreas, Span<PhysicsShapeResult> results)`

Compute intersected logical shape owners across the live shape RID, unit-scale global pose and swept motion, expanded by the nonnegative margin. Fill at most the span length with first unique RID/shape-ordered hits; return the written count. Called synchronously on the prepared world owner. Use IsBodyExcludedFromQuery for the current exclusions. Inputs are engine values; shapes are borrowed live identities and native handles never enter the signature. Hook exceptions leave caller output unchanged.

### CastMotionCore

`protected abstract (float SafeFraction, float UnsafeFraction) CastMotionCore(RID shape, Transform transform, Vector2 motion, float margin, uint collisionMask, bool collideWithBodies, bool collideWithAreas)`

Compute the first new collision bracket, ignoring initial overlaps. Return finite ordered fractions in [0,1], with (1,1) as the no-new-hit sentinel. The tuple replaces borrowed raw pointer outputs. Called synchronously on the prepared world owner. Use IsBodyExcludedFromQuery for the current exclusions. Inputs are engine values; shapes are borrowed live identities and native handles never enter the signature. Hook exceptions leave caller output unchanged.

### CollideShapeCore

`protected abstract int CollideShapeCore(RID shape, Transform transform, Vector2 motion, float margin, uint collisionMask, bool collideWithBodies, bool collideWithAreas, Span<Vector2> results)`

Compute contact-point pairs over the live query shape and sweep. The borrowed span is even: query point then collider point per contact. Return a complete pair count at most half the span length. All supplied points must be finite. Called synchronously on the prepared world owner. Use IsBodyExcludedFromQuery for the current exclusions. Inputs are engine values; shapes are borrowed live identities and native handles never enter the signature. Hook exceptions leave caller output unchanged.

### GetRestInfoCore

`protected abstract PhysicsRestInfo? GetRestInfoCore(RID shape, Transform transform, Vector2 motion, float margin, uint collisionMask, bool collideWithBodies, bool collideWithAreas)`

Compute the deepest eligible contact over the live query shape and sweep. Return a typed collider/slot, global point/normal and collider point velocity, or null when no contact qualifies. Called synchronously on the prepared world owner. Use IsBodyExcludedFromQuery for the current exclusions. Inputs are engine values; shapes are borrowed live identities and native handles never enter the signature. Hook exceptions leave caller output unchanged.

### ValidateDisposal and Dispose

The sealed disposal validator rejects disposal during an active hook; derived implementations cannot bypass this guard. The ordinary Dispose(bool) override releases pooled scratch. Derived implementations should call base after releasing their own resources. Disposing the view never releases the world or collider resources.

## Lifecycle, errors and verification

Every query requires the bound world owner outside solving/failure; disposed-view and expired-world access reject. A hook may query the same view recursively using separate retained buffers, and may inspect live public physics state. It cannot dispose its borrowed view, change world bindings, release the physical world, capture/restore its checkpoints or recursively run an active physical interval. Balanced finally restores context after user/result-validation errors.

Invalid counts, duplicate/unordered hits, wrong-world/filter/disabled results, nonfinite pairs and invalid fractions throw before output publication. Existing result constructors retain invalid-RID/shape/vector exceptions. Unused caller storage and odd contact tails remain unchanged. Result references are cleared after each invocation. Unchanged warmed scalar/span calls reuse storage; a larger output, deeper nesting, structural edits, user work or nonempty caller-owned arrays can allocate. ShapeGetData deliberately copies data; retained implementation geometry avoids that hot-path allocation.

The separate consumer exercises CPU/GPU and no-device CPU queries, every inherited overload family, real scene/raw/Area geometry and physical motion, policy changes, nesting/failure, output validation, stale association resampling and owner/lifetime guards. Native allocation and foreign platforms remain unverified. Registered-backend factory integration, direct-body extensions and custom geometry remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).
