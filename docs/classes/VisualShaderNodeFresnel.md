# VisualShaderNodeFresnel

Last updated: 2026-10-25

Source: [VisualShaderNodeFresnel.cs](../../src/Scene/Resources/VisualShaderNodeFresnel.cs). Inherits [VisualShaderNode](VisualShaderNode.md).

```csharp
public sealed class VisualShaderNodeFresnel : VisualShaderNode
```

Computes a scalar normal/view falloff. Its four inputs are Vector3 normal, Vector3 view, Boolean invert and scalar power. Output zero is scalar. The formula clamps the dot product to [0,1], selects that value when inverted or one minus it otherwise, then raises the selected base to the supplied power. It does not normalize either vector or clamp the result; negative powers can exceed one. Native shader power-domain/precision rules apply.

## Members

| Signature | Contract |
| --- | --- |
| `public VisualShaderNodeFresnel()` | Leaves normal/view absent; stores false inversion and power one. |
| `protected override Resource CreateDuplicateInstance()` | Constructs an independent definition for inherited typed duplication. |

Inherited input defaults, graph connections, duplication and portable resource archives retain typed values and explicit absence. Authored defaults or connections override implicit inputs. An absent view is zero. An absent normal uses the current normal image in a canvas fragment, and zero in particle/writable-texture stages. A canvas vertex requires an explicitly authored or connected normal; unsupported implicit vertex use rejects import without changing its previous artifact.

The canvas normal is captured before graph execution. It decodes RG as `(2R-1, 1-2G)`, applies the command rectangle's transpose/flips and reconstructs nonnegative Z with `sqrt(max(0,1-X*X-Y*Y))`. An absent normal channel uses the existing encoded flat-normal fallback. Mesh UV mirroring follows the attribute path and does not introduce rectangle flip flags. The graph observes this local normal before later item/canvas lighting transforms. These flags belong to the renderer, not material parameters.

## Example

```csharp
using var fresnel = new VisualShaderNodeFresnel();
fresnel.SetInputPortDefaultValue(1, new Vector3(0, 0, 1));
fresnel.SetInputPortDefaultValue(3, 2f);
graph.AddNode(VisualShader.Type.Fragment, fresnel, default, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
```

Save and import the graph with the existing offline shader tool. Graph edits do not compile or change a loaded immutable program. All runtime shader execution requires the GPU backend; compatibility retains its explicit shader refusal.

## Verification

The public [CPU](../../tests/VisualShader.Consumer/Fresnel.CPU.cs), [authoring](../../tests/VisualShader.Consumer/Fresnel.Author.cs) and [native consumer](../../tests/VisualShader.Consumer/Fresnel.Render.cs) exercise types/defaults/absence, invalid mutations, shared portable aliases, eight executing stages, dot clamping, inversion, nonunit vectors, zero/negative/fractional powers, live parameters/reset and full RGBAF values. Actual canvas pixels include normal textures, encoded fallback, source/destination flips, transpose, mesh transitions and normal-channel replacement. Warmed active and idle intervals retain drawing commands and measure managed allocation separately. See [the connected contract](../components/visual-shaders.md#fresnel-and-implicit-canvas-normals) for receipts and remaining native/platform limits.
