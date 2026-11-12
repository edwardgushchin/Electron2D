# Canvas tone mapping

A [WorldEnvironment](../classes/WorldEnvironment.md) selects a [CanvasEnvironment](../classes/CanvasEnvironment.md) for the world. A GPU viewport reads that environment after rendering the canvas and resolving MSAA. Both ordinary RGBA8 and [HDR canvas storage](hdr-canvas.md) use the same operators. The viewport environment mode controls whether it uses the selected resource. Window and SubViewport targets can use independent worlds or share one resource.

`TonemapExposure` multiplies linear RGB before the operator. `TonemapMode` selects Linear, Reinhard, Filmic, ACES or AgX. Linear with unit exposure takes the ordinary canvas path. Reinhard uses the greater of the authored white point and the attached window's output range. Filmic and ACES normalize their curves at the authored white point with a minimum of one. AgX uses its own white point and contrast. Its white point uses a minimum of two and scales with the window's output range. Offscreen targets with no containing output use a linear maximum of one. The pass preserves alpha. HDR output stores finite linear values in the completed half-float viewport texture. Native window presentation and movie capture apply their usual output transfer later.

Exposure also affects bright-pixel extraction for [canvas glow](canvas-glow.md). Additive, Screen, Replace and Mix combine source and glow before tone mapping. SoftLight combines their tone-mapped values. Tone mapping shares the existing post-process scratch target with glow. With glow inactive it samples the raw canvas and can apply [color adjustment](canvas-color-adjustment.md) in the same pass. The backend retains raw draw history for Never-clear targets, and the resource setters and server calls keep the logical texture and existing device alias alive.

## SDR canvas environments

An RGBA8 viewport keeps its ordinary rendering and MSAA resolve. The environment reads the resolved sRGB bytes and decodes them before exposure, glow, tone mapping and adjustment. The byte viewport uses unit output headroom. Its result clips to the SDR range and encodes to sRGB once before writing back to RGBA8. The output retains the original alpha and the logical and native texture identities.

The retained raw history uses the viewport's own format. The single-source pass decodes its exact byte sample. With active blur levels the renderer first decodes to a native half-float image so the glow kernel filters linear values. It releases this extra image with the glow levels. Never-clear frames seed the scene from unprocessed pixels, and disabling the environment restores ordinary output. Screen textures within the scene still read its pre-effect content.

Run the Release test executable with `ELECTRON2D_TEST_SDR_ENVIRONMENT=gpu` or `compatibility`. The Windows GPU profile checks byte and 4x MSAA output, exposure and Reinhard against numeric references, a checkerboard that distinguishes linear filtering from encoded averaging, glow map atlases and texture proxies, LUT and alpha, unprocessed history, inherited bypass and warmed allocations. The Direct3D 11 Compatibility profile checks the ordinary byte path.

## Verification and remaining profiles

Run the Release test executable with `ELECTRON2D_TEST_TONEMAP=managed`, `gpu` or `compatibility`. The managed profile checks stored descriptors, scene-local copies, argument validation and headroom coefficients. The Vulkan profile compares all five modes with independent double-precision color samples on a window and a 4x-MSAA SubViewport. It checks alpha, server edits, texture identity, raw history, environment bypass, exposure-dependent glow and finite extreme values. The Direct3D 11 Compatibility profile exercises ordinary SDR output with tone settings authored.

Compatibility needs its own executing environment path. [Canvas mode](canvas-environment-background.md#canvas-layers) applies the effect before drawing higher layers on GPU viewports. The physical HDR display, additional drivers and target platforms, native allocation counters, performance and release-package qualification retain their own verification gates.
