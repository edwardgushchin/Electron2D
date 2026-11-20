# GlobalRandom

- **Source:** [GlobalRandom.cs](../../src/Core/Math/GlobalRandom.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public static class GlobalRandom`

## Description

A process-wide PCG32 stream for games and procedural generation. It starts with a time-dependent seed. Set the seed before drawing and repeat the same operations in the same order to replay a sequence.

```csharp
GlobalRandom.Seed(12345);
int room = GlobalRandom.RandiRange(1, 6);
double offset = GlobalRandom.Randfn(0, 0.25);
var (value, seed) = GlobalRandom.RandFromSeed(42);
```

| Member | Contract |
| --- | --- |
| `Seed(ulong seed)` | Restarts the shared stream using all 64 seed bits. |
| `Randomize()` | Restarts the shared stream using wall and monotonic clocks and the prior state. |
| `RandFromSeed(ulong seed)` | Returns the first unsigned draw and unchanged input seed as `(uint Value, ulong Seed)`. The call leaves the shared position intact. |
| `Randi()` | Draws a `uint` using one PCG32 word. |
| `RandiRange(int from, int to)` | Draws an unbiased `int` between the inclusive bounds in either order. Equal bounds return immediately without drawing. The full signed range is supported. |
| `Randf()` | Scales one PCG32 word into a `float` from zero through one, inclusive. |
| `RandfRange(double from, double to)` | Computes `sample * (to - from) + from` in double precision. Equal bounds still consume a sample. Rounding can place the result outside the bounds even when both bounds and their difference are finite. Nonfinite inputs and overflow follow ordinary floating-point arithmetic. |
| `Randfn(double mean = 0, double deviation = 1)` | Uses two double-precision unit samples and the Box-Muller transform. A first sample below `1e-5` receives that offset before the logarithm. |

Each shared operation is atomic. Concurrent calls are serialized in lock acquisition order, so callers must coordinate their order for replay. Normal samples depend on the runtime's transcendental math. Use [RandomNumberGenerator](RandomNumberGenerator.md) for an independent stream with checkpoints, or a cryptographic generator for secrets.

See [random generation](../components/random-generation.md) for verification.
