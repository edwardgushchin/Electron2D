# HDR canvas targets

Last updated: 2026-10-20

[Viewport.UseHDR2D](../classes/Viewport.md#hdr-canvas-rendering) selects linear RGBA half-float canvas storage on the existing GPU backend. It defaults to false. [ProjectSettings.HDR2D](../classes/ProjectSettings.md#root-canvas-hdr) names `rendering/viewport/hdr_2d`; Engine startup reads its active feature override for an unassigned root Window. An explicit root assignment wins, and SubViewport values remain independent. [RenderingServer.ViewportSetUseHDR2D](../classes/RenderingServer.md#viewportsetusehdr2d) applies the same behavior to a live scene or caller-owned viewport as a renderer-session override.

## Authoring and lifetime

The scene getter retains authored state after a server override. Any valid scene assignment, including an equal value, republishes authoring. Stored descriptors preserve the bool through PackedScene, resource copying, and file reconstruction without serializing native objects. Invalid/wrong-owner/stale identities and off-owner access reject. Server mutation rejects active canvas capture/submission. Initial scene drawing may edit authoring before the renderer recaptures targets; native-submission callbacks cannot mutate it.

A change publishes effective texture format metadata and notifies every borrowed ViewportTexture before public viewport observers. Observer exceptions aggregate after the state commits. Native replacement happens on the next submission. Size, HDR and MSAA jointly identify target storage; replacement is prepared before retiring old handles. The logical texture RID remains stable, while borrowed RenderingDevice aliases expire when their native target retires. Readback still describes the last completed target, so its format can differ from newly selected metadata until the next submission. Disabled/hidden targets keep ordinary update-policy behavior.

## Color and sampling

HDR target storage is `Image.Format.Rgbah` / `RenderingDevice.DataFormat.R16G16B16A16SFLOAT`, eight bytes per pixel. Framebuffer reads and shaders retain linear RGB, including negative and above-one values within half-float precision. Alpha remains linear. The default byte target remains RGBA8 with its existing behavior.

Authored primitive/command modulation and clear colors are converted from sRGB. Polygon/mesh vertex colors and their command modulation are converted separately before multiplication. MultiMesh instance colors retain the same semantics through native instancing and the snapped/skinned CPU fallback. Unlit CanvasModulate colors remain a separate per-batch factor and convert independently of command colors. Built-in group/Only-mask owners are unshaded; shared AndDraw color/mask geometry retains distinct pass factors and converts its original vertices once. This also fixes the existing repeated canvas modulation in ordinary GPU/Compatibility groups and masks. Automatic ambient/light/specular colors convert independently of energy and geometric lighting. Nonfinite conversion overflow rejects instead of publishing invalid command colors. Built-in group RGB is converted before premultiplication; coverage, masks, normal/specular texture data, glyph distance fields and LCD coverage remain data.

Ordinary byte color textures, including their decoded LDR compressed sources, use a private retained half-float sampling view. Every stored source mip is converted before consumer filtering. The view has exactly the source's mip count, including partial RenderingDevice chains. The renderer refreshes it from native storage each submitted draw, preserving device writes without copying a framebuffer through managed memory. Shared sources retain raw storage for ordinary viewports, CPU getters and native device aliases. Source changes, resize, disposal and renderer shutdown retain existing cache ownership.

Float/HDR and UInt16 scalar image sources remain linear/numeric. Native sRGB RenderingDevice formats already decode during hardware sampling and receive no second transfer. UNORM byte/BC/ASTC device color formats use the derived view. Typed integer samplers remain raw and retain their nearest-filter requirement. Named GLSL/HLSL sampler slots and numeric/color uniforms remain typed numeric data under the existing shader profile; no source-color hint language is introduced. The reserved `TEXTURE` color input follows HDR canvas sampling, and `SCREEN_TEXTURE` reads linear target/backbuffer pixels. Existing explicit native capabilities still govern supported formats, storage usage and filtering.

Sampling an HDR ViewportTexture into another HDR target preserves its linear values. Sampling an ordinary viewport into HDR decodes its completed byte image. An ordinary target retains its existing numeric sampling of float sources, including HDR viewport images; applications select matching color spaces or explicitly encode in their shader. Feedback/cycles use prior completed storage, never the active writable attachment.

## Composition, presentation and capture

Completed/write targets, screen copies, group/mask backbuffers and optional 2/4/8-sample attachments use matching half-float formats and pipelines. Resolve, pass restart, Never-clear history and generated screen/group mips preserve linear values.

By default the root presents through a separate SDR byte image, applying linear-to-sRGB transfer and clipping values outside the SDR range. [Native HDR output](native-hdr-output.md) instead selects a per-window extended-linear or BT.2020 ST2084 swapchain with an explicit request, capability and luminance policy. Both paths preserve fitted presentation and independent logical half-float viewport readback.

Movie capture uses the public completed viewport image, retains crop/resize in its source precision, then encodes linear RGB before RGB8 quantization for every custom and built-in writer. SDR capture keeps its existing bytes. [Movie writing](movie-writing.md) retains its fixed dimensions, rational PCM clock, explicit alpha loss and cold readback/allocation contract. Public `Image.LinearToSRGB()` keeps its RGB8/RGBA8-only contract; the capture conversion is internal.

## Executable verification and limits

[CanvasHDRTests](../../tests/Electron2D.Tests/CanvasHDRTests.cs) and its storage/source/mesh/movie partials exercise actual GPU pixels and public host workflows. Profiles selected by `ELECTRON2D_TEST_HDR` are `managed`, `gpu`, `compatibility`, `color`, `sources`, `mesh`, `movie`, `msaa`, `composition`, `lighting`, `programmable-lighting`, `glyph`, `glyph-msaa`, `modulation`, `modulation-byte` and `modulation-compatibility`. The managed profile also loads scene/project files in a fresh process.

Native checks cover 2.5-valued half-float output versus byte clipping, exact device format/stride and alias expiry, committed observer failures, authoring precedence, owner/capture guards, linear black/white interpolation, live native byte writes and source restoration, SDR presentation, full/partial mips and high-LOD clamping, native sRGB, shared independent viewport formats and resize, GLSL/HLSL color/data inputs, separate mesh/instance colors and fallback, screen/group/mask composition, 1/2/4/8 sample coverage and retained history. Twenty-seven diffuse/normal/specular scalar combinations are compared against float references in both lighting paths. Scene/server LCD and MSDF checks retain coverage data, linear tint/clear blending and 64 active/64 idle zero-byte intervals with and without MSAA. Four alternating HDR/SDR movie frames check dark gradients, output clipping, RGB8 transfer before quantization, exact silent PCM and borrowed-frame cleanup; PNG files permit independent inspection.

Warmed owner-thread measurements cover 64 active color redraws, 64 active plus 64 idle MSAA frames, active/idle group composition, and 32 active frames in each lighting path with zero managed bytes in the measured intervals. These are bounded checks, not whole-engine/native allocation or hardware-performance acceptance. Compatibility explicitly rejects enabled HDR before native target publication. Devices lacking half-float render/sampler or requested sample-count support reject explicitly. Foreign GPU drivers/platforms, browser WebGPU, HDR displays, human visual acceptance and device-loss recovery retain separate platform gates under ADR 0021. The existing Wayland hide/remap capability restriction is unchanged.

A local public NuGet candidate, `0.1.0-viewporthdr.20261016.2` over the unchanged Linux native package `0.1.0-movie.20261015.4`, passes self-contained CoreCLR and NativeAOT consumers with reflection-based JSON disabled. Both load authored scene/project files in a fresh process, render actual HLSL half-float pixels, inspect borrowed device format/stride and alias expiry, execute movie transfer/PCM callbacks, and reject unsupported Compatibility HDR with cleanup. This verifies local package consumption; package publication and foreign/native-display acceptance are separate.

## Screen debanding

`Viewport.UseDebanding`, `RenderingServer.ViewportSetUseDebanding` and `ProjectSettings.UseDebanding` form the stored/live/startup contract. Defaults are false. Startup uses the active feature override for an unassigned root; explicit authoring wins. Scene files and project feature values survive fresh-process reconstruction. Session overrides preserve authored getters; even an equal authored assignment republishes the scene selection. Target identity, precision, borrowed native aliases and texture notifications remain unchanged when only debanding changes. Ordinary owner/disposal/capture/submission guards apply, including caller-owned and stale server viewports.

In the strictly 2D rendering domain, the effect runs only while presenting the root HDR canvas. Its existing color-copy shader encodes linear RGB to sRGB, adds a stable screen-coordinate RGB dither, then writes the SDR byte attachment. The three phases use the fractional parts of `dot((171,231), pixelCenter) / (103,71,97)`; subtracting one half and dividing by 255 gives zero-centered noise with a total range of one quantization step. Native normalized storage clips out-of-range output. Alpha is untouched. The pattern uses final client-pixel centers after attachment fitting/clipping, remains stable between identical frames and uses the ordinary resolved source with MSAA. No extra image or render pass is allocated.

Linear viewport textures, offscreen HDR canvases, child-window viewports, ordinary byte canvases and movie capture retain their original values. Turning the flag on in these 2D roles cannot introduce quantization noise into reusable source images. Compatibility rejects enabled debanding at target preparation under ADR 0028; it never reports a performed effect. Tone mapping, a 3D environment/material effect and HDR monitor output remain outside this implementation.

`CanvasDebandingTests` checks fresh scene/project files, real screen versus linear/offscreen pixels, an independent per-channel quantization oracle, negative/above-one clipping, alpha preservation, stationary frames, live overrides/equal republishing, SDR inactivity, scaled/clipped presentation with 4x MSAA, native storage identity, invalid/stale/foreign/owner/capture guards and failure-safe Compatibility teardown. Its GPU profile measures 24 warmup submissions followed by 64 changing and 64 unchanged ForceDraw submissions with zero owner-thread managed bytes. Cold setup/readback and native/device/compositor allocation are outside that measurement. The HDR movie test enables debanding while retaining its existing exact transfer/PCM checks. Linux software Vulkan/GL profiles establish only their exercised backend behavior; physical GPUs, other platforms and human visual acceptance remain separate.

Focused commands use `ELECTRON2D_TEST_DEBANDING=managed|gpu|compatibility` with the Release test runner; optional `ELECTRON2D_DEBANDING_OUTPUT` saves disabled/enabled/scaled PNGs. Movie regression uses `ELECTRON2D_TEST_HDR=movie`.

## Native output integration

The [complete native output family](native-hdr-output.md) now executes per-window
request/support/enabled states, luminance policy, HDR presentation/transfer and
value-before-1013/signal delivery. Embedded and native child ownership, failure,
reentry and teardown use the ordinary window contract. HDR requests force effective
Window half-float storage while preserving authored UseHDR2D and independent
SubViewport policies.

The 2026-10-25 format/metadata probe remains historical evidence only. Current
controlled-metadata native rendering and independent shader pixel checks qualify
engine behavior separately from this host's actual SDR state. Actual HDR displays,
physical luminance, foreign/native-heap/performance and owner acceptance remain
open; accepted swapchain formats alone do not establish them.
