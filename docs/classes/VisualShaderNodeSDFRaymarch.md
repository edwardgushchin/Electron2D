# VisualShaderNodeSDFRaymarch

Last updated: 2026-10-22

Source: [VisualShaderNodeSDFRaymarch.cs](../../src/Scene/Resources/VisualShaderNodeSDFRaymarch.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeSDFRaymarch
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Casts a segment through the canvas signed-distance field.

Vector inputs are from/to framebuffer pixel positions, absent as zero. Outputs are signed travelled distance, hit and endpoint. Each step adds the sampled distance before testing a 0.01-pixel hit threshold; the result is capped at segment length. Starting inside may return a negative distance and a backwards endpoint. A zero-length segment returns zero, false and its start. Uses the renderer's pixel-space field, including its viewport oversize/resolution and occluder policies. Only reachable fragment-stage nodes import; definition edits require reimport.

## Example

This snippet assumes an initialized project resource root.

```csharp
using Electron2D;
using var node = new VisualShaderNodeSDFRaymarch();
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
| [`public sealed class Electron2D.VisualShaderNodeSDFRaymarch`](#member-a567895dba1b) | Casts a segment through the canvas signed-distance field. |
| [`public VisualShaderNodeSDFRaymarch()`](#member-7017d8f1b1a6) | Creates a definition with no authored input defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-a0417cd99cd3) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-a567895dba1b"></a>
### `VisualShaderNodeSDFRaymarch`

```csharp
public sealed class Electron2D.VisualShaderNodeSDFRaymarch
```

Casts a segment through the canvas signed-distance field.

Vector inputs are from/to framebuffer pixel positions, absent as zero. Outputs are signed travelled distance, hit and endpoint. Each step adds the sampled distance before testing a 0.01-pixel hit threshold; the result is capped at segment length. Starting inside may return a negative distance and a backwards endpoint. A zero-length segment returns zero, false and its start. Uses the renderer's pixel-space field, including its viewport oversize/resolution and occluder policies. Only reachable fragment-stage nodes import; definition edits require reimport.

<a id="member-7017d8f1b1a6"></a>
### `.ctor`

```csharp
public VisualShaderNodeSDFRaymarch()
```

Creates a definition with no authored input defaults.

<a id="member-a0417cd99cd3"></a>
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
