# VisualShaderNodeScreenUVToSDF

Last updated: 2026-10-22

Source: [VisualShaderNodeScreenUVToSDF.cs](../../src/Scene/Resources/VisualShaderNodeScreenUVToSDF.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeScreenUVToSDF
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Converts normalized screen UV to framebuffer pixel coordinates in a fragment graph.

An unconnected vector input uses the current fragment screen UV even when an authored numeric default is stored. A connected input is multiplied by framebuffer size without clamping. Conversion uses framebuffer dimensions and does not request field generation. Only reachable fragment-stage nodes import; definition edits require reimport.

## Example

This snippet assumes an initialized project resource root.

```csharp
using Electron2D;
using var node = new VisualShaderNodeScreenUVToSDF();
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
| [`public sealed class Electron2D.VisualShaderNodeScreenUVToSDF`](#member-f79107b00ee9) | Converts normalized screen UV to framebuffer pixel coordinates in a fragment graph. |
| [`public VisualShaderNodeScreenUVToSDF()`](#member-6aa48d8d8ec5) | Creates a definition with no authored input defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-9ca3bbdb955a) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-f79107b00ee9"></a>
### `VisualShaderNodeScreenUVToSDF`

```csharp
public sealed class Electron2D.VisualShaderNodeScreenUVToSDF
```

Converts normalized screen UV to framebuffer pixel coordinates in a fragment graph.

An unconnected vector input uses the current fragment screen UV even when an authored numeric default is stored. A connected input is multiplied by framebuffer size without clamping. Conversion uses framebuffer dimensions and does not request field generation. Only reachable fragment-stage nodes import; definition edits require reimport.

<a id="member-6aa48d8d8ec5"></a>
### `.ctor`

```csharp
public VisualShaderNodeScreenUVToSDF()
```

Creates a definition with no authored input defaults.

<a id="member-9ca3bbdb955a"></a>
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
