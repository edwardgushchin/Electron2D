# PhysicsMaterial

Last updated: 2026-10-05

**Inherits:** [Resource](Resource.md), ElectronObject

- **Source:** [PhysicsMaterial.cs](../../src/Scene/Resources/PhysicsMaterial.cs)
- **Declaration:** `public sealed class PhysicsMaterial : Resource`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Description

A reusable surface material for a `RigidBody` or `StaticBody`. The body borrows the resource through `PhysicsMaterialOverride`; the caller owns its lifetime. A property edit raises `Changed`, invalidates attached fixtures, and affects contacts after the next fixed physics step. Disposing a borrowed material removes the override and restores friction one and bounce zero. Scene packing preserves a shared borrowed resource; `Duplicate()` copies its four values independently.

## Example

```csharp
using var material = new PhysicsMaterial { Friction = 0.25f, Bounce = 0.8f };
var floor = new StaticBody { PhysicsMaterialOverride = material };
// Add CollisionShape geometry and attach the floor to a SceneTree.
```

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `public PhysicsMaterial()` | — | Creates a detached material resource. |
| `public float Friction { get; set; }` | 1 | Finite friction coefficient. |
| `public float Bounce { get; set; }` | 0 | Finite bounce coefficient. |
| `public bool Rough { get; set; }` | false | Makes this surface's friction take precedence over nonrough friction. |
| `public bool Absorbent { get; set; }` | false | Subtracts this surface's bounce from the other surface's bounce. |
| `protected override Resource CreateDuplicateInstance()` | — | Creates a separate material for resource copying. |
| `protected override void CopyCustomStateTo(...)` | — | Copies all four material values. |

## Property descriptions

<a id="friction"></a>
### `Friction` and `Rough`

Two nonrough surfaces combine with the lower friction. One rough surface supplies its friction; two rough surfaces combine with the higher friction. A negative assigned friction reverses the effective rough marker, as in the reference's signed material convention. The public value remains unchanged; `NaN` and infinity throw `ArgumentOutOfRangeException` before mutation. The usual range is zero to one, but finite values outside it are accepted.

<a id="bounce"></a>
### `Bounce` and `Absorbent`

Bounce values add and clamp to zero through one, including at low impact speed. An absorbent surface subtracts its bounce; negative assigned values reverse the effective absorbent marker. The public value remains unchanged. `NaN` and infinity throw `ArgumentOutOfRangeException` before mutation. The usual range is zero to one, but finite values outside it are accepted.

Every successful property assignment raises `Changed`, including an assignment of the existing value. If a subscriber throws, the assigned value remains; attached bodies still detect the new material revision at their next step. Material resource state and callbacks have no implicit synchronization; attached body mutation and stepping use the owning scene thread.

## Lifecycle and verification

The caller retains and disposes the resource. Bodies unsubscribe when replaced, disposed, or after the material is disposed. `PhysicsMaterialOverride = null` restores the default surface. A failed earlier `Disposed` subscriber cannot retain a disposed override: its getter reports null and the next step clears the stale reference. Live material changes recreate fixtures before the next step; continuous contacts may be reestablished by that rebuild. [PhysicsMaterialTests](../../tests/Electron2D.Tests/PhysicsMaterialTests.cs) checks defaults, invalid values, notification failure, duplication, packing, bounce/absorbent mixing, rough friction, live edits, borrowed disposal and 64 warmed resting and active-contact frames each with zero managed allocations on Linux/.NET 8. Native allocator counts, other platforms and visual acceptance remain unverified.

[ADR 0054](../decisions/physics-backends.md#adr-0054) defines the internal physics backend. [PhysicsMaterial coverage](../coverage/classes/PhysicsMaterial.md) records the reference mapping.

## Body-local server coefficients

PhysicsServer signed friction/bounce setters can override one body's fixtures without mutating this borrowed resource or other borrowers. Assignment, revision or disposal reloads both body coefficients; polling at body preparation recovers a change whose earlier subscriber threw before the body handler. [PhysicsBodyParameterTests](../../tests/Electron2D.Tests/PhysicsBodyParameterTests.cs) verifies isolation, signed contact behavior and reload under [ADR 0076](../decisions/physics-mass.md#adr-0076).

## Typed file integration

See [resource-file contracts](../components/resource-files.md) for registered typed schemas, cache/UID resolution, file-root and scene-instance ownership, public extension hooks and exercised verification. File operations allocate outside frame processing. UID paths resolve through the permanent catalog before directory-backed path resolution; unknown UIDs fail explicitly. The archive profile does not add an editor, arbitrary import/remap rules or every resource schema.

## File integration API additions

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |

## Method Descriptions

<a id="member-3987c8c97192"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Overrides append or replace descriptors; they must not yield null entries.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.
