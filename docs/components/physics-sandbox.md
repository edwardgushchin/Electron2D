# Water playground

Last updated: 2026-10-09

[PhysicsSandbox](../../examples/PhysicsSandbox/README.md) now contains one full-window, resizable water scene. The former twelve-story selector, inspectors and scene code were removed. [WaterWindow](../classes/WaterWindow.md) owns input and presentation; [WaterSimulation](../classes/WaterSimulation.md) owns the application-side liquid and its coupling to rigid toys. The example consumes only public Electron2D API.

## Physical model

The initial 1152×800 container holds 65,536 equal-mass particles. One hundred scene units represent a metre and the modeled slice is 0.1 m deep, giving 3.072 m³ at a bulk density of 1,000 kg/m³. Positions, velocities and mass are actual simulation state. The renderer follows those positions; there is no scripted waterline or floating trajectory.

The liquid uses an iterative density-constraint method based on [Position Based Fluids](https://mmacklin.com/pbf_sig_preprint.pdf). Each interval predicts motion under gravity, finds local neighbors, estimates density with a normalized two-dimensional poly6 kernel, solves nonnegative compression using a spiky gradient, projects contacts, reconstructs velocity and applies XSPH viscosity. The numerical profile uses six density iterations, eight coupling intervals per 1/60 s tick, 0.25 Jacobi relaxation and 0.1 viscosity. Mirrored wall neighbors supply density support near the container. This finite-iteration real-time model approximates incompressibility and dissipates energy; it is not an exact CFD solution and does not implement air, surface tension or vorticity confinement.

The retained dense neighbor grid expands its upper extent when splashes leave the prepared range. It does not leave escaped particles permanently concentrated in a clamped boundary cell. Particle count remains constant. CPU and GPU implement the same passes and parameters. GPU storage stays resident throughout projection iterations; one state read publishes all 65,536 positions/velocities per outer tick. Small reductions return reaction impulses at each coupling interval. CPU mode uses retained arrays with parallel particle passes; the modes are numerically comparable, not bitwise identical.

The duck and boat are public PhysicsServer bodies with capsule/circle and convex-hull geometry. Gravity, rigid-body contacts, mass and inertia use the existing CPU space in both modes. Fluid contacts exchange linear/angular impulses with these bodies before advancing that space. Thus GPU mode identifies the liquid solver; it does not claim that the still-incomplete independent rigid-body GPU backend is exposed. The separate [GPU Smash developer preview](gpu-smash-preview.md) remains test-only and independent of this example.

## Interaction, rendering and lifetime

Water begins above the window. The duck is inserted at 3.5 simulation seconds, the boat at 6 seconds. Their solved transforms determine rendering. Mouse dragging applies a bounded spring impulse at the picked toy anchor; the sail and beak are included in visual picking. Empty-water dragging applies a local physical force. Release or focus loss ends interaction. Space pauses; R disposes the old simulation and rebuilds at the current size. CPU/GPU buttons or Tab preserve the current particle state when switching.

The entire client area is the container, starting at 1152×800 with a 480×360 minimum. Resize edits real boundary geometry and transports existing contents into new bounds while preserving water mass and sequence time. It does not create water to maintain a fixed level. A reset recomputes the initial volume for the new size. Resources are released in body/space/shape and set/buffer/pipeline/shader order; the window retains its compute device across resets.

Opaque particle discs are drawn into one CanvasGroup with 0.65 opacity. The group is composited in front of both toys, so overlap between particles does not accumulate opacity. MultiMesh retains hardware instances inside CanvasGroup; transformed instance bounds determine the group region. Compatibility rendering remains functional but expands geometry. The frame budget allows one fixed interval per display frame so expensive CPU work does not accumulate catch-up steps; simulation time can lag wall time.

## Verification and measurements

```sh
ELECTRON2D_TEST_RENDERING_DEVICE=1 dotnet run --project tests/Electron2D.Tests -c Release
ELECTRON2D_TEST_PHYSICS_SANDBOX=1 dotnet run --project tests/Electron2D.Tests -c Release
ELECTRON2D_TEST_PHYSICS_SANDBOX_NATIVE=1 dotnet run --project tests/Electron2D.Tests -c Release
```

The focused simulation check exercises the full 65,536 particles for 20 simulated seconds, insertion order, finite containment, settled height, both floating toys, state-preserving CPU/GPU changes, mouse spring behavior and mass-preserving resize. It reports a short sequential settled-state CPU/GPU comparison using identical numerical settings. These are owner-thread full-step timings including rigid coupling, waits and GPU readback, not device timestamp measurements. CPU mode is substantially slower at this fixed population. Prepared engine solver-storage regressions survive in PhysicsSolverStorageTests.

On 2026-10-09 the short settled-state comparison on the local machine measured 933.589 ms per CPU step and 23.971 ms per GPU step (about 39× faster for this workload). CPU uses two warmup and eight measured steps; GPU uses eight warmup and 32 measured steps. The native rendered run reported roughly 36–54 FPS during the fall and toy interaction. These numbers do not establish stable 60 FPS, other hardware performance or zero allocation for the whole scene. The separate local-device test verifies zero owner-thread managed allocations for warmed dispatch and span readback.

The native check uses the ordinary Engine.Run lifecycle, saves falling-water/toy/resized PNGs under ignored `bin/water-playground/`, and injects mouse/key events for dragging both toys, CPU/GPU buttons, pause and reset. MultiMeshRenderingTests checks transparent grouped instance pixels and real hardware-stream selection on GPU, plus compatibility rendering. Linux x64/Wayland/.NET 10 and the local NVIDIA GeForce RTX 3090 Ti are the exercised profile; other platforms and owner visual acceptance remain separate. The [retired sandbox performance report](physics-sandbox-performance.md) does not describe this liquid scene.
