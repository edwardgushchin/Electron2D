# VisualShaderNodeSDFToScreenUV

Last updated: 2026-10-22

Source: [VisualShaderNodeSDFToScreenUV.cs](../../src/Scene/Resources/VisualShaderNodeSDFToScreenUV.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeSDFToScreenUV
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Converts framebuffer pixel coordinates to normalized screen UV in a fragment graph.

The vector input is a framebuffer pixel position, absent as (0,0). The vector output divides each component by the framebuffer size; offscreen coordinates remain unclamped. Conversion uses framebuffer dimensions and does not request field generation. Only reachable fragment-stage nodes import; definition edits require reimport.

## Example

This snippet assumes an initialized project resource root.

```csharp
using Electron2D;
using var node = new VisualShaderNodeSDFToScreenUV();
node.SetInputPortDefaultValue(0, new Vector2(64, 32));
using var graph = new VisualShader();
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, VisualShader.NodeIDOutput, 0);
ResourceSaver.Save(graph, "res://Shaders/sdf-source.e2dres", SaverFlags.BundleResources);
```

Import through [the graph workflow](../components/visual-shaders.md#author-import-and-execute), then load the compiled resource and assign it to ShaderMaterial.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShaderNodeSDFToScreenUV`](#member-b8416287526b) | Converts framebuffer pixel coordinates to normalized screen UV in a fragment graph. |
| [`public VisualShaderNodeSDFToScreenUV()`](#member-5030e5125267) | Creates a definition with no authored input defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-856e3e6e9292) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-b8416287526b"></a>
### `VisualShaderNodeSDFToScreenUV`

```csharp
public sealed class Electron2D.VisualShaderNodeSDFToScreenUV
```

Converts framebuffer pixel coordinates to normalized screen UV in a fragment graph.

The vector input is a framebuffer pixel position, absent as (0,0). The vector output divides each component by the framebuffer size; offscreen coordinates remain unclamped. Conversion uses framebuffer dimensions and does not request field generation. Only reachable fragment-stage nodes import; definition edits require reimport.

<a id="member-5030e5125267"></a>
### `.ctor`

```csharp
public VisualShaderNodeSDFToScreenUV()
```

Creates a definition with no authored input defaults.

<a id="member-856e3e6e9292"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer exercises typed defaults, copies, factories, unsupported vertex imports, fresh archives and renderer-driven SDF effects. See [the operation contract](../components/visual-shaders.md#canvas-sdf-graphs) for ports, pixel units, sampling/raymarch rules and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
