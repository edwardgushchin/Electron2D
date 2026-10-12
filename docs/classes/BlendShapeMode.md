# BlendShapeMode

Last updated: 2026-10-12

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.BlendShapeMode` · **Source:** [Mesh.Morphs.cs](../../src/Scene/Resources/Mesh.Morphs.cs).

## Description

One shared value contract for named mesh target mixing. Normalized uses one minus the sum of effective target weights for the base contribution; Relative keeps the base contribution at one. Targets store absolute positions or additive offsets accordingly. Weights at absolute values at most 0.0001 do not contribute. Mixing precedes skinning and preserves base numerical normal/tangent data; raw target frames are available through SurfaceTool pose extraction.

## Example

```csharp
mesh.BlendShapeMode = BlendShapeMode.Normalized;
```


## Complete API

| Complete signature | Contract |
| --- | --- |
| `public const Electron2D.BlendShapeMode Normalized = 0` | Base contribution is one minus the sum of effective target weights. |
| `public const Electron2D.BlendShapeMode Relative = 1` | Base contribution remains one and targets are additive offsets. |

## Member descriptions

### Normalized

`public const Electron2D.BlendShapeMode Normalized = 0`

summary: Base contribution is one minus the sum of effective target weights.

### Relative

`public const Electron2D.BlendShapeMode Relative = 1`

summary: Base contribution remains one and targets are additive offsets.
