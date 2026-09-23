# RandomNumberGenerator

Last updated: 2026-09-23

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [RandomNumberGenerator.cs](../../src/Core/Math/RandomNumberGenerator.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class RandomNumberGenerator : ElectronObject`

## Description

An independent, stateful PCG32 stream for gameplay and procedural calculations. The constructor chooses a time-dependent seed. Set `Seed` to reproduce a sequence; save and restore `State` after setting `Seed` to resume at a particular draw. Calls on one instance are serialized and fail after disposal. This generator is not cryptographic; file encryption, temporary names and resource IDs continue to use `System.Security.Cryptography.RandomNumberGenerator`.

```csharp
using var rng = new Electron2D.RandomNumberGenerator { Seed = 12345 };
uint value = rng.Randi();
ulong checkpoint = rng.State;
int chosen = rng.RandWeighted([1f, 3f]);
rng.State = checkpoint; // Repeats the weighted draw.
```

## Constructor

| Member | Contract |
| --- | --- |
| [`public RandomNumberGenerator()`](#constructor-description) | Initializes PCG32 and chooses a time-dependent seed. |

## Properties

| Member | Contract |
| --- | --- |
| [`public ulong Seed { get; set; }`](#seed-description) | Gets the initialization seed; assignment restarts the stream. |
| [`public ulong State { get; set; }`](#state-description) | Gets or restores the current PCG32 state. |

## Methods

| Member | Contract |
| --- | --- |
| [`public uint Randi()`](#randi-description) | Draws one unsigned 32-bit integer. |
| [`public int RandiRange(int from, int to)`](#randirange-description) | Draws uniformly from inclusive signed bounds. |
| [`public float Randf()`](#randf-description) | Draws a float from 0 through 1 inclusive. |
| [`public float RandfRange(float from, float to)`](#randfrange-description) | Interpolates between the supplied float bounds. |
| [`public float Randfn(float mean = 0, float deviation = 1)`](#randfn-description) | Draws a normally distributed float. |
| [`public int RandWeighted(float[] weights)`](#randweighted-description) | Draws an index proportional to nonnegative weights. |
| [`public void Randomize()`](#randomize-description) | Replaces the seed using the wall and monotonic clocks. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#property-descriptors-description) | Publishes typed, stored `Seed` and `State` descriptors. |

## Constructor description

<a id="constructor-description"></a>

The constructor initializes the fixed PCG stream parameters and then selects a time-dependent seed. The documented upstream zero default is a placeholder; neither initial `Seed` nor initial `State` is fixed.

## Property descriptions

<a id="seed-description"></a>

`Seed` holds all 64 seed bits. Setting it reruns PCG initialization, changes `State`, and makes subsequent draws reproducible. Similar seeds can yield related sequences; callers may hash externally chosen seeds when that matters.

<a id="state-description"></a>

`State` holds the current 64-bit stream state. Assigning it leaves `Seed` unchanged. Restore a value previously obtained from a generator configured with the same seed; arbitrary values may weaken stream quality. Both properties are available through typed stored property descriptors. No random draw is made by either getter.

## Method descriptions

<a id="randi-description"></a>

`Randi` returns one raw unsigned 32-bit value in `[0, 4294967295]` and advances the stream once.

<a id="randirange-description"></a>

`RandiRange` accepts either bound order and includes both endpoints. It uses PCG rejection sampling to avoid modulo bias, handles the entire signed 32-bit range, and does not draw when the bounds are equal.

<a id="randf-description"></a>

`Randf` uses PCG bits to generate a float in `[0, 1]`. It normally advances the stream twice; the rare zero-exponent branch advances once. Rounding may produce the inclusive upper endpoint.

<a id="randfrange-description"></a>

`RandfRange` computes `Randf() * (to - from) + from`. Reversed bounds work; nonfinite values and overflow follow ordinary single-precision arithmetic.

<a id="randfn-description"></a>

`Randfn` uses two unit samples and the Box-Muller transform. A unit sample below `1e-5` receives that epsilon before the logarithm. Default mean is zero and deviation is one. Nonfinite inputs retain ordinary floating-point results.

<a id="randweighted-description"></a>

`RandWeighted` accepts a non-null `float[]`; callers must keep its contents stable during the call. An empty array or any negative weight returns `-1` without drawing. Otherwise it draws one unit sample, subtracts weights in index order and returns the first interval containing the sample. A rounding fallthrough returns the last positive index. All-zero weights return `-1` after the draw. A null array throws `ArgumentNullException`.

<a id="randomize-description"></a>

`Randomize` combines the current Unix seconds, monotonic microseconds, prior stream state and the PCG increment, then assigns the resulting seed. It is useful for nonreproducible gameplay runs, not for secrets or cryptographic tokens.

<a id="property-descriptors-description"></a>

The inherited property list exposes `Seed` and `State` as typed stored `ulong` properties. Their zero metadata defaults are placeholders for the actual time-dependent initial values.

## Verification and limits

[RandomNumberGeneratorTests](../../tests/Electron2D.Tests/RandomNumberGeneratorTests.cs) checks pinned PCG32 outputs, save/restore, equal and full-range integer bounds, reversed float bounds, weight behavior, normal finite samples, typed descriptors and disposal. This is managed Linux/.NET verification; other platforms and statistical quality beyond the underlying PCG32 contract are unverified.
