# Canvas environment backgrounds

Assign a [CanvasEnvironment](../classes/CanvasEnvironment.md) to the scene's [WorldEnvironment](../classes/WorldEnvironment.md). Background selection follows the same per-world scene order and inherited viewport environment mode as glow, tone mapping and adjustment. Clear colors and camera feeds precede canvas items. Canvas mode controls which items participate in environment effects.

| Mode | Background before the canvas draw |
| --- | --- |
| `ClearColor` | The rendering session's default clear color. This is the initial mode. |
| `Color` | The environment's authored `BackgroundColor`. |
| `Canvas` | Canvas layers up to and including `BackgroundCanvasMaxLayer`. |
| `Keep` | The preceding unprocessed canvas. A newly allocated target starts with its ordinary initial clear. |
| `CameraFeed` | The active registered feed selected by `BackgroundCameraFeedID`. |

`BackgroundEnergyMultiplier` scales the default or authored clear color in linear RGB. SDR targets encode the result to sRGB and clip to their byte range. HDR targets retain signed linear values in the half-float range. The multiplier leaves canvas draws and clear alpha independent. Opaque viewports clear with alpha one, and transparent viewports clear to transparent black.

`ViewportClearMode.Never` retains the canvas. In `Canvas` mode it retains the background layer range. `Keep` also retains it under `Always`. An explicit `Once` takes precedence and clears `Keep` to the session default, then changes the viewport clear mode to `Never` after successful submission. Texture-backed GPU and Compatibility viewports use native history. GPU MSAA retains its unresolved samples and active effects read unprocessed history. Direct-to-screen Compatibility draws can clear a region, but their native window backbuffer does not provide a retained per-viewport image for `Keep`.

The resource stores the mode, color, energy multiplier, canvas layer limit and process-local feed ID in typed scenes. The static [RenderingServer methods](../classes/RenderingServer.md#canvas-background) use the live environment's borrowed RID. `EnvironmentSetBGEnergy` sets the multiplier and a separate runtime linear exposure factor together. Assigning the resource multiplier restores unit exposure. Duplication and scene files retain authored settings.

## Canvas layers

`BackgroundCanvasMaxLayer` defaults to zero and accepts the full signed integer range. On texture-backed viewports all canvas items at or below that layer draw before GPU glow, tone mapping and color adjustment. All items on the cutoff layer are included. Higher layers and tooltips blend over the resulting color in their ordinary draw order. Automatic foreground screen reads start from the processed background. A copy requested explicitly by a foreground item can include preceding foreground draws.

`Never` preserves the unprocessed background so live effects can be applied to the same color again. The renderer recomposes foreground items every frame. Removing a foreground item reveals the current background. Changing the cutoff or background mode preserves existing retained pixels until the next clear. On GPU MSAA viewports the renderer preserves the lower layers' sample history and seeds foreground samples from the processed background.

Texture-backed Compatibility targets retain the background range and recompose the foreground. Compatibility effects require a native effects pass. Direct-to-screen Compatibility uses ordinary layer order and the native window backbuffer.

## Camera feed

`BackgroundCameraFeedID` defaults to one. Zero leaves the ordinary clear active. Select a registered [CameraFeed](camera-feeds.md) and activate it explicitly through the camera API. Background selection and scene loading never activate a device. An absent, inactive or unsupported feed leaves the viewport's ordinary clear or retention policy in effect. A feed needs a new frame after reactivation. Removing a caller-owned feed leaves its activity and ownership with the caller.

The viewport draws the latest frame as its first canvas batch. The feed transform maps normalized viewport UVs to source UVs. The frame fills the viewport, uses nearest sampling and preserves RGB/RGBA alpha during composition. Canvas items and any enabled GPU post-processing follow. A renderer frame uses one source snapshot per selected feed across its viewports. Registration, activation, transform and frame publication invalidate rendering while a camera background is selected.

RGB uses ordinary color textures and follows the renderer's texture-format capabilities. Packed `Rg8` YUYV and per-pixel `Rgb8` or `Rgba8` Y, Cb, Cr use a retained RGBA8 conversion. Separate `R8` luminance and `Rg8` chroma use nearest chroma reconstruction from their plane dimensions. Application-submitted YCbCr uses full-range BT.601. Native YUYV retains BT.601 or BT.709 range and matrix metadata, with limited-range BT.601 for unspecified metadata. Native device RGB conversion remains available for other source color profiles. Color conversion runs once per changed plane revision before native upload.

Run `ELECTRON2D_TEST_CAMERA_BACKGROUND=managed`, `gpu` or `compatibility`. The Windows pixel profiles use a caller-owned feed to test RGB, packed and separate YCbCr, transform and edge sampling, activation and retirement, overlay order, HDR, MSAA and live prepared frames. Physical capture and external-image background import retain separate verification and ownership gates.

## Verification

Run the Release test executable with `ELECTRON2D_TEST_CANVAS_BACKGROUND=managed`, `gpu` or `compatibility`. The managed profile checks authoring defaults, errors and typed scene round trips. The Windows profiles check native pixels with Vulkan, Direct3D 12, and Compatibility with Direct3D 11 and software rendering. GPU coverage includes HDR and RGBA8 with 4× MSAA, effect order, unprocessed history and finite extreme energy. Both native profiles check viewport inheritance, clear policies, transparency, live server writes and a warmed owner-thread allocation measurement.

Run `ELECTRON2D_TEST_LAYER_BACKGROUND=managed`, `gpu` or `compatibility` to exercise typed scene storage, the inclusive layer boundary, upper-layer blending, GPU screen reads, retained unprocessed history, viewport bypass and live RID writes. The Windows native profiles exercise HDR and 4× MSAA where supported and measure owner-thread allocations after warm-up.

Authored intensity in nits needs physical light units and canvas exposure normalization.
