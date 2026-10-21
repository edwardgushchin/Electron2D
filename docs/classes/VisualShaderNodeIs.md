# VisualShaderNodeIs

Last updated: 2026-10-22

Source: [VisualShaderNodeIs.cs](../../src/Scene/Resources/VisualShaderNodeIs.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeIs
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Tests a floating graph result for infinity or NaN.

Authored defaults remain finite. Nonfinite computed inputs retain native shader classification rules.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeIs()`](#member-f70ed7c147a6) | Creates an infinity test with an explicit zero input. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-9015aa0ce630) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-a5922dedf888) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeIs.FunctionEnum Function { get; set; }`](#member-02cc24151344) | Gets or selects the classification function without changing defaults. |

<a id="member-f70ed7c147a6"></a>
### `.ctor`

```csharp
public VisualShaderNodeIs()
```

Creates an infinity test with an explicit zero input.

<a id="member-9015aa0ce630"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-a5922dedf888"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-02cc24151344"></a>
### `Function`

```csharp
public Electron2D.VisualShaderNodeIs.FunctionEnum Function { get; set; }
```

Gets or selects the classification function without changing defaults.

Invalid values leave state unchanged. Repeated selection is quiet; observers run after commit.

IsInf initially.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The function is undefined or the count sentinel.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed requests, dormant tolerance, condition presence, transactional operand conversion, copy/archives and real software-rendered pixels. See [the component contract](../components/visual-shaders.md#comparisons-and-conditional-results) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
