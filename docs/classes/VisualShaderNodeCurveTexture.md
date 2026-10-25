# VisualShaderNodeCurveTexture

Last updated: 2026-10-25

Source: [VisualShaderNodeCurveTexture.cs](../../src/Scene/Resources/VisualShaderNodeCurveTexture.cs). Inherits [VisualShaderNodeResizableBase](VisualShaderNodeResizableBase.md).

```csharp
public sealed class VisualShaderNodeCurveTexture : VisualShaderNodeResizableBase
```

Samples a borrowed [CurveTexture](CurveTexture.md) at `vec2(input)`, returning its red scalar. Input zero is scalar and initially stores `0f`. A connection overrides an authored default. Removing the default without connecting a producer returns typed zero; it does not sample the image at zero. A null image uses white. An assigned uninitialized image retains the source texture readiness contract; writable-texture drawing rejects until it has pixels, preserving the target. Initializing that same bound image makes it drawable without reimport. Samples preserve signed and HDR numeric values without color conversion. Sampling and native floating-point texture support follow the existing graph image contract.

## Members

| Signature | Contract |
| --- | --- |
| `public VisualShaderNodeCurveTexture()` | Creates scalar input zero, no image and inherited zero layout size. |
| `public CurveTexture? Texture { get; set; }` | Borrows a live image, initially null; rejects disposed resources and publishes Changed after each assignment, including equal values. |
| `protected override Resource CreateDuplicateInstance()` | Creates an independent definition for typed duplication. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Extends inherited layout/default storage with the typed image edge. |
| `protected override void CopyCustomStateTo(...)` | Preserves shallow references or deep resource graph aliases according to the inherited copy policy. |

The node never disposes its image or source curves. Inherited Size is source layout metadata. Shallow copies borrow the same image; deep copies retain internal aliases within a copied resource graph. Typed archives preserve the image, curves, port default presence and shared definitions across stages.

Graph/source changes require offline reimport. An already bound curve image may rebake on source curve changes and refresh its ordinary texture pixels without rebuilding the shader or retained drawing commands. See [curve graph sampling](../components/visual-shaders.md#curve-image-lookups) for the executing contract and verification limits.
