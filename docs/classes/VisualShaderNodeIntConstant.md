# VisualShaderNodeIntConstant

Last updated: 2026-10-21

Source: [`VisualShaderNodeIntConstant.cs`](../../src/Scene/Resources/VisualShaderNodeIntConstant.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeIntConstant
```

Inherits: [VisualShaderNodeConstant](VisualShaderNodeConstant.md).

## Description

Produces a copied int constant in an imported visual shader.

## Example

```csharp
using var node = new VisualShaderNodeIntConstant { Constant = 1 };
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
```

The surrounding graph is supplied by the caller; save and import it before drawing. Approximately equal floating constants and equal integer/Boolean constants preserve the stored value and emit no Changed event. Invalid/nonfinite requests fail before publication.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeIntConstant()`](#member-44bd7c998d7b) | Creates the node with its initial constant. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-7f45c11e64f6) | Inherited typed Resource lifecycle implementation. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-a3110a045963) | Inherited typed Resource lifecycle implementation. |
| [`public System.Int32 Constant { get; set; }`](#member-59735de90147) | Gets or replaces the authored constant. |

<a id="member-44bd7c998d7b"></a>
### `.ctor`

```csharp
public VisualShaderNodeIntConstant()
```

Creates the node with its initial constant.


<a id="member-7f45c11e64f6"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```


<a id="member-a3110a045963"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```


<a id="member-59735de90147"></a>
### `Constant`

```csharp
public System.Int32 Constant { get; set; }
```

Gets or replaces the authored constant.

Initially 0; numeric floating components must be finite.

- `System.ArgumentException`: A numeric component is nonfinite.
- `System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Graph authoring, snapshots, import and file I/O are allocating cold operations. Definition resources are single-threaded authoring objects. Access after disposal rejects ordinary reads/writes; graph disposal detaches borrowed definitions and retires its owned outputs/replacements. Immutable connection/default entries have value lifetime and are not disposable.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
