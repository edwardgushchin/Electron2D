# Noise component

Last updated: 2026-09-23

## Scope and owned types

The component provides an executable abstract [Noise](../classes/Noise.md) resource for 1D/2D sampling and managed 2D image generation. It does not yet ship a concrete generator. [FastNoiseLite coverage](../coverage/classes/FastNoiseLite.md) tracks that next dependency.

## Runtime flow

1. A concrete `Noise` subclass returns scalar values from `GetNoise1D` and `GetNoise2D`.
2. `GetNoise2DV` forwards a `Vector2` into the 2D sampler.
3. `GetImage` samples a rectangular integer grid, normalizes its range or maps expected `[-1, 1]` values, then creates an independent L8 `Image`.
4. `GetSeamlessImage` asks the virtual `GetImage` path for an enlarged grid, rearranges its quadrants and blends overlap strips, then disposes the temporary image.

## Dependencies and invariants

The component uses only managed `Resource`, `Vector2` and `Image`. No SDL, GPU, editor or asset importer is involved. Output images are caller-owned resources. Invalid dimensions or skirt values fail before sampling; nonfinite samples fail before publication. Derived state must coordinate concurrent mutations with image sampling. A derived type that supports Resource duplication supplies explicit copying hooks under ADR 0013.

## Current limits and verification

Three-dimensional sampling and image volumes are outside the product boundary. No built-in FastNoiseLite implementation, noise texture, or native pixel test exists yet. The default seamless path supports L8 and RGBA8 sources; exact pixel parity for unusual skirt values and derived formats remains partial. [NoiseTests](../../tests/Electron2D.Tests/NoiseTests.cs) exercises the managed 2D path, positive and failure cases. [The coverage page](../coverage/classes/Noise.md) lists every reference member and its status.
