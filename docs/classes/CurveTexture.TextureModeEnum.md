# CurveTexture.TextureModeEnum

Last updated: 2026-09-23

- Declaration: `public enum CurveTexture.TextureModeEnum`
- Source: [CurveTexture.cs](../../src/Scene/Resources/CurveTexture.cs)
- Component: [Curves](../components/curves.md)

Selects stored channels through [CurveTexture.TextureMode](CurveTexture.md#texturemode). The suffix avoids a C# collision with the property name.

| Value | Integer | Contract |
| --- | --- | --- |
| `RGB` | 0 | Repeats each sample into RGB float channels; default. |
| `Red` | 1 | Stores one red float channel; sampled green/blue are zero. |

## RGB

Image.Format.Rgbf, 12 original bytes per texel. Alpha is implicitly one.

## Red

Image.Format.Rf, 4 original bytes per texel. Original CPU storage is smaller; both modes currently upload as RGBA32Float, so GPU allocation size is unchanged. Alpha is one. No gamma conversion or clamp is performed.

Example assignment: `texture.TextureMode = CurveTexture.TextureModeEnum.Red;`. Undefined values throw ArgumentOutOfRangeException before changing the resource. Actual changes rebuild and emit Changed; equal assignments are silent. [CurveTextureTests](../../tests/Electron2D.Tests/CurveTextureTests.cs) and native shader readback verify both modes. See [ADR 0013](../decisions/resources.md#adr-0013).
