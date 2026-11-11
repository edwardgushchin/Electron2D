# CanvasEnvironment

`CanvasEnvironment : Resource` stores HDR canvas glow settings. Assign it to a [WorldEnvironment](WorldEnvironment.md). Glow starts disabled. The resource remains valid across renderer sessions and can be shared or stored with a scene.

- `GlowEnabled` enables the effect. `GlowHDRThreshold`, `GlowHDRScale`, `GlowHDRLuminanceCap` and `GlowBloom` control extraction in linear color.
- `GlowStrength` controls the blur. `SetGlowLevel` and `GetGlowLevel` use zero-based indices 0 through 6. Default weights are 0, 0.8, 0.4, 0.1, 0, 0, 0 from narrow to broad. `GlowNormalized` normalizes their contribution.
- `GlowBlend` selects Additive, Screen, SoftLight, Replace or Mix. Screen is the default. `GlowIntensity` scales the glow. Mix uses `GlowMix`.
- `GlowMap` borrows a filterable texture and `GlowMapStrength` controls its RGB modulation. A null map leaves the glow unmodulated.
- `GetRID()` returns a borrowed resource identity usable with `RenderingServer.EnvironmentSetGlow`. Disposing the resource retires the identity.

Scalar glow settings accept finite, nonnegative values through 65,504. Mix and map strength accept 0 through 1.

The [glow component](../components/canvas-glow.md) describes the native output, world selection and current backend limits.
