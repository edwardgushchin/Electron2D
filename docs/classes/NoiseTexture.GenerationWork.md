# NoiseTexture.GenerationWork

Last updated: 2026-10-11

Internal implementation. **Source:** [NoiseTexture.Generation.cs](../../src/Scene/Resources/NoiseTexture.Generation.cs).

Owns captured generation settings, copied built-in noise/gradient inputs, a borrowed custom sampler, optional file-graph lease, task identity, prepared pixels and captured failure. Computation runs on workers after the first owner-deferred generation. Owner publication, stale results, errors and teardown release snapshots/lease and retire the task once. See [Noise](../components/noise.md).
