# VisualShaderNodeUVPolarCoord

Last updated: 2026-10-22

Source: [VisualShaderNodeUVPolarCoord.cs](../../src/Scene/Resources/VisualShaderNodeUVPolarCoord.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeUVPolarCoord
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Converts UV coordinates to wrapped radius and angle in an imported canvas graph.

Inputs are UV, center, zoom and angular repeat. Unconnected UV uses the current stage UV; stored numeric UV defaults remain source data. Results wrap into [0,1) using floor-based modulo. The angle at the exact center retains native shader semantics. Edits require reimport.

## Example

This snippet assumes an initialized project resource root.

```csharp
using Electron2D;
using var node = new VisualShaderNodeUVPolarCoord();
node.SetInputPortDefaultValue(2, .5f);
using var graph = new VisualShader();
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, VisualShader.NodeIDOutput, 0);
ResourceSaver.Save(graph, "res://Shaders/effect-source.e2dres", SaverFlags.BundleResources);
```

Import through [the graph workflow](../components/visual-shaders.md#author-import-and-execute), then load the compiled resource and assign it to ShaderMaterial.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShaderNodeUVPolarCoord`](#member-99c2daad6c4d) | Converts UV coordinates to wrapped radius and angle in an imported canvas graph. |
| [`public VisualShaderNodeUVPolarCoord()`](#member-dc8117c9a611) | Creates implicit UV with center (0.5,0.5), unit zoom and unit angular repeat. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-e4b0052819f3) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-99c2daad6c4d"></a>
### `VisualShaderNodeUVPolarCoord`

```csharp
public sealed class Electron2D.VisualShaderNodeUVPolarCoord
```

Converts UV coordinates to wrapped radius and angle in an imported canvas graph.

Inputs are UV, center, zoom and angular repeat. Unconnected UV uses the current stage UV; stored numeric UV defaults remain source data. Results wrap into [0,1) using floor-based modulo. The angle at the exact center retains native shader semantics. Edits require reimport.

<a id="member-dc8117c9a611"></a>
### `.ctor`

```csharp
public VisualShaderNodeUVPolarCoord()
```

Creates implicit UV with center (0.5,0.5), unit zoom and unit angular repeat.

<a id="member-e4b0052819f3"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer exercises both canvas stages, selector transitions, errors, source/compiled archives and composed image-effect materials. See [the operation contract](../components/visual-shaders.md#uv-and-color-image-effects) for formulas, native arithmetic and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
