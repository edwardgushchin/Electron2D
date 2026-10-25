# VisualShaderNodeVarying

Last updated: 2026-10-25

Source: [VisualShaderNodeVarying.cs](../../src/Scene/Resources/VisualShaderNodeVarying.cs). Inherits [VisualShaderNode](VisualShaderNode.md). Inherited by [VisualShaderNodeVaryingGetter](VisualShaderNodeVaryingGetter.md) and [VisualShaderNodeVaryingSetter](VisualShaderNodeVaryingSetter.md).

```csharp
public abstract class VisualShaderNodeVarying : VisualShaderNode
```

## Description

Selects a graph-owned varying by exact name and numeric role. The graph borrows node definitions; a node neither owns nor creates its declaration. Authoring is cold and single-threaded. Changes notify observers after commitment and affect the next offline import; retained compiled programs stay executable. Duplication copies name, role and inherited numeric defaults. Portable resource archives retain declarations separately from their accesses and preserve shared node identities.

## Member summary

| Signature | Contract |
| --- | --- |
| `protected VisualShaderNodeVarying()` | Initializes an unselected floating access. |
| [`public string VaryingName { get; set; }`](#varyingname) | Exact declaration name; initially `[None]`. |
| [`public VisualShaderVaryingType VaryingType { get; set; }`](#varyingtype) | Numeric port role; initially Float. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores role before the inherited typed input defaults. |

## Property descriptions

<a id="varyingname"></a>
### VaryingName

Null throws `ArgumentNullException`. Equal assignments are quiet. `[None]` makes a getter return typed zero or numeric matrix identity, and makes a setter write nothing. Other names must resolve to a declaration with the same role at import. Source edits may temporarily retain dangling references; import rejects them before publishing a replacement artifact.

<a id="varyingtype"></a>
### VaryingType

Uses the shared [VisualShaderVaryingType](VisualShaderVaryingType.md), whose valid values differ from sampled-image graph ports. Undefined values and Max throw `ArgumentOutOfRangeException`. A changed setter role resets its input default to typed zero or matrix identity; an equal role preserves it. Name and role access after disposal throws `ObjectDisposedException`.

## Example

```csharp
using var graph = new VisualShader();
graph.AddVarying("amount", VisualShader.VaryingMode.VertexToFragmentLight, VisualShaderVaryingType.Float);
using var write = new VisualShaderNodeVaryingSetter { VaryingName = "amount" };
write.SetInputPortDefaultValue(0, .375f);
graph.AddNode(VisualShader.Type.Vertex, write, default, 2);
using var read = new VisualShaderNodeVaryingGetter { VaryingName = "amount" };
graph.AddNode(VisualShader.Type.Fragment, read, default, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
```

Save and import with the existing offline graph workflow. The setter has no output port and remains an executing root. See [typed varying transport](../components/visual-shaders.md#typed-varying-transport) for interpolation, native capacity, verification commands and limits.

## Authored canvas lights

See [canvas light graphs](../components/canvas-light-graphs.md) for the executing stage, typed inputs/outputs, fragment/vertex varying producers, shared native resource slots, custom provider activation, portable program validation and measured limits.
