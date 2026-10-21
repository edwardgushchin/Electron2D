# VisualShaderNodeInput

Last updated: 2026-10-21

Source: [`VisualShaderNodeInput.cs`](../../src/Scene/Resources/VisualShaderNodeInput.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeInput
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Reads a named numeric canvas input in an imported visual shader stage.

Unknown input names retain the scalar-zero source role. Known inputs without a current canvas ABI producer reject import rather than silently producing a replacement value.

## Example

```csharp
using var node = new VisualShaderNodeInput { InputName = "uv" };
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
```

The surrounding graph is supplied by the caller. Canvas vertex coordinates follow the existing framebuffer-pixel vertex ABI. Unknown names retain the scalar-zero source role. Recognized but unsupported producers reject authoring or import explicitly.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeInput()`](#member-df6a594a559d) | Creates a scalar-zero input operation. |
| [`public event System.Action InputTypeChanged`](#member-b7f7fe261ce3) | Occurs after a name edit changes the output's numeric role. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-94ac2e8f5771) | Inherited typed Resource lifecycle implementation. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-b5818444deae) | Inherited typed Resource lifecycle implementation. |
| [`public System.String GetInputRealName()`](#member-58f82dfcb768) | Returns the source symbol corresponding to the selected input. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-30ef36d55514) | Inherited typed Resource lifecycle implementation. |
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

Occurs after a name edit changes the output's numeric role.


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

- `System.ObjectDisposedException`: The node is disposed.

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

Changed is emitted after commitment. InputTypeChanged then reports a changed numeric role.

Initially [None]; examples are uv, color, vertex and time.

- `System.ArgumentException`: The name exceeds 128 characters or contains a control character.
- `System.ArgumentNullException`: The name is null.
- `System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Graph authoring, snapshots, import and file I/O are allocating cold operations. Definition resources are single-threaded authoring objects. Access after disposal rejects ordinary reads/writes; graph disposal detaches borrowed definitions and retires its owned outputs/replacements. Immutable connection/default entries have value lifetime and are not disposable.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
