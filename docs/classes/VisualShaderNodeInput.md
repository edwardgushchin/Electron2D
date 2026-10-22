# VisualShaderNodeInput

Last updated: 2026-10-22

Source: [VisualShaderNodeInput.cs](../../src/Scene/Resources/VisualShaderNodeInput.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeInput
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Reads a named typed canvas input in an imported visual shader stage.

Unknown input names retain the scalar-zero source role. Known inputs without a current canvas ABI producer reject import rather than silently producing a replacement value.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeInput()`](#member-df6a594a559d) | Creates a scalar-zero input operation. |
| [`public event System.Action InputTypeChanged`](#member-b7f7fe261ce3) | Occurs after a name edit changes the output's value role. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-94ac2e8f5771) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-b5818444deae) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.String GetInputRealName()`](#member-58f82dfcb768) | Returns the source symbol corresponding to the selected input. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-30ef36d55514) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.String InputName { get; set; }`](#member-2ae4f40562df) | Gets or replaces the case-sensitive canvas input name. |

<a id="member-df6a594a559d"></a>
### `.ctor`

```csharp
public VisualShaderNodeInput()
```

Creates a scalar-zero input operation.

<a id="member-b7f7fe261ce3"></a>
### `InputTypeChanged`

```csharp
public event System.Action InputTypeChanged
```

Occurs after a name edit changes the output's value role.

<a id="member-94ac2e8f5771"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-b5818444deae"></a>
### `Dispose`

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

<a id="member-58f82dfcb768"></a>
### `GetInputRealName`

```csharp
public System.String GetInputRealName()
```

Returns the source symbol corresponding to the selected input.

The uppercase canvas symbol, or an empty string for an unknown input.

Exceptions:

- `T:System.ObjectDisposedException`: The node is disposed.

<a id="member-30ef36d55514"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-2ae4f40562df"></a>
### `InputName`

```csharp
public System.String InputName { get; set; }
```

Gets or replaces the case-sensitive canvas input name.

Changed is emitted after commitment. InputTypeChanged then reports a changed value role.

Initially [None]; examples are uv, color, vertex and time.

Exceptions:

- `T:System.ArgumentException`: The name exceeds 128 characters or contains a control character.
- `T:System.ArgumentNullException`: The name is null.
- `T:System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed numeric and opaque ports, sampler policies, resource aliases and portable archives. Its image fixtures check real software-rendered pixels, color transfer, independent layers/mips and refreshed native storage. See [the component contract](../components/visual-shaders.md#sampled-images-and-canvas-channels) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
