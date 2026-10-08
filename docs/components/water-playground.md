# Water playground

Last updated: 2026-10-09

[WaterPlayground](../../examples/WaterPlayground/README.md) now contains one full-window, resizable water scene. The former twelve-story selector, inspectors and scene code were removed. [WaterWindow](../classes/WaterWindow.md) owns input and presentation; [WaterSimulation](../classes/WaterSimulation.md) owns the application-side liquid and its coupling to rigid toys. The example consumes only public Electron2D API.

## Physical model

The initial 1152×800 container holds 65,536 equal-mass particles. One hundred scene units represent a metre and the modeled slice is 0.1 m deep, giving 3.072 m³ at a bulk density of 1,000 kg/m³. Positions, velocities and mass are actual simulation state. The renderer follows those positions; there is no scripted waterline or floating trajectory.

The liquid uses an iterative density-constraint method based on [Position Based Fluids](https://mmacklin.com/pbf_sig_preprint.pdf). Each interval predicts motion under gravity, finds local neighbors, estimates density with a normalized two-dimensional poly6 kernel, solves nonnegative compression using a spiky gradient, projects contacts, reconstructs velocity and applies XSPH viscosity. The numerical profile uses four density iterations, four coupling intervals per 1/60 s tick, 0.25 Jacobi relaxation and 0.1 viscosity. Mirrored wall neighbors supply density support near the container. This finite-iteration real-time model approximates incompressibility and dissipates energy; it is not an exact CFD solution and does not implement air, surface tension or vorticity confinement.

The retained dense neighbor grid expands its upper extent when splashes leave the prepared range. It does not leave escaped particles permanently concentrated in a clamped boundary cell. Particle count remains constant. CPU and GPU implement the same passes and parameters. GPU storage stays resident throughout projection iterations; one state read publishes all 65,536 positions/velocities per outer tick. Small reductions return reaction impulses at each coupling interval. CPU mode uses retained arrays with parallel particle passes; the modes are numerically comparable, not bitwise identical.

The duck and boat are public PhysicsServer bodies with capsule/circle and convex-hull geometry. Gravity, rigid-body contacts, mass and inertia use the existing CPU space in both modes. Fluid contacts exchange linear/angular impulses with these bodies before advancing that space. Thus GPU mode identifies the liquid solver; it does not claim that the still-incomplete independent rigid-body GPU backend is exposed. The separate [GPU Smash developer preview](gpu-smash-preview.md) remains test-only and independent of this example.

## Interaction, rendering and lifetime

Water pours from an offscreen moving outlet over eight simulation seconds. The pool reserves 65,536 particles; only released particles take part in queries, constraints and rendering. The duck is inserted at nine seconds, the boat at eleven. A point spring with gravity compensation and a sufficiently high bounded force lifts either toy, including a grab on the sail. Empty-water dragging applies a local force. Release or focus loss ends interaction. Space pauses; R refills; CPU/GPU buttons or Tab preserve released particles and sequence time.

The physical basin is always 1152×800 scene units, independently of window size, fullscreen state and rendering backend. A uniform fit transform centers it in the client area; input uses the inverse transform. Resizing never moves bodies, rescales water, changes its mass or rebuilds physics buffers. F11 toggles ordinary borderless fullscreen and restores the prior window size. The example requests a maximum of 144 rendered frames per second and disabled vertical synchronization. On Linux Wayland sessions with an available X display it prefers XWayland for immediate presentation, while respecting an explicit SDL_VIDEO_DRIVER choice. The selected policy is shown in the window; unsupported native modes report the enabled fallback. No system or desktop configuration is modified.

[WaterSurface](../classes/WaterSurface.md) splats solved particles into a retained four-unit density grid, smooths it, and clips triangles to the density contour. Full interior runs merge into strips. This replaces the old 65,536-disc draw with a compact mesh, including transparent feathered edges and depth tint. It uses public CanvasItem triangle submission and no per-frame image upload. The water is drawn after toys and six fish; immersed toy silhouettes receive the foreground water tint. Fish use application-side steering inside the measured wet field and are shown only while submerged; they are visual swimming agents, not additional rigid/fluid collision bodies.

The fixed step allows one physics interval per display frame so slow CPU work does not accumulate catch-up steps. Simulation time may lag wall time under overload. The window retains its compute device across refills and releases its own simulation resources on disposal.

## Verification and measurements

```sh
ELECTRON2D_TEST_RENDERING_DEVICE=1 dotnet run --project tests/Electron2D.Tests -c Release
ELECTRON2D_TEST_WATER_PLAYGROUND=1 dotnet run --project tests/Electron2D.Tests -c Release
ELECTRON2D_TEST_WATER_PLAYGROUND_NATIVE=1 dotnet run --project tests/Electron2D.Tests -c Release
```

The focused simulation check exercises the full 65,536 particles for 20 simulated seconds, insertion order, finite containment, settled height, both floating toys, state-preserving CPU/GPU changes, mouse spring behavior and a fixed physical world. It reports a short sequential settled-state CPU/GPU comparison using identical numerical settings. These are owner-thread full-step timings including rigid coupling, waits and GPU readback, not device timestamp measurements. CPU mode is substantially slower at this fixed population. Prepared engine solver-storage regressions survive in PhysicsSolverStorageTests.

The final 2026-10-09 headless check after switching to a progressive pour and a four-by-four constraint profile measured 303.175 ms per CPU step and 9.200 ms per GPU step on the local Ryzen 7 5700X/RTX 3090 Ti. The earlier eight-by-six profile measured 933.589/23.971 ms; these are different solver profiles, not an isolated backend optimization ratio. CPU/GPU comparisons within each run use identical settings. A native XWayland run of the new surface reported roughly 99–126 FPS during the pour, toy motion and resize. These are short observations, not a stable 144 FPS guarantee or an allocation measurement for the entire scene.

The native check uses the ordinary Engine.Run lifecycle, saves falling-water/toy/resized PNGs under ignored `bin/water-playground/`, and injects mouse/key events for dragging both toys, CPU/GPU buttons, pause, refill, F11 round-trip, fixed world coordinates and six submerged fish. Native captures exercise the triangle surface on both rendering backends. Linux x64/XWayland/.NET 10 and the local NVIDIA GeForce RTX 3090 Ti are the exercised profile; other platforms and owner visual acceptance remain separate. The [retired sandbox performance report](physics-sandbox-performance.md) does not describe this liquid scene.
