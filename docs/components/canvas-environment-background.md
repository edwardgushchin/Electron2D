# Canvas environment backgrounds

Assign a [CanvasEnvironment](../classes/CanvasEnvironment.md) to the scene's [WorldEnvironment](../classes/WorldEnvironment.md). Background selection follows the same per-world scene order and inherited viewport environment mode as glow, tone mapping and adjustment. The background precedes the normal canvas draw and any post-processing.

| Mode | Background before the canvas draw |
| --- | --- |
| `ClearColor` | The rendering session's default clear color. This is the initial mode. |
| `Color` | The environment's authored `BackgroundColor`. |
| `Keep` | The preceding unprocessed canvas. A newly allocated target starts with its ordinary initial clear. |

`BackgroundEnergyMultiplier` scales the default or authored clear color in linear RGB. SDR targets encode the result to sRGB and clip to their byte range. HDR targets retain signed linear values in the half-float range. The multiplier leaves canvas draws and clear alpha independent. Opaque viewports clear with alpha one, and transparent viewports clear to transparent black.

`ViewportClearMode.Never` retains the canvas in every background mode. `Keep` also retains it under `Always`. An explicit `Once` takes precedence and clears `Keep` to the session default, then changes the viewport clear mode to `Never` after successful submission. Texture-backed GPU and Compatibility viewports use native history. GPU MSAA retains its unresolved samples and active effects read unprocessed history. Direct-to-screen Compatibility draws can clear a region, but their native window backbuffer does not provide a retained per-viewport image for `Keep`.

The resource stores the mode, color and energy multiplier in typed scenes. The static [RenderingServer methods](../classes/RenderingServer.md#canvas-background) use the live environment's borrowed RID. `EnvironmentSetBGEnergy` sets the multiplier and a separate runtime linear exposure factor together. Assigning the resource multiplier restores unit exposure. Duplication and scene files retain authored settings.

## Verification

Run the Release test executable with `ELECTRON2D_TEST_CANVAS_BACKGROUND=managed`, `gpu` or `compatibility`. The managed profile checks authoring defaults, errors and typed scene round trips. The Windows profiles check native pixels with Vulkan, Direct3D 12, and Compatibility with Direct3D 11 and software rendering. GPU coverage includes HDR and RGBA8 with 4× MSAA, effect order, unprocessed history and finite extreme energy. Both native profiles check viewport inheritance, clear policies, transparency, live server writes and a warmed owner-thread allocation measurement.

Layer-limited backgrounds still need a native composition boundary that preserves the upper layers' blend and screen reads and the lower layers' raw history. Camera backgrounds need a live frame producer with a selected color layout and feed transform. Authored intensity in nits needs physical light units and canvas exposure normalization. These are separate coverage dependencies.
