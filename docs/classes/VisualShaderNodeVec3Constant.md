# VisualShaderNodeVec3Constant

Last updated: 2026-10-21

Source: [`VisualShaderNodeVec3Constant.cs`](../../src/Scene/Resources/VisualShaderNodeVec3Constant.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeVec3Constant
```

Inherits: [VisualShaderNodeConstant](VisualShaderNodeConstant.md).

## Description

Produces a copied Vector3 constant in an imported visual shader.

## Example

```csharp
using var node = new VisualShaderNodeVec3Constant { Constant = new Vector3(.25f, .5f, .75f) };
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
```

The surrounding graph is supplied by the caller; save and import it before drawing. Approximately equal floating constants and equal integer/Boolean constants preserve the stored value and emit no Changed event. Invalid/nonfinite requests fail before publication.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeVec3Constant()`](#member-838db0f62256) | Creates the node with its initial constant. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-d0f64c7517a5) | Inherited typed Resource lifecycle implementation. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-2222f6bb2ec4) | Inherited typed Resource lifecycle implementation. |
| [`public Electron2D.Vector3 Constant { get; set; }`](#member-c9856e5bb21f) | Gets or replaces the authored constant. |

<a id="member-838db0f62256"></a>
### `.ctor`

```csharp
public VisualShaderNodeVec3Constant()
```

Creates the node with its initial constant.


<a id="member-d0f64c7517a5"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```


<a id="member-2222f6bb2ec4"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```


<a id="member-c9856e5bb21f"></a>
### `Constant`

```csharp
public Electron2D.Vector3 Constant { get; set; }
```

Gets or replaces the authored constant.

Initially Vector3.Zero; numeric floating components must be finite.

- `System.ArgumentException`: A numeric component is nonfinite.
- `System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Graph authoring, snapshots, import and file I/O are allocating cold operations. Definition resources are single-threaded authoring objects. Access after disposal rejects ordinary reads/writes; graph disposal detaches borrowed definitions and retires its owned outputs/replacements. Immutable connection/default entries have value lifetime and are not disposable.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
