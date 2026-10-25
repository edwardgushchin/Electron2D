# VisualShaderNodeInput

Last updated: 2026-10-25

Source: [VisualShaderNodeInput.cs](../../src/Scene/Resources/VisualShaderNodeInput.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeInput
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Reads a named typed input in an imported visual shader stage.

Unknown or out-of-context names retain the scalar-zero role. A borrowing graph resolves the port type independently for its stage; changing one borrower does not bind this definition to it. InputTypeChanged reports the named semantic role. Known inputs in their applicable context require a real renderer producer.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShaderNodeInput`](#member-f87639042e23) | Reads a named typed input in an imported visual shader stage. |
| [`public VisualShaderNodeInput()`](#member-df6a594a559d) | Creates a scalar-zero input operation. |
| [`public event System.Action InputTypeChanged`](#member-b7f7fe261ce3) | Occurs after a name edit changes the output's value role. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-94ac2e8f5771) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-b5818444deae) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.String GetInputRealName()`](#member-58f82dfcb768) | Returns the source symbol corresponding to the selected input. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-30ef36d55514) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.String InputName { get; set; }`](#member-2ae4f40562df) | Gets or replaces the case-sensitive input name. |

<a id="member-f87639042e23"></a>
### `VisualShaderNodeInput`

```csharp
public sealed class Electron2D.VisualShaderNodeInput
```

Reads a named typed input in an imported visual shader stage.

Unknown or out-of-context names retain the scalar-zero role. A borrowing graph resolves the port type independently for its stage; changing one borrower does not bind this definition to it. InputTypeChanged reports the named semantic role. Known inputs in their applicable context require a real renderer producer.

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

The uppercase named symbol, or an empty string for an unknown input; this query has no borrowing-stage context.

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

Gets or replaces the case-sensitive input name.

Changed is emitted after commitment. InputTypeChanged then reports a changed value role.

Initially [None]; examples are uv, color, velocity, number and time.

Exceptions:

- `T:System.ArgumentException`: The name exceeds 128 characters or contains a control character.
- `T:System.ArgumentNullException`: The name is null.
- `T:System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer and native particle integration corpus exercise typed defaults, copies, factories, source/compiled archives and executable compute/blit graphs. See [the domain contract](../components/visual-shaders.md#particle-and-writable-texture-graph-domains) for ports, units, scope, control flow, source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)

Canvas command texture size uses the renderer-owned float2 `TEXTURE_PIXEL_SIZE` contract in both canvas stages; it follows the resolved diffuse image logical metadata, independently of material samplers. See [command texture pixel size](../components/visual-shaders.md#command-texture-pixel-size) for authoring, stage/lifetime rules and verification limits.
