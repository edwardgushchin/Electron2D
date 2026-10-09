# Water playground

Last updated: 2026-10-09

## Scope and ownership

[WaterPlayground](../../examples/WaterPlayground/README.md) is a standalone consumer of public Electron2D API. The example owns a 65,536-particle liquid, its compute resources, an explicit PhysicsServer space, a duck and a sailboat, and optional solid mechanisms. It adds no exported runtime type and does not close runtime physics coverage rows. Liquid CPU/GPU selection is independent of canvas renderer selection. Solid bodies and joints use the public CPU physics space in both liquid modes. The independent rigid-body GPU backend has separate acceptance work in [its component report](gpu-physics.md).

The basin is always 1152×800 scene units. One scene unit is 0.01 metres; the two-dimensional slice has 0.1 m thickness, rest areal density 100 kg/m² and approximately 3.072 m³ total volume. Equal-mass particles have approximately 2.165-unit spacing at the default count. A fixed reserve supplies the inlet; the explicit drain returns released particles to that same reserve. The scene never silently reduces its population for performance.

## Liquid calculation

Both paths implement position-based density constraints with identical settings: eight coupling intervals per 1/60-second tick, two pressure passes per interval, a smoothing radius of 2⅓ particle spacings, compression-only density constraints and numerical XSPH mixing scaled as `1 - exp(-2.4 * dt)`. The velocity filter is numerical stabilization, not a measured physical viscosity. This is a finite-resolution real-time approximation of an incompressible liquid. It has no air model, three-dimensional turbulence or physical surface tension, and does not establish exact CFD accuracy.

Side and bottom reflected samples supply wall pressure. Their derivatives are accumulated with the real particle, including the doubled self-reflection derivative; they are not counted as independent moving masses. Near a toy, a nearest-plane solid-side mirror sample supplies pressure support; its correction returns a matching linear/angular reaction impulse. Exact toy distance functions and mass/inertia-weighted contact projection enforce nonpenetration. The local-plane approximation loses accuracy at small, highly curved and closely adjacent features. Hydrostatic support, rest density and energy are checked separately from dynamic scenes.

The inlet uses narrow stratified jitter and a velocity derived from release rate and outlet width, avoiding the former compressed overlapping batches. Particles and entire toy silhouettes start above the visible entry plane. The top is open: neither a ceiling nor deletion/respawn clips splashes. A dense neighbor grid grows upward when needed; only the sides and bottom remain closed. Large artificial upward excursions are a regression check, not an accepted clipping mechanism.

Particle records are grouped by spatial cell once per outer tick, improving neighbor access locality on both CPU and GPU. Their ordering is private. Body transforms and rotation bases are prepared once per coupling interval. GPU pressure/contact work stays resident; a second reduction sends only final body impulses to CPU, followed by one retained full particle readback per outer tick for rendering and CPU-mode transitions. All compute resources fit eight storage bindings. CPU uses retained arrays and reusable work items on the existing system thread pool, with the calling thread participating. Completion and worker exceptions are joined before the next pass; no per-pass tasks or a second thread pool are created. Modes preserve state when switched but are not bitwise identical.

A trial grid pressure/PIC-FLIP implementation passed a resting-pool check but failed the full inflow workload through clustering, excessive excursions and much slower GPU steps. It was removed. Trial reductions in coupling frequency also failed the high-splash check and were removed. Lower numerical filtering with extra pressure iterations passed a long full-resolution run but generated energy in a coarser resting pool, so that candidate was also rejected. These experiments are not shipped behavior.

## Toys and interaction

The initial stream fills the basin over eight simulation seconds; the duck enters at nine seconds and boat at eleven. The bottom toolbar uses independent on/off buttons for:

- A hollow compound bucket, retaining and pouring actual particles through its open top.
- A pinned eight-paddle wheel, with a spring winch coupling its angle to a groove-guided platform and returning reaction torque. Strong fluid/cargo loads can stall the lift.
- A vertical kinematic gate, lowering gradually from above the pool and draggable upward to release a wave.
- A buoyant wooden block and sinking steel ball, including cargo resting on the boat.
- A buoyant ball that can be submerged and released.

Each pressed toy button creates one object; switching it off removes that kind, even while paused. Removing the wheel also frees both joints and its lift. Removal ends a grab on any deleted body, and subsequent activation creates a fresh mechanism with no retained winding or gate motion. Reset clears all toy toggles. The model retains eight cargo slots for programmatic use; removal releases every matching cargo body while preserving other kinds. Native checks cover switching all six buttons off/on while paused and resetting their states; CPU/GPU checks cover removal during a grab, repeated removal and recreation.

A physical mouse spring lifts toys and compensates gravity; empty-water dragging stirs nearby particles. Holding the wheel brakes it. Q/E or mouse wheel input tilts a held object. Without a held object, Q/E aims the jet and the wheel changes flow. The top handle moves the faucet; F or the Flow button cycles its valve. D or Drain toggles the bottom outlet. Once the reserve is empty, opening the drain enables recirculation.

H hides/restores the complete interface, including mode buttons, toolbar, text and faucet handle. Hidden button rectangles no longer intercept input; keyboard controls continue to work. Space pauses without activating a focused button, R resets, and Tab selects the liquid backend. Focus loss releases dragging.

## Presentation

The window scales the fixed world uniformly to its width and anchors the basin bottom to the lower edge. Aspect ratio changes expose more or less upper air; they never stretch objects or change existing physics coordinates or mass. Rotated sprite bounds retain complete toys, including beaks and sails, inside the side and bottom edges. The top remains open. F11 toggles borderless fullscreen and restores the previous window size.

[WaterSurface](../classes/WaterSurface.md) reconstructs a transparent density mesh from solved particle positions. Interior triangles merge into strips; the clipped free surface retains holes and splashes. There is no per-object waterline bridge, silhouette fill, harmonic extension or foreground overlay. The water is drawn in front of the toys.

Rendering is capped at 144 FPS and requests disabled VSync. On Linux, an available XWayland display is selected unless the caller explicitly chooses a video driver. Unsupported modes report the platform fallback. No desktop/system setting changes. One physics tick per host frame bounds catch-up work; simulation time can lag wall time under overload. The window keeps its compute device across resets and releases owned resources on disposal.

## Verification

```sh
ELECTRON2D_TEST_WATER_PLAYGROUND=1 dotnet run --project tests/Electron2D.Tests -c Release
ELECTRON2D_TEST_WATER_PLAYGROUND=1 ELECTRON2D_TEST_WATER_TOYS=1 dotnet run --project tests/Electron2D.Tests -c Release
ELECTRON2D_TEST_WATER_PLAYGROUND_NATIVE=1 dotnet run --project tests/Electron2D.Tests -c Release
ELECTRON2D_TEST_WATER_PLAYGROUND_NATIVE=1 ELECTRON2D_TEST_WATER_TOYS=1 dotnet run --project tests/Electron2D.Tests -c Release
```

The full-population check runs twenty simulated seconds, checks insertion order, offscreen births, open-top ballistic return, side/bottom containment, settled fill level, floating toys, state-preserving backend changes, physical grabbing and continuous basin rendering. Controlled toy checks exercise hydrostatic displaced-mass support, resting density/energy, inlet overcompression, bucket retention/pouring, wheel/lift coupling, gate release, cargo draft and finite recirculation in both backends.

Native checks use ordinary Engine.Run and real rendered readback, with injected input confined to tests. They exercise CPU/GPU controls, dragging, H, pause, refill, fullscreen and portrait/wide layouts. Captures and optional recordings live under ignored `bin/water-playground/`. Linux x64, .NET 10, XWayland and the local NVIDIA GPU are the exercised profile. Other platforms, whole-scene allocation guarantees and owner visual acceptance require separate evidence.

### Local measurements, 2026-10-09

On Ryzen 7 5700X / GeForce RTX 3090 Ti, the final sequential settled-state sample measured CPU **302.273 ms** (8 samples) and GPU **4.814 ms mean, 4.792 ms median, 5.325 ms p95** (128 samples after a one-second GPU warm-up). Both full-step measurements include coupling and readback; both measured **0 managed bytes per warmed step across threads**. The earlier Parallel.For implementation allocated about 207 KB per CPU step. These numbers cover this isolated simulation workload, not native-driver allocations or every UI frame.

The rendered 65,536-particle scene measured **124.6 mean FPS**, frame-time median **7.73 ms** and p95 **15.92 ms** over 748 frames after a wide resize. The cap was 144 and native VSync was disabled. With all optional toys, short native captures observed about 113–114 FPS and 6.6–7.5 ms liquid steps. A 120-second simulated soak retained its particle count and passed containment, flotation and excursion checks. PNG recording substantially reduces observed FPS and is excluded from these rendering measurements.

The controlled sphere supported 24.526 kg equivalent on CPU and 24.964 kg on GPU against 24.630 kg analytical displaced mass. A released water column reached x=1045 units after 0.5 seconds on both paths and its peak mechanical energy did not exceed its initial energy. The bucket retained all 320 seeded interior particles while lifted and poured them out when tilted; balanced boat cargo increased mean draft by 8.3/8.7 units on CPU/GPU. These checks establish bounded numerical behavior, not universal realism or owner visual acceptance.
