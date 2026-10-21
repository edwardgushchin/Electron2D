# VisualShaderNodeMatrixFunc

Last updated: 2026-10-22

Source: [VisualShaderNodeMatrixFunc.cs](../../src/Scene/Resources/VisualShaderNodeMatrixFunc.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeMatrixFunc
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Applies native inverse or transpose to a numeric matrix input.

Matrix4x4 is numeric shader data. Arithmetic retains native precision and undefined-domain rules; inverse requires a nonsingular input for portable results.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeMatrixFunc()`](#member-5e1f3ef5bfc5) | Creates the definition with an explicit identity matrix default. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-4e2fa4fc42a0) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-455d670f61a6) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeMatrixFunc.FunctionEnum Function { get; set; }`](#member-beab66f077f2) | Gets or selects the native operation without changing stored defaults. |

<a id="member-5e1f3ef5bfc5"></a>
### `.ctor`

```csharp
public VisualShaderNodeMatrixFunc()
```

Creates the definition with an explicit identity matrix default.

<a id="member-4e2fa4fc42a0"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-455d670f61a6"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-beab66f077f2"></a>
### `Function`

```csharp
public Electron2D.VisualShaderNodeMatrixFunc.FunctionEnum Function { get; set; }
```

Gets or selects the native operation without changing stored defaults.

An unchanged selection is quiet. The new value commits before Changed observers execute.

Inverse initially.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The value is undefined or the count sentinel.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed requests, all sixteen matrix components, copy/archive state, row/column layout and real software-rendered pixels. See [the component contract](../components/visual-shaders.md#comparisons-and-conditional-results) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
