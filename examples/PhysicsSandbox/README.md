# PhysicsSandbox

Eleven interactive physics stories in a fixed **1152 × 800** desktop window, the same client size as the Electron2D editor splash. The example uses only public Electron2D APIs. The editor palette carries through to the sandbox: aubergine surfaces, cream text, pink accents and pastel physics objects. Shapes are drawn as retained geometry; no sprite downloads are needed.

Run from the repository root with .NET 10:

```bash
dotnet run --project examples/PhysicsSandbox -c Release
```

For local source builds of the private native dependencies:

```bash
dotnet run --project examples/PhysicsSandbox -c Release -p:Electron2DBuildNativeFromSource=true
```

The project references this checkout. It enables compiler/JIT optimization for itself and its runtime reference even in Debug, while retaining symbols and Debug backend assertions. This keeps a plain `dotnet run` usable for active particle loads. Windows, Linux and macOS desktop hosts use the same scene code; actual execution has been checked on Linux x64/Wayland. This example does not include mobile, TV or browser entry points.

## Choose a story

Use the upper-left dropdown. It supports pointer selection, arrow keys and Enter. Switching or resetting destroys the previous story, bodies, joints, borrowed geometry owned by that story and any independent server world. The new story starts from its factory configuration.

| Story | Your task | Main mechanisms |
| --- | --- | --- |
| Collision warehouse | Dismantle and rebuild three towers containing 72 crates; fire a heavy ball or release a shockwave | Rigid/static contact response, point grabbing, impulses, contact events, sleep, mass, friction |
| Marble delivery | Feed the machine and deliver marbles into the mint collection bowl; compare rubber, ice and absorbent clay | Materials, live shared resources, circles/capsules/polygons, segments, hollow terrain, sensors |
| Clockwork playground | Pull the pendulum and slider, power the wheel and tune the spring | Pin angular limits, motor/torque cap, groove, spring stiffness/damping, live joint changes |
| Gravity garden | Release orbiting seeds, relocate a star and reverse attraction | Point gravity with falloff, directional fields, priority, all five override modes, linear/angular damping |
| Rooftop courier | Collect four golden parcels across shelves, slopes and a moving lift | Grounded/floating character sliding, jump, floor snap, separation ray, one-way platform, platform carry |
| Radar rescue | Steer the probe to the golden beacon and inspect the route ahead | Ray/shape casts, wide-clearance versus ray hits, direct overlaps/manifolds/rest info, body motion tests, layers and exceptions |
| Orbital tug | Tow the cargo into the mint dock; disconnect/reconnect the cable, change thrust or cargo mass | Custom direct-body integration, forces/torque, spring ownership through shared scene/server RIDs, custom centre/inertia |
| Shape atelier | Morph a rolling creature without adding scene children, disable its extra geometry, and kick the independent-world marbles | Manual shape owners, live indexed replacement/pose/enablement, solid concave decomposition, standalone shape collision, explicit bodies/space, server Area monitoring/fields |
| Physics stress test | Stir 64–1,024 always-awake real particles, add more and compare normal/debug drawing | Dense solver contacts, body lifecycle, configurable world/material/object properties |
| Gravity Defied | Drive both wheels, lean and jump over ramps toward the golden finish | Groove guides, spring suspension, friction, torque, chassis mass and gravity |
| Angry birds | Pull and release the pink bird, aim with a gravity-aware preview and knock three mint targets down | Impulse velocity, mass, restitution, stacked timber contacts and solver-driven scoring |

Each story has three buttons below the stage, also bound to **B**, **N** and **F**. Their captions reflect current settings. The footer describes that story's movement controls and objective. The status strip shows actual body counts, collision transitions and deliveries; it does not simulate those results for display.

## Live parameters

Every scene has eleven native HSlider controls. Four affect the world: gravity, linear/angular damping and time scale. Six edit the clicked object's mass, friction, bounce, gravity scale and linear/angular damping; inapplicable rigid-body fields are disabled on static/character bodies. The last slider controls a story-specific quantity: impulse strength, spring stiffness, star gravity, move speed, sweep margin, tug thrust, particle count, wheel torque or sling power. Values show physical units/actual counts and edits update real public physics properties. Selecting another scene restores its factory settings and time scale.

Friction and bounce sliders show coefficient magnitudes and preserve the object's rough/absorbent material policy when edited, including the motorcycle tires.

Motorcycle controls: W/Up to drive, S/Down to reverse/brake, A/D to lean and Space for jump assist. For the bird, pull the pink projectile backward and release; N reloads it. The stress test deliberately keeps sleeping disabled, so its population remains an active solver load.

## Shared controls

- **Left drag:** grab at the clicked point with a bounded damped force. Rotation and solver contact response continue. Releasing over the toolbar or losing window focus releases the grab too.
- **Right click:** apply an offset impulse to a selected body, or create a body in empty space.
- **Q / E:** apply torque to the selected body.
- **K:** freeze/unfreeze the selected body. **H:** freeze it and alternate static/kinematic frozen modes; the kinematic mode can be dragged as a moving collider.
- **Z:** sleep/wake. **L:** lock/unlock rotation. **C:** toggle a persistent upward force.
- **V:** cycle disabled participation through Remove, MakeStatic, KeepActive, then enable processing again.
- **F3 / Debug:** show live collider geometry, disabled shape outlines, centres of mass, velocities, retained contacts/normals and joint anchors. The normal scene keeps its rods, spring cable and query visualization.
- **P / Pause:** suspend the scene space and the independent atelier space. Input, queries and UI remain available.
- **. / Step:** pause and advance exactly one fixed physics interval. **R / Reset:** recreate the current story. **Escape:** exit.

In the garden, **Shift + click** moves the pink star and **G** cycles field mixing. The courier and radar use **WASD / arrow keys**; **Space** jumps in grounded courier mode. The tug uses **W/S** for forward/reverse thrust and **A/D** for rotation. Movement keys do not toggle the debug layer.

## Implementation boundaries

This example demonstrates the implemented gameplay mechanisms, including their scene/server representations. It is not an exhaustive conformance test of every API overload or a claim that all physics coverage rows are complete. Public CCD modes, infinite boundary shapes, tile collider ownership, picking signals and unsupported joint bias/softness are outside the implemented engine profile. The debug layer belongs to this executable and reads public shape/contact data; it does not add a runtime debug-rendering API.

Physics bodies and shape poses retain unit scale and zero skew. Dimensions come from shape resources. The main scene uses the ordinary SceneTree fixed lane; only the atelier's explicitly created independent space uses `SpaceStep`. Rendering is interpolated; debug geometry observes the current solver pose. Bodies are capped at 160 per ordinary story and 1,024 in stress. The development gate requires zero managed bytes in every sampled prepared frame, with debug off and on. Scene construction, adding new bodies, configuration/resource edits and reset are explicit allocating operations. Native/GPU allocations remain unmeasured. Automatic marble feed recycles existing delivered pieces rather than constructing new nodes in fixed callbacks.

## Performance measurements

The development runner measures fixed simulation separately from the real GPU frame, including Debug on/off. It writes JSON reports under ignored `bin/physics-sandbox/`:

```bash
DOTNET_TieredCompilation=0 ELECTRON2D_TEST_PHYSICS_SANDBOX_PROFILE=1 ELECTRON2D_SANDBOX_PROFILE_TAG=current SDL_VIDEODRIVER=wayland dotnet run --project tests/Electron2D.Tests -c Release -p:Electron2DBuildNativeFromSource=true
```

Use `-c Debug -p:Optimize=true` to measure the optimized Debug configuration used by this example. Omitting both options measures the ordinary unoptimized runtime Debug profile instead. Profiling is test-only; the shipped example has no benchmark or capture switch. The scene/render owner's managed allocations are measured; native/GPU allocations are not. Samples exclude factory/reset work and fresh native input-event construction. Trials clear native GUI hover before warmup; cursor movement is injected through the public scene interaction path. Sustained native mouse-event allocation and native/GPU heaps are separate from this simulation/render budget. For focused stress measurements set `ELECTRON2D_SANDBOX_PROFILE_SCENE=8` and `ELECTRON2D_SANDBOX_STRESS_COUNT=1024`; optional headless mode uses `ELECTRON2D_SANDBOX_PROFILE_HEADLESS=1`. The JSON records actual warmup/sample counts and whether the runtime is optimized. FPS uses the normal 60 FPS cap and includes scheduling/presentation waits, so render duration does not represent CPU-only draw time.

The static stage is recorded once. Dynamic paths use their own canvas, stable shape-owner identities are cached, readouts refresh at 10 Hz and changing stroke data uses spans. Changing numeric readouts draws glyphs from a stack buffer, point/shape/contact queries fill reused destination buffers, contact storage is prepared by its configured cap and the solver retains freed slots and sleeping-island capacity. The profiler applies repeated impulses and moves the query cursor during warmup and measurement; any nonzero prepared fixed-step or frame allocation fails the gate. Parcel collection latches visibility in the event and disables monitoring on the next fixed callback, outside overlap delivery.

Set `ELECTRON2D_SANDBOX_PROFILE_LONG=1` for a three-minute 1,024-body settling run with debug enabled. It records fifteen-second windows and requires the final cadence to retain at least 80% of the settled 60–90-second cadence. This gate detects time-dependent degradation; it does not promise an absolute FPS on every host.

[Performance report](../../docs/components/physics-sandbox-performance.md) · [Physics capability and verification map](../../docs/components/physics-sandbox.md) · [Story source](PhysicsScene.Stories.cs) · [Game source](PhysicsScene.Games.cs) · [Parameter controls](SandboxWindow.Parameters.cs) · [Window source](SandboxWindow.cs)

The bundled IBM Plex Sans fonts retain their [SIL Open Font License](../../editor/Assets/IBMPlexSans-OFL.txt).
