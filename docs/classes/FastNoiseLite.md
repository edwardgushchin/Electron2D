# FastNoiseLite

Last updated: 2026-09-23

**Inherits:** [Noise](Noise.md) → [Resource](Resource.md) → [ElectronObject](ElectronObject.md)

- **Source:** [FastNoiseLite.cs](../../src/Core/IO/FastNoiseLite.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class FastNoiseLite : Noise`

## Description

`FastNoiseLite` is a concrete CPU noise generator for one- and two-dimensional sampling. It uses a pinned, internally compiled C# FastNoiseLite 1.1.0 implementation. No backend type enters the public API. Its samples feed inherited `GetImage`/`GetSeamlessImage` and a borrowed [NoiseTexture](NoiseTexture.md) directly. Each instance owns an independent base generator and domain-warp generator.

## Constructor and modes

`public FastNoiseLite()` configures the defaults below. Six nested enums select the algorithms: [NoiseTypeEnum](FastNoiseLite.NoiseTypeEnum.md), [FractalTypeEnum](FastNoiseLite.FractalTypeEnum.md), [CellularDistanceFunctionEnum](FastNoiseLite.CellularDistanceFunctionEnum.md), [CellularReturnTypeEnum](FastNoiseLite.CellularReturnTypeEnum.md), [DomainWarpTypeEnum](FastNoiseLite.DomainWarpTypeEnum.md), and [DomainWarpFractalTypeEnum](FastNoiseLite.DomainWarpFractalTypeEnum.md). Their numeric values match the pinned 2D source contract. `Enum` suffixes avoid C# name collisions with properties.

## Properties

| Property | Default | Effect |
| --- | --- | --- |
| `NoiseType` | `SimplexSmooth` | Base algorithm. |
| `Seed` | `0` | Seeds both base and warp generators. |
| `Frequency` | `0.01` | Base sampling frequency. |
| `Offset` | `Vector2.Zero` | Shifts the X/Y coordinates before sampling. |
| `FractalType` | `FBM` | Base fractal mode. |
| `FractalOctaves` | `5` | Base octave count; 1–1024 accepted. |
| `FractalLacunarity` | `2` | Base octave frequency multiplier. |
| `FractalGain` | `0.5` | Base octave amplitude multiplier. |
| `FractalWeightedStrength` | `0` | Weights subsequent base octaves. |
| `FractalPingPongStrength` | `2` | Shapes ping-pong fractals. |
| `CellularDistanceFunction` | `Euclidean` | Cellular distance metric. |
| `CellularReturnType` | `Distance` | Cellular output mode. |
| `CellularJitter` | `1` | Cellular point displacement. |
| `DomainWarpEnabled` | `false` | Applies coordinate warping before base sampling. |
| `DomainWarpType` | `Simplex` | Warp algorithm. |
| `DomainWarpAmplitude` | `30` | Warp displacement amplitude. |
| `DomainWarpFrequency` | `0.05` | Warp coordinate frequency. |
| `DomainWarpFractalType` | `Progressive` | Warp fractal mode. |
| `DomainWarpFractalOctaves` | `5` | Warp octave count; 1–1024 accepted. |
| `DomainWarpFractalLacunarity` | `6` | Warp octave frequency multiplier. |
| `DomainWarpFractalGain` | `0.5` | Warp octave amplitude multiplier. |

Every assignment except an unchanged `DomainWarpEnabled` emits synchronous `Changed` after applying the value. `NoiseType`, `FractalType`, and a changed `DomainWarpEnabled` also emit `PropertyListChanged`. Nonfinite numeric settings, undefined enum values and out-of-range octaves throw before mutation. The X/Y offset is a deliberate 2D adaptation of the source's three-component offset; Z has no sampling role in this engine.

## Sampling and ownership

`GetNoise2D(x, y)` applies `Offset`, optionally warps coordinates, then samples the base generator. `GetNoise1D(x)` follows the source's two-dimensional delegation, including its X-offset and optional warp before invoking `GetNoise2D(x, 0)`; consequently the X offset may be applied twice on that path. Inputs must be finite, and use after disposal throws. Derived image generation rejects nonfinite sample results. Resource duplication and `CopyFromResource` copy all settings into independent generators; a `NoiseTexture` borrows and observes this resource without owning it. Coordinate mutations, copying and sampling on different threads require caller coordination.

## Verification and limits

[FastNoiseLiteTests](../../tests/Electron2D.Tests/FastNoiseLiteTests.cs) checks defaults, all six base algorithm, four fractal, cellular, and nine warp-mode vectors against the pinned C++ implementation; change events, one-dimensional offset delegation, independent copying, disposal and texture invalidation. These are managed Linux checks. Existing NoiseTexture retained-pixel checks use a custom sampler on Wayland GPU and compatibility backends; this concrete generator also passes retained Sprite pixel comparisons on Linux Wayland compatibility and GPU backends before and after a seed change. Other platforms remain unverified. Editor inspector filtering is a future editor concern. [Coverage](../coverage/classes/FastNoiseLite.md) and [ADR 0013](../decisions/resources.md#adr-0013) record the scope.
