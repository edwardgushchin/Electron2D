# Independent GPU Smash preview

Last updated: 2026-10-09

## Launch

Run from the repository root:

```sh
ELECTRON2D_TEST_GPU_SMASH=1 dotnet run --project tests/Electron2D.Tests -c Release
```

This opens a dedicated 1152×800 Smash window, initially paused with 9,600 fragments.
Press **Launch block** or **B** to start. The regular WaterPlayground executable is now a separate water playground using
public CPU/GPU liquid algorithms and the public rigid-body space; this developer host does not add a public
backend selector or route through the older Box2D-hosted GPU experiment.

The preview requires an SDL GPU compute/render device. Initialization failures
propagate; there is no silent CPU simulation fallback. Startup prints the actual
compute driver and device. Linux Wayland/Vulkan is the verified native profile.

Set `ELECTRON2D_GPU_SMASH_COUNT` to an integer from 64 through 65,536 to choose the
initial population. The inspector also exposes population; **Rebuild wall** applies it.
The largest population is an overload option, not an established frame-rate target.

## Controls

| Control | Behavior |
| --- | --- |
| Launch block / B | Rebuild the wall, apply pending material/mass/population settings and launch at the configured speed |
| Rebuild wall / F / Reset / R | Apply pending settings and restore the scene, paused with no selection |
| Shockwave / N | Apply radial impulses to the existing fragments and resume |
| Play / Pause / P | Toggle fixed physics steps |
| Step / period | Pause and run exactly one fixed interval |
| Left drag | Select and move a body through bounded central impulses while running |
| Right click on a body | Apply a central kick and resume |
| Click empty space | Clear selection |
| Escape | Close the preview |

Hotkeys use physical keys, including on non-Latin keyboard layouts. Losing focus
or releasing the mouse over the toolbar releases a grab. There is one selection
outline and no collision-debug overlay. Neutral fragments have negligible motion;
pink and peach distinguish ordinary and faster motion relative to fragment size.

Gravity and time scale apply live. Launch speed, block mass, fragment mass,
friction and bounce apply when launching/rebuilding, as the inspector headings state.

## Execution and publication boundary

`GPUPhysicsSmashPreview` is test-only and owns its palette/layout helpers independently of the water example. It owns `GPUPhysicsBodyStore` directly and
uses the ordinary engine window, input routing, fixed callbacks and MultiMesh drawing.
The body store owns the authoritative body poses, broad/narrow phase, contacts and
constraint response. In zero gravity, fragments start asleep and wake through the
GPU contact graph when the block hits. The preview creates no scene rigid bodies, server body RIDs,
CPU contact mirror or second CPU solver. Four static device boundaries retain
fragments within the clipped playfield. Geometry and masses follow the original
Smash proportions, including its 20 scene-unit scale factor.

After each simulation step, `ReadChanges` obtains only changed body states, including
sleep/wake and identity, into a retained scratch array. The consumer retains display
poses/velocities for MultiMesh instances, motion colors and pointer picking. Only
changed instance records are rewritten; no visible changes means no instance-buffer
upload. A nonempty update still uses the ordinary full MultiMesh buffer upload.
This is not a GPU-to-GPU render publication path. The HUD separates simulation time
from explicit state-read time/bytes, including owner-thread waits. Records are 80 bytes
each, plus eight count/status bytes; unchanged bodies produce no records. See
[publication storage, waits and measurements](gpu-body-publication.md).

The preview caps catch-up to one physics interval per display frame, preserving
input responsiveness under overload. Simulation can run slower than wall-clock time.
Render FPS includes publication and rendering; solver-only benchmarks cannot be used
as its frame-rate prediction. This host does not establish CCD completeness, public
queries/events/direct-state semantics, CPU/GPU selection parity or networking acceptance.

## Verification

```sh
ELECTRON2D_TEST_GPU_SMASH=1 \
ELECTRON2D_GPU_SMASH_SMOKE=1 \
ELECTRON2D_GPU_SMASH_COUNT=9600 \
ELECTRON2D_GPU_SMASH_CAPTURE=/tmp/gpu-smash.png \
dotnet run --project tests/Electron2D.Tests -c Release
```

The smoke run sends real SDL mouse/key events to the same window and exits after
150 rendered frames. It checks GPU collision propagation before any shockwave,
pause and single step, selection/clear, dragging, right-click impulse, reset,
shockwave and changing population to 1,024. Optional captures save initial, impact,
grab and final images. The host rejects nonfinite displayed state.

The 512- and 9,600-fragment Linux/Vulkan runs passed; the latter observed 32,534 peak
contact points and 9,600 moved fragments across its actions. Initial/impact/grab
captures were visually inspected for a solid wall, clipped fragments, readable controls
and a single selection outline. These are native interaction/visual checks, not a
controlled sustained-performance benchmark or a full physics API acceptance test.

After changed-state publication, the 9,600-fragment smoke run passed again on
2026-10-09 (`/tmp/e2d-changes-smash.log`, 27,109 peak contact points). Impact/final
captures were inspected; the [publication report](gpu-body-publication.md) records
the observed frame-rate limitation separately from readback measurements.
