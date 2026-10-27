# RenderingDevice.SamplerBorderColor

Last updated: 2026-10-27

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.SamplerBorderColor`. **Source:** [RenderingDevice.TextureEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.TextureEnums.cs). **Component:** [Native sampler borders](../components/sampler-borders.md).

## Description

Predefined colors returned by clamp-to-border sampling. The integer/floating-point role must match the sampled image. Colors own no resources. BorderColor is ignored when neither addressing axis selects ClampToBorder.

## Example

```csharp
using var state = new RDSamplerState
{
    RepeatU = RenderingDevice.SamplerRepeatMode.ClampToBorder,
    BorderColor = RenderingDevice.SamplerBorderColor.FloatOpaqueWhite
};
```

## API and member descriptions

| Member | Value | Color and sampled role |
| --- | --- | --- |
| FloatTransparentBlack | 0 | Floating-point (0, 0, 0, 0). |
| IntTransparentBlack | 1 | Integer (0, 0, 0, 0). |
| FloatOpaqueBlack | 2 | Floating-point (0, 0, 0, 1); configuration default. |
| IntOpaqueBlack | 3 | Integer (0, 0, 0, 1). |
| FloatOpaqueWhite | 4 | Floating-point (1, 1, 1, 1). |
| IntOpaqueWhite | 5 | Integer (1, 1, 1, 1). |
| Max | 6 | Nonselectable upper bound. |

## Lifetime and verification

See [RDSamplerState](RDSamplerState.md) for copied configuration and [native sampler borders](../components/sampler-borders.md) for the backend, lifetime and executable check boundaries.
