# VisualShaderNodeVectorOp

Last updated: 2026-10-21

Source: [`VisualShaderNodeVectorOp.cs`](../../src/Scene/Resources/VisualShaderNodeVectorOp.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeVectorOp
```

Inherits: [VisualShaderNodeVectorBase](VisualShaderNodeVectorBase.md).

## Description

Applies one binary component-wise vector operation to two graph inputs.

Arithmetic executes in the imported shader, including its native NaN/infinity rules.

## Example

```csharp
using var node = new VisualShaderNodeVectorOp { OperationType = VisualShaderNodeVectorBase.OpType.Vector2 };
node.SetInputPortDefaultValue(0, new Vector2(.25f, .5f));
node.SetInputPortDefaultValue(1, new Vector2(.5f, .25f));
```

Cross product requires three components; unsupported width combinations reject explicitly.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeVectorOp()`](#member-535aceb313b6) | Creates an addition operation with two zero Vector3 defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-c92b3046eb9e) | Inherited typed Resource lifecycle implementation. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-1bf1a522679d) | Inherited typed Resource lifecycle implementation. |
| [`public Electron2D.VisualShaderNodeVectorOp.Operator Operation { get; set; }`](#member-537ebcd43de8) | Gets or selects the binary vector operation. |

<a id="member-535aceb313b6"></a>
### `.ctor`

```csharp
public VisualShaderNodeVectorOp()
```

Creates an addition operation with two zero Vector3 defaults.


<a id="member-c92b3046eb9e"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```


<a id="member-1bf1a522679d"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```


<a id="member-537ebcd43de8"></a>
### `Operation`

```csharp
public Electron2D.VisualShaderNodeVectorOp.Operator Operation { get; set; }
```

Gets or selects the binary vector operation.

Add initially.

- `System.ArgumentOutOfRangeException`: The operation is undefined.
- `System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Graph authoring, snapshots, import and file I/O are allocating cold operations. Definition resources are single-threaded authoring objects. Access after disposal rejects ordinary reads/writes; graph disposal detaches borrowed definitions and retires its owned outputs/replacements. Immutable connection/default entries have value lifetime and are not disposable.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
