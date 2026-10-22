# VisualShaderNodeTextureSDF

Last updated: 2026-10-22

Source: [VisualShaderNodeTextureSDF.cs](../../src/Scene/Resources/VisualShaderNodeTextureSDF.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeTextureSDF
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Samples the signed nearest-contour field in an imported fragment graph.

The vector input is a framebuffer pixel position; absence means (0,0). The scalar result is a signed pixel distance, negative inside a participating closed contour. Open contours have no interior. Uses the renderer's pixel-space field, including its viewport oversize/resolution and occluder policies. Only reachable fragment-stage nodes import; definition edits require reimport.

## Example

This snippet assumes an initialized project resource root.

```csharp
using Electron2D;
using var node = new VisualShaderNodeTextureSDF();
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
| [`public sealed class Electron2D.VisualShaderNodeTextureSDF`](#member-8ffec52fb672) | Samples the signed nearest-contour field in an imported fragment graph. |
| [`public VisualShaderNodeTextureSDF()`](#member-64c67894cc6a) | Creates a definition with no authored input defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-ddb268e20576) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-8ffec52fb672"></a>
### `VisualShaderNodeTextureSDF`

```csharp
public sealed class Electron2D.VisualShaderNodeTextureSDF
```

Samples the signed nearest-contour field in an imported fragment graph.

The vector input is a framebuffer pixel position; absence means (0,0). The scalar result is a signed pixel distance, negative inside a participating closed contour. Open contours have no interior. Uses the renderer's pixel-space field, including its viewport oversize/resolution and occluder policies. Only reachable fragment-stage nodes import; definition edits require reimport.

<a id="member-64c67894cc6a"></a>
### `.ctor`

```csharp
public VisualShaderNodeTextureSDF()
```

Creates a definition with no authored input defaults.

<a id="member-ddb268e20576"></a>
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
