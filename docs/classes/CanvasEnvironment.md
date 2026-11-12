# CanvasEnvironment

`CanvasEnvironment : Resource` stores the canvas background and GPU glow, tone mapping and color adjustment settings. Assign it to a [WorldEnvironment](WorldEnvironment.md). Glow starts disabled. The resource remains valid across renderer sessions and can be shared or stored with a scene.

- `GlowEnabled` enables the effect. `GlowHDRThreshold`, `GlowHDRScale`, `GlowHDRLuminanceCap` and `GlowBloom` control extraction in linear color.
- `GlowStrength` controls the blur. `SetGlowLevel` and `GetGlowLevel` use zero-based indices 0 through 6. Default weights are 0, 0.8, 0.4, 0.1, 0, 0, 0 from narrow to broad. `GlowNormalized` normalizes their contribution.
- `GlowBlend` selects Additive, Screen, SoftLight, Replace or Mix. Screen is the default. `GlowIntensity` scales the glow. Mix uses `GlowMix`.
- `GlowMap` borrows a filterable texture and `GlowMapStrength` controls its RGB modulation. A null map leaves the glow unmodulated.
- `GetRID()` returns a borrowed resource identity usable with the rendering server glow, tone mapping and color adjustment methods. Disposing the resource retires the identity.

Scalar glow settings accept finite, nonnegative values through 65,504. Mix and map strength accept 0 through 1.

## Background

`BackgroundMode` selects `ClearColor`, `Color` or `Keep`. `BackgroundColor` defaults to black and `BackgroundEnergyMultiplier` defaults to one. See [canvas environment backgrounds](../components/canvas-environment-background.md) for clear policy, retention and color transfer.

## Tone mapping

`TonemapMode` selects a [tone operator](CanvasEnvironment.ToneMapper.md), with Linear as the default. `TonemapExposure` is a nonnegative linear multiplier and defaults to one. `TonemapWhite` defaults to one. AgX uses `TonemapAGXWhite` and `TonemapAGXContrast`, which default to 16.29 and 1.25. White points and AgX contrast must be finite and positive. See [canvas tone mapping](../components/canvas-tonemapping.md).

## Color adjustment

`AdjustmentEnabled` starts false. `AdjustmentBrightness`, `AdjustmentContrast` and `AdjustmentSaturation` start at one. `AdjustmentColorCorrection` optionally borrows a filterable color ramp. The effect applies after glow and tone mapping on GPU RGBA8 and half-float viewports. See [canvas color adjustment](../components/canvas-color-adjustment.md).

The [glow component](../components/canvas-glow.md) describes the native output, world selection and current backend limits.
