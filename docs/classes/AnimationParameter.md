# AnimationParameter

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public abstract class Electron2D.AnimationParameter` · **Source:** [AnimationParameter.cs](../../src/Scene/Animation/AnimationParameter.cs).

**Inherited By:** [AnimationParameter<TValue>](AnimationParameter-TValue.md).

## Description

Immutable animation-domain parameter metadata with no untyped value accessor.

Immutable name/type/read-only schema metadata for animation-domain memory. It has no universal value accessor or setter. Concrete AnimationParameter instances bind exact typed cells on graph preparation. Name must be nonblank; ValueType must be non-null.

## Example

A custom AnimationNode declares `static readonly AnimationParameter<double> Gain = new("gain", 1);`, adds it from OnGetParameterList, reads GetParameter(Gain) during OnProcess and writes it through `tree.SetParameter("node", Gain, 0.5)`. [The executable custom-node example](../../tests/Electron2D.Tests/AnimationGraphTests.cs) demonstrates the complete hook.

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `protected AnimationParameter(System.String name, System.Type valueType, System.Boolean readOnly)` | Initializes a named parameter definition. |

## Constructor Descriptions

<a id="member-2061026dde26"></a>
### .ctor

`protected AnimationParameter(System.String name, System.Type valueType, System.Boolean readOnly)`

Initializes a named parameter definition.

name: The nonblank local parameter name.

valueType: The exact value type.

readOnly: Whether external tree callers may write this parameter.

System.ArgumentException: The parameter name is null or blank.

System.ArgumentNullException: The value type is null.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean IsReadOnly { get;  }` | Gets whether external callers may write the parameter. |
| `public System.String Name { get;  }` | Gets the exact local name. |
| `public System.Type ValueType { get;  }` | Gets the exact parameter value type. |

## Property Descriptions

<a id="member-10c6f1f0995a"></a>
### IsReadOnly

`public System.Boolean IsReadOnly { get;  }`

Gets whether external callers may write the parameter.

Value: The typed value described in the summary.

<a id="member-d9339a1e2cab"></a>
### Name

`public System.String Name { get;  }`

Gets the exact local name.

Value: The typed value described in the summary.

<a id="member-8d92b6ac32a2"></a>
### ValueType

`public System.Type ValueType { get;  }`

Gets the exact parameter value type.

Value: The typed value described in the summary.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) owns this typed contract. [Scene animation](../components/scene-animation.md#animation-graphs) records formulas, topology, lifetime, tests and limitations. [AnimationGraphTests](../../tests/Electron2D.Tests/AnimationGraphTests.cs) verifies public managed execution and two real Wayland GPU/two compatibility host cycles with five rendered positions. Warmed scalar graph/property passes allocate zero managed bytes on the owner thread; cold preparation/result arrays, native/driver allocations and other platforms are separate. State machines/grouped controllers, Expression evaluation, non-property track schedulers and graph/disk/editor round trips remain applicable separate slices in coverage.
