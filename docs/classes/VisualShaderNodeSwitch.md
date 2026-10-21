# VisualShaderNodeSwitch

Last updated: 2026-10-22

Source: [VisualShaderNodeSwitch.cs](../../src/Scene/Resources/VisualShaderNodeSwitch.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeSwitch
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Selects between two numeric or Boolean graph inputs using a Boolean condition.

Inputs are condition, true value and false value. Floating selections use native interpolation; integer and Boolean selections retain exact values. Type changes preserve the condition's presence and value.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeSwitch()`](#member-b94539c6aefb) | Creates a floating definition with false condition, true value one and false value zero. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-a9c27ba92a67) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-f72615693d11) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderValueType OpType { get; set; }`](#member-412cc62fba22) | Gets or selects the result type, converting both value defaults atomically. |

<a id="member-b94539c6aefb"></a>
### `.ctor`

```csharp
public VisualShaderNodeSwitch()
```

Creates a floating definition with false condition, true value one and false value zero.

<a id="member-a9c27ba92a67"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-f72615693d11"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-412cc62fba22"></a>
### `OpType`

```csharp
public Electron2D.VisualShaderValueType OpType { get; set; }
```

Gets or selects the result type, converting both value defaults atomically.

Vector expansion repeats the last component. Boolean conversions retain target initial values. Unchanged selection is quiet; Changed runs after the new type and defaults commit.

Float initially. A changed type recreates absent value defaults; the condition is unchanged.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The selection is undefined or the count sentinel.
- `T:System.OverflowException`: An operand cannot be represented by the target integer type; state is unchanged.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed requests, dormant tolerance, condition presence, transactional operand conversion, copy/archives and real software-rendered pixels. See [the component contract](../components/visual-shaders.md#comparisons-and-conditional-results) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
