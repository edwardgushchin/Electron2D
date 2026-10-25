# VisualShaderNodeGlobalExpression

Last updated: 2026-10-25

Source: [VisualShaderNodeGlobalExpression.cs](../../src/Scene/Resources/VisualShaderNodeGlobalExpression.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeGlobalExpression
```

Inherits: [VisualShaderNodeExpression](VisualShaderNodeExpression.md).

## Description

Contributes shared GLSL declarations to both imported canvas stages.

Every global definition in either canvas graph participates once per graph entry, ordered by stage and node ID, even without output connections. Use this source for functions, constants or declarations that satisfy the existing shader ABI. Compiler diagnostics preserve native GLSL stage restrictions. ELECTRON2D_SHADER_STAGE is zero for Vertex and one for Fragment during preprocessing. Local statements and graph port values belong to VisualShaderNodeExpression instead.

## Example

This snippet assumes an initialized project resource root.

```csharp
using Electron2D;
using var node = new VisualShaderNodeGlobalExpression();
node.Expression = "vec3 shade(float amount) { return vec3(amount); }";
using var graph = new VisualShader();
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
ResourceSaver.Save(graph, "res://Shaders/expression-source.e2dres", SaverFlags.BundleResources);
```

Import through [the graph workflow](../components/visual-shaders.md#author-import-and-execute), then load the compiled resource and assign it to ShaderMaterial.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShaderNodeGlobalExpression`](#member-a63decbacc7f) | Contributes shared GLSL declarations to both imported canvas stages. |
| [`public VisualShaderNodeGlobalExpression()`](#member-afa59221d7f7) | Creates an empty global source definition. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-5e9608902786) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-a63decbacc7f"></a>
### `VisualShaderNodeGlobalExpression`

```csharp
public sealed class Electron2D.VisualShaderNodeGlobalExpression
```

Contributes shared GLSL declarations to both imported canvas stages.

Every global definition in either canvas graph participates once per graph entry, ordered by stage and node ID, even without output connections. Use this source for functions, constants or declarations that satisfy the existing shader ABI. Compiler diagnostics preserve native GLSL stage restrictions. ELECTRON2D_SHADER_STAGE is zero for Vertex and one for Fragment during preprocessing. Local statements and graph port values belong to VisualShaderNodeExpression instead.

<a id="member-afa59221d7f7"></a>
### `.ctor`

```csharp
public VisualShaderNodeGlobalExpression()
```

Creates an empty global source definition.

<a id="member-5e9608902786"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer exercises typed defaults, copies, factories, typed source/port limits, source/compiled archives and executable GLSL procedures. See [the operation contract](../components/visual-shaders.md#glsl-expression-blocks) for ports, pixel units, port mutation, scope, control flow and source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)

[Canvas command metadata](../components/canvas-command-metadata.md) supplies read-only fragment regions and initial-UV specular values through the same retained program. See its geometry conventions, ownership, typed sampler boundary and verification limits.

## Canvas points

[Canvas point programs](../components/canvas-points.md) document vertex point-size output, stage-specific typed inputs, expression/global/custom source roles, actual point topology and backend capability limits.
