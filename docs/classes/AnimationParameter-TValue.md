# AnimationParameter<TValue>

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.AnimationParameter<TValue>` · **Source:** [AnimationParameter.cs](../../src/Scene/Animation/AnimationParameter.cs).

**Inherits:** [AnimationParameter](AnimationParameter.md).

## Description

A typed key and default value for per-tree animation-node memory.

The generic key carries an exact typed default. Per-tree cells use the same key identity, not name-only coercion. Default array containers are cloned shallowly for each tree; array elements, custom mutable reference values and resources stay borrowed. GetDefaultValue creates another shallow array container when applicable. The definition itself is immutable; external tree writes observe readOnly.

## Example

A custom AnimationNode declares `static readonly AnimationParameter<double> Gain = new("gain", 1);`, adds it from OnGetParameterList, reads GetParameter(Gain) during OnProcess and writes it through `tree.SetParameter("node", Gain, 0.5)`. [The executable custom-node example](../../tests/Electron2D.Tests/AnimationGraphTests.cs) demonstrates the complete hook.

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationParameter`1(System.String name, TValue defaultValue, System.Boolean readOnly = false)` | Creates a new independent instance with the defaults described above. |

## Constructor Descriptions

<a id="member-c1e018090dab"></a>
### .ctor

`public AnimationParameter`1(System.String name, TValue defaultValue, System.Boolean readOnly = false)`

Creates a new independent instance with the defaults described above.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public TValue GetDefaultValue()` | Returns the default value, with an independent array container when applicable. |

## Method Descriptions

<a id="member-86aa635a4c13"></a>
### GetDefaultValue

`public TValue GetDefaultValue()`

Returns the default value, with an independent array container when applicable.

Returns: The typed default; custom mutable references and resources remain borrowed.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) owns this typed contract. [Scene animation](../components/scene-animation.md#animation-graphs) records formulas, topology, lifetime, tests and limitations. [AnimationGraphTests](../../tests/Electron2D.Tests/AnimationGraphTests.cs) verifies public managed execution and two real Wayland GPU/two compatibility host cycles with five rendered positions. Warmed scalar graph/property passes allocate zero managed bytes on the owner thread; cold preparation/result arrays, native/driver allocations and other platforms are separate. State machines, transitions, OneShot, Expression evaluation, non-property track schedulers and graph/disk/editor round trips remain applicable separate slices in coverage.
