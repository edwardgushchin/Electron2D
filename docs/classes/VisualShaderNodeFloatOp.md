# VisualShaderNodeFloatOp

Last updated: 2026-10-21

Source: [`VisualShaderNodeFloatOp.cs`](../../src/Scene/Resources/VisualShaderNodeFloatOp.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeFloatOp
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Applies one binary floating-point operation to two graph inputs.

Arithmetic executes in the imported shader, including its native NaN/infinity rules.

## Example

```csharp
using var node = new VisualShaderNodeFloatOp { Operation = VisualShaderNodeFloatOp.Operator.Multiply };
node.SetInputPortDefaultValue(0, .25f);
node.SetInputPortDefaultValue(1, .5f);
```

Add the node to a graph and connect its output before saving/importing. Arithmetic executes in the shader, not in the resource setter.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeFloatOp()`](#member-8149cc356b66) | Creates an addition operation with two zero defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-601d692e825b) | Inherited typed Resource lifecycle implementation. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-31a98d1e6aa1) | Inherited typed Resource lifecycle implementation. |
| [`public Electron2D.VisualShaderNodeFloatOp.Operator Operation { get; set; }`](#member-145118a07d73) | Gets or selects the binary scalar operation. |

<a id="member-8149cc356b66"></a>
### `.ctor`

```csharp
public VisualShaderNodeFloatOp()
```

Creates an addition operation with two zero defaults.


<a id="member-601d692e825b"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```


<a id="member-31a98d1e6aa1"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```


<a id="member-145118a07d73"></a>
### `Operation`

```csharp
public Electron2D.VisualShaderNodeFloatOp.Operator Operation { get; set; }
```

Gets or selects the binary scalar operation.

Add initially.

- `System.ArgumentOutOfRangeException`: The operation is undefined.
- `System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Graph authoring, snapshots, import and file I/O are allocating cold operations. Definition resources are single-threaded authoring objects. Access after disposal rejects ordinary reads/writes; graph disposal detaches borrowed definitions and retires its owned outputs/replacements. Immutable connection/default entries have value lifetime and are not disposable.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
