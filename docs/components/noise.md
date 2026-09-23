# Noise component

Last updated: 2026-09-23

## Scope and owned types

The component provides an executable abstract [Noise](../classes/Noise.md) resource for 1D/2D sampling, a concrete [FastNoiseLite](../classes/FastNoiseLite.md) generator, and a generated [NoiseTexture](../classes/NoiseTexture.md) consumer. [FastNoiseLite coverage](../coverage/classes/FastNoiseLite.md) records the complete 2D mapping.

## Runtime flow

1. `FastNoiseLite` or an application `Noise` subclass returns scalar values from `GetNoise1D` and `GetNoise2D`.
2. `GetNoise2DV` forwards a `Vector2` into the 2D sampler.
3. `GetImage` samples a rectangular integer grid, normalizes its range or maps expected `[-1, 1]` values, then creates an independent L8 `Image`.
4. `GetSeamlessImage` asks the virtual `GetImage` path for an enlarged grid, rearranges its quadrants and blends overlap strips, then disposes the temporary image.
5. NoiseTexture borrows Noise and an optional Gradient. A changed setting or source invalidates the cached image and notifies consumers. The next read samples Noise, applies gradient color, optional normal conversion and mipmaps, and returns a copied texture image.

## Dependencies and invariants

The base sampler uses managed `Resource`, `Vector2` and `Image`. FastNoiseLite compiles a pinned internal C# algorithm into Electron2D.dll; its public surface contains only Electron2D types. NoiseTexture uses the existing Texture snapshot/rendering path and managed Gradient/Image operations; it adds no backend dependency. Output images are caller-owned resources. Invalid dimensions or skirt values fail before sampling; nonfinite samples fail before publication. Derived state must coordinate concurrent mutations with image sampling. A derived type that supports Resource duplication supplies explicit copying hooks under ADR 0013. NoiseTexture borrows both sources, retries concurrent invalidation, and rejects recursive generation.

## Current limits and verification

Three-dimensional sampling and image volumes are outside the product boundary. FastNoiseLite supports all six base algorithms, fractal and cellular settings, XY offset and independent domain warp. Its base and warp settings are copied into independent resources. The default seamless path supports L8 and RGBA8 sources; exact pixel parity for unusual skirt values and derived formats remains partial. NoiseTexture's lazy notification timing differs from the reference's worker schedule. [NoiseTests](../../tests/Electron2D.Tests/NoiseTests.cs), [FastNoiseLiteTests](../../tests/Electron2D.Tests/FastNoiseLiteTests.cs) and [NoiseTextureTests](../../tests/Electron2D.Tests/NoiseTextureTests.cs) exercise the managed paths. [NoiseTextureRenderingTests](../../tests/Electron2D.Tests/NoiseTextureRenderingTests.cs) verifies retained Sprite pixels and source updates on Linux Wayland GPU and compatibility backends; other platforms remain unverified. [Noise coverage](../coverage/classes/Noise.md) and [NoiseTexture coverage](../coverage/classes/NoiseTexture2D.md) list each reference member and its status.
