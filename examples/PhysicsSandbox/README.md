# PhysicsSandbox

Twelve interactive physics stories in a fixed **1152 × 800** desktop window, the same client size as the Electron2D editor splash. The example uses only public Electron2D APIs. The supplied visual reference sets the palette: dark plum surfaces, cream text, pink accents, and berry, blush and apricot physics objects. Shapes are drawn as retained geometry; no sprite downloads are needed.

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
| Marble delivery | Feed the machine and deliver marbles into the blush collection bowl; compare rubber, ice and absorbent clay | Materials, live shared resources, circles/capsules/polygons, segments, hollow terrain, sensors |
| Clockwork playground | Pull the pendulum and slider, power the wheel and tune the spring | Pin angular limits, motor/torque cap, groove, spring stiffness/damping, live joint changes |
| Gravity garden | Release orbiting seeds, relocate a star and reverse attraction | Point gravity with falloff, directional fields, priority, all five override modes, linear/angular damping |
| Rooftop courier | Collect four apricot parcels across shelves, slopes and a moving lift | Grounded/floating character sliding, jump, floor snap, separation ray, one-way platform, platform carry |
| Radar rescue | Steer the probe to the apricot beacon and inspect the route ahead | Ray/shape casts, wide-clearance versus ray hits, direct overlaps/manifolds/rest info, body motion tests, layers and exceptions |
| Orbital tug | Tow the cargo into the blush dock; disconnect/reconnect the cable, change thrust or cargo mass | Custom direct-body integration, forces/torque, spring ownership through shared scene/server RIDs, custom centre/inertia |
| Shape atelier | Morph a rolling creature without adding scene children, disable its extra geometry, and kick the independent-world marbles | Manual shape owners, live indexed replacement/pose/enablement, solid concave decomposition, standalone shape collision, explicit bodies/space, server Area monitoring/fields |
| Physics stress test | Stir 64–1,024 always-awake real particles, add more and compare simulation and drawing cost | Dense solver contacts, body lifecycle, configurable world/material/object properties |
| Gravity Defied | Drive both wheels, lean and jump over ramps toward the apricot finish | Groove guides, spring suspension, friction, torque, chassis mass and gravity |
| Angry birds | Pull and release the pink bird, aim with a gravity-aware preview and knock three blush targets down | Impulse velocity, mass, restitution, stacked timber contacts and solver-driven scoring |
| Smash | Launch a heavy block through 64–65,536 small square fragments in zero gravity; rebuild or blast the cloud apart | Sleeping/wake propagation, dense rectangular contacts, momentum transfer, damping and live population |

Each story has three buttons below the stage, also bound to **B**, **N** and **F**. Their captions reflect current settings. The footer describes that story's movement controls and objective. The highlighted button is the primary action. Hotkeys stay in tooltips and the shared footer. FPS and body count have separate readouts; scene results appear only for stories that score them.

## Live parameters

The inspector separates native HSliders into **World**, **Object** and **Scene** tabs. New and reset scenes start with no selected object. Clicking empty field space clears the selection and shows the Object tab’s selection prompt without sliders. Clicking an object opens its tab; a pink selection outline and a named label link it to the inspector. Each row shows its unit, range and a factory-value tick, with one clean circular thumb. Counts are integers and multipliers use ×. Four affect the world: gravity, linear/angular damping and time scale. Six edit the clicked object's mass, friction, bounce, gravity scale and linear/angular damping; inapplicable rigid-body fields are disabled on static/character bodies. The last slider controls a story-specific quantity: impulse strength, spring stiffness, star gravity, move speed, sweep margin, tug thrust, particle count, wheel torque, sling power or Smash impact speed. Smash adds a second scene slider for 64–65,536 fragments. Values show physical units/actual counts and edits update real public physics properties. Selecting another scene restores its factory settings and time scale.

Friction and bounce sliders show coefficient magnitudes and preserve the object's rough/absorbent material policy when edited, including the motorcycle tires.

Motorcycle controls: W/Up to drive, S/Down to reverse/brake, A/D to lean and Space for jump assist. For the bird, pull the pink projectile backward and release; N reloads it. The stress test deliberately keeps sleeping disabled, so its population remains an active solver load. Smash instead begins with 9,600 sleeping fragments and one heavy moving block. Their bodies collide individually; one retained colored-stroke batch draws their rotated square silhouettes, interpolating unit-scale position and angle. Shape size adjusts with population to keep the wall framed. Physics uses geometry twenty times the screen size and a 0.05 camera scale, keeping small visual fragments above solver tolerances; the block is 1,280 units wide and default impact speed is 12,000 u/s. Fragment contact notifications are disabled; the block still reports real contacts. B rebuilds and launches a fresh impact, N sends a radial shockwave, and F rebuilds an idle wall. Speed and fragment count are under Scene; count edits rebuild the wall. Object mass accepts 0.0001–20 kg so each 0.0045 kg fragment and the 12 kg block remain editable.

Smash colors follow physical state: sleeping bodies use muted gray-purple, awake slow bodies pink, and fast bodies apricot. Fast means the maximum linear-plus-angular step travel exceeds one quarter of the square side; the block uses its own 1,280-unit side. The check uses public velocity/sleep properties and the last fixed interval, matching the reference speed/size rule without exposing backend diagnostics. Frozen static/kinematic bodies use blush/berry.

Smash is inspired by the [video reference](https://www.youtube.com/watch?v=_a1QxD4Al_w) and its [original scene mechanism](https://github.com/ikpil/Box2D.NET/blob/main/src/Box2D.NET.Shared/Benchmarks.cs). It is authored with public Electron2D APIs; the sandbox keeps its own layout, palette, bounded arena and 65,536-fragment ceiling.

The window uses one fixed physics interval per rendered frame at most, then restores the previous engine budget on disposal. When a stress load exceeds the fixed interval, simulation slows rather than spending several catch-up intervals before handling input/drawing. The 65,536 limit is an overload test, not a 60 FPS guarantee. Fragment shapes/materials are shared in groups of 128 to bound event subscription and teardown copying.

## Shared controls

- **Left drag:** grab at the clicked point with a bounded damped force. Rotation and solver contact response continue. Releasing over the toolbar or losing window focus releases the grab too.
- **Right click:** apply an offset impulse to a selected body, or create a body in empty space.
- **Q / E:** apply torque to the selected body.
- **K:** freeze/unfreeze the selected body. **H:** freeze it and alternate static/kinematic frozen modes; the kinematic mode can be dragged as a moving collider.
- **Z:** sleep/wake. **L:** lock/unlock rotation. **C:** toggle a persistent upward force.
- **V:** cycle disabled participation through Remove, MakeStatic, KeepActive, then enable processing again.
- **P / Pause:** suspend the scene space and the independent atelier space. Input, queries and UI remain available.
- **. / Step:** pause and advance exactly one fixed physics interval. **R / Reset:** recreate the current story. **Escape:** exit.

In the garden, **Shift + click** moves the pink star and **G** cycles field mixing. The courier and radar use **WASD / arrow keys**; **Space** jumps in grounded courier mode. The tug uses **W/S** for forward/reverse thrust and **A/D** for rotation.

## Implementation boundaries

This example demonstrates the implemented gameplay mechanisms, including their scene/server representations. It is not an exhaustive conformance test of every API overload or a claim that all physics coverage rows are complete. Public CCD modes, infinite boundary shapes, tile collider ownership, picking signals and unsupported joint bias/softness are outside the implemented engine profile.

Physics bodies and shape poses retain unit scale and zero skew. Dimensions come from shape resources. The main scene uses the ordinary SceneTree fixed lane; only the atelier's explicitly created independent space uses `SpaceStep`. Rendering is interpolated. The field and inspector share a 536-unit height. Scene-specific canvas framing enlarges gameplay; the motorcycle camera follows the chassis. A public Control with ClipContents clips all story/selection descendants above the action dock. Its translation cancels the scene translation, preserving unit-scale world poses. Invisible enclosure colliders retain physical boundaries; the visible floor is a thin line. Crates have clean silhouettes, the bike and slingshot use simple geometric illustrations, and trajectories appear only during aiming. Bodies are capped at 160 per ordinary story, 1,024 in stress and 65,537 in Smash (65,536 fragments plus the block). The development gate requires zero managed bytes in every sampled prepared frame, with normal scene and inspector drawing. Scene construction, adding new bodies, configuration/resource edits and reset are explicit allocating operations. Native/GPU allocations remain unmeasured. Automatic marble feed recycles existing delivered pieces rather than constructing new nodes in fixed callbacks.

## Performance measurements

The development runner measures fixed simulation separately from the real GPU frame, including scene and inspector drawing. It writes JSON reports under ignored `bin/physics-sandbox/`:

```bash
DOTNET_TieredCompilation=0 ELECTRON2D_TEST_PHYSICS_SANDBOX_PROFILE=1 ELECTRON2D_SANDBOX_PROFILE_TAG=current SDL_VIDEODRIVER=wayland dotnet run --project tests/Electron2D.Tests -c Release -p:Electron2DBuildNativeFromSource=true
```

Use `-c Debug -p:Optimize=true` to measure the optimized Debug configuration used by this example. Omitting both options measures the ordinary unoptimized runtime Debug profile instead. Profiling is test-only; the shipped example has no benchmark or capture switch. The scene/render owner's managed allocations are measured; native/GPU allocations are not. Samples exclude factory/reset work and fresh native input-event construction. The profile window requests no activation when shown and suppresses fresh native mouse/keyboard events during measurement, restoring their previous states on completion. Trials clear GUI hover with queued native motion before warmup; cursor movement is injected through the public scene interaction path. Sustained native mouse-event allocation and native/GPU heaps are separate from this simulation/render budget. For focused stress measurements set `ELECTRON2D_SANDBOX_PROFILE_SCENE=8` and `ELECTRON2D_SANDBOX_STRESS_COUNT=1024`; optional headless mode uses `ELECTRON2D_SANDBOX_PROFILE_HEADLESS=1`. The JSON records actual warmup/sample counts and whether the runtime is optimized. FPS uses the normal 60 FPS cap and includes scheduling/presentation waits, so render duration does not represent CPU-only draw time.

The static stage is recorded once. Dynamic paths use their own canvas, stable shape-owner identities are cached, readouts refresh at 10 Hz and changing stroke data uses spans. Changing numeric readouts draws glyphs from a stack buffer, point/shape/contact queries fill reused destination buffers, contact storage is prepared by its configured cap and the solver retains freed slots and sleeping-island capacity. The profiler applies repeated impulses and moves the query cursor during warmup and measurement; any nonzero prepared fixed-step or frame allocation fails the gate. Parcel collection latches visibility in the event and disables monitoring on the next fixed callback, outside overlap delivery.

Set `ELECTRON2D_SANDBOX_PROFILE_LONG=1` for a three-minute 1,024-body settling run with the normal scene drawing. It records fifteen-second windows and requires the final cadence to retain at least 80% of the settled 60–90-second cadence. This gate detects time-dependent degradation; it does not promise an absolute FPS on every host.

[Performance report](../../docs/components/physics-sandbox-performance.md) · [Physics capability and verification map](../../docs/components/physics-sandbox.md) · [Story source](PhysicsScene.Stories.cs) · [Game source](PhysicsScene.Games.cs) · [Parameter controls](SandboxWindow.Parameters.cs) · [Window source](SandboxWindow.cs)

The bundled IBM Plex Sans fonts retain their [SIL Open Font License](../../editor/Assets/IBMPlexSans-OFL.txt).
