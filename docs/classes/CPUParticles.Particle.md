# CPUParticles.Particle

Last updated: 2026-10-07

- Declaration: `private struct CPUParticles.Particle`
- Source: [CPUParticles.Simulation.cs](../../src/Scene/2D/CPUParticles.Simulation.cs)
- Owner/component: [CPUParticles](CPUParticles.md), [CPU particles](../components/cpu-particles.md)

Transient value record of pose, velocity, color factors, stable random samples, seed, animation, age, randomized lifetime and active membership. The node owns two capacity-matched arrays. Each substep writes complete scratch records and swaps arrays only after all finite-state checks, preserving the previously published substep on failure. No delegates, native handles or resource ownership live here. Runtime state is omitted from scene storage and copying. Prepared loops copy values without allocation; tests observe actual poses/colors through test-only internal queries.
