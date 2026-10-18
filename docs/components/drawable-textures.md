# Writable textures

Last updated: 2026-10-18

[DrawableTexture](../classes/DrawableTexture.md) supplies sampled color attachments written by ordered GPU graphics passes. [BlitMaterial](../classes/BlitMaterial.md) supplies five built-in blend operations. [Shader](../classes/Shader.md) selects `Mode.TextureBlit` for custom HLSL/GLSL fragments; the existing [ShaderMaterial](../classes/ShaderMaterial.md) supplies typed values, defaults and registered globals.

## Executing contract

A blit writes one mip level on one through four distinct targets. Actual dimensions, storage format and mip allocation must match. The destination rectangle may extend outside the target; raster clipping keeps the original source-coordinate mapping. Negative rectangle sizes flip sampling. Empty rectangles validate but write no pixels. All source/target alias checks finish before a render pass, including proxies, channel views, device exports and material textures. Actual native image scalar type and array shape must match each shader descriptor; built-in source sampling requires float images. Material atlas parameters sample their full backing image, retaining the ordinary shader policy. Source preparation cannot mutate/dispose the targets or recursively issue another blit.

Formats are normalized RGBA8, sRGB RGBA8, half-float RGBA and float RGBA. sRGB native sampling and attachments perform their hardware color conversion; float formats retain HDR channel values. Setup and format/mip setters replace storage, clearing all allocated levels to the authored color. Blits never regenerate lower levels implicitly. `GenerateMIPMaps` regenerates them from level zero.

Setup is legal before startup. Native creation occurs on first GPU use in each session. Canvas, material, atlas/channel and logical proxy consumers sample native pixels directly. Explicit GetImage synchronizes and copies all levels. There is no per-frame CPU pixel replay. Compatibility rendering rejects this GPU-only family. Setup and typed serialization do not require a renderer. CopyFromResource and ResetState discard native results and restore authored clear storage on next use. Allocation replacement guards native readback callbacks against reentrant mutation/disposal. Callback failures after retirement leave valid authored setup; failed owned transfer does not consume its source.

## Compiled blit programs

Compile with `ShaderImport source.glsl fragment output.spv --texture-blit`, or use `Profile="texture-blit"` on `Electron2DShader` project items. Load with `Shader.CreateFromSPIRV(bytes, Shader.Mode.TextureBlit)`. Replacement accepts the same explicit domain. The resource archive preserves domain and bytecode; raw GetSPIRV exports the module and requires the same explicit domain on reimport.

Fragment inputs are optional float4 MODULATE at location zero and float2 UV at one. Outputs are contiguous float4 locations starting at zero, with the exact supplied target count. Scalar float samplers SOURCE_TEXTURE0 through SOURCE_TEXTURE3 select the call's sources at their declared contiguous bindings. These names are renderer-owned in this domain and absent from material descriptors. Remaining uniforms, images, arrays, numeric matrices, defaults, globals and TIME retain the existing typed material contract. Blit programs have no authored vertex, instance, screen or scene-light interface. The built-in vertex derives a rectangle from vertex index without vertex-buffer allocation. Custom material blending uses ordinary mixing.

## Server and native lifetime

RenderingServer.TextureDrawableCreate returns an owned canvas RID. TextureDrawableBlitRect accepts owned or resource-borrowed target RIDs; TextureDrawableGenerateMIPMaps operates on the same resource. TextureDrawableGetDefaultMaterial retains one session material and recreates it after explicit FreeRID. FreeRID and session shutdown release owned resources. Matching drawable TextureReplace transfers storage and redirects logical aliases; source replacement identities expire. Size overrides change only logical display size. Paths retain ordinary server metadata.

TextureGetRDTexture exports a borrowed native allocation wrapper. Freeing the wrapper preserves producer storage. Setup, storage transfer or source disposal expires previous exports; old native allocations remain leased until wrappers retire. Materials and sources remain borrowed, and a sampled native alias can never also be an output of the same pass.

## Verification

DrawableTextureTests passes managed authoring, typed archives with fresh-process load, graph/local copies, all four native formats including unclamped HDR and encoded sRGB readback, default one/two/three/four-target passes, authored GLSL/HLSL MRT, mip writes/generation/readback, five blends, clipping/flips, aliases, native export ownership, source callbacks, copied/reset storage, failed callback cleanup, lazy first canvas use and a later renderer session. Linux Wayland software Vulkan verifies actual offscreen and canvas/RID pixels, with 64 active and 64 retained frames at zero managed bytes. Compatibility rejection and related mesh, matrix and owned-canvas regressions pass. Native allocations, hardware throughput, foreign platforms and visual owner acceptance remain unverified.
