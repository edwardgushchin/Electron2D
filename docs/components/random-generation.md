# Random generation component

Last updated: 2026-09-23

## Scope and owned type

Core provides [RandomNumberGenerator](../classes/RandomNumberGenerator.md), one independent PCG32 stream for games and procedural systems. It inherits managed identity and disposal from ElectronObject. It does not replace the .NET cryptographic generator used by encryption, temporary names and resource IDs.

The PCG32 core is adapted from M.E. O'Neill's Apache-2.0 implementation ([notice and license](../../licence/PCG32-LICENSE.txt)). Sampling behavior follows the pinned upstream implementation covered by the existing [MIT notice](../../licence/ReferenceData-LICENSE.txt).

## Runtime flow and invariants

Construction initializes a time-dependent seed. `Seed` restarts the stream; `State` snapshots or restores its exact position. Raw integer draws advance PCG32 once. Bounded integers use rejection to avoid modulo bias; floating draws use exponent/significand sampling; normal draws use Box-Muller; weighted draws select an index by subtracting nonnegative weights. A lock makes each instance's draws and state changes atomic across threads. Instances share no mutable stream state.

The algorithm has no native backend, renderer, scene or asset dependency. Time-dependent initialization reads the .NET wall and monotonic clocks. The generator is deterministic for a given seed and runtime numeric behavior, but is not suitable for cryptographic security. Full signed integer ranges, unsigned output, reversed bounds, empty/invalid weights and disposal have explicit contracts in the class page.

## Current implementation and verification

The complete pinned class and member surface is implemented. [RandomNumberGeneratorTests](../../tests/Electron2D.Tests/RandomNumberGeneratorTests.cs) compares fixed PCG32 vectors to the pinned native algorithm and exercises state replay, sampling boundaries, weights, descriptors and lifetime. Tests ran on Linux/.NET 8. Native platform hosts, other .NET targets and statistical distribution acceptance remain unverified. [Coverage](../coverage/classes/RandomNumberGenerator.md) records all ten upstream declarations and the two C# lifecycle/property projections.
