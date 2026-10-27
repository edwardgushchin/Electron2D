# Native mirror-clamp sampling

Last updated: 2026-10-27

`RDSamplerState.RepeatU` and `RepeatV` accept `RenderingDevice.SamplerRepeatMode.MirrorClampToEdge`. Each selected axis mirrors negative normalized coordinates once, then clamps to the edge texel beyond either end. For example, -0.25 samples like 0.25, while -1.25 and 1.25 clamp to the far edge. This differs from the indefinitely alternating MirroredRepeat mode. Nearest/linear filtering, mip selection, integer image restrictions and comparison state retain their existing contracts.

The shared sampler path serves local compute/graphics bindings and copied Shader settings, including independent image/sampler descriptors and array images. Authoring and archives preserve the numeric mode without opening a device. Native creation requires the pinned Vulkan adapter and an enabled `VK_KHR_sampler_mirror_clamp_to_edge` device extension. Other backends and devices where negotiation failed throw NotSupportedException before publishing a sampler. UnnormalizedUVW still permits only ClampToEdge/ClampToBorder and rejects mirror-clamp during configuration validation.

GPU device creation requests this optional extension alongside its existing point/relaxation policy. If that attempt fails, it retries without the extension while preserving that policy; the existing fallback without sized points and the Android relaxation remain available. Only a successful Vulkan request records the private device capability. All canvas/compute leases observe that same device. The native adapter checks the capability before replacing the unpublished sampler handle, retaining SDL command references and deferred destruction. No shader rewrite, CPU sampling substitute, vendor source change or public native handle is introduced.

The extension must be enabled even when the physical driver advertises Vulkan 1.2. See the [Khronos extension contract](https://docs.vulkan.org/refpages/latest/refpages/source/VK_KHR_sampler_mirror_clamp_to_edge.html) and [sampler validity rules](https://docs.vulkan.org/spec/latest/chapters/samplers.html). This implementation follows [ADR 0028](../decisions/rendering.md#adr-0028) and retains the [ADR 0021](../decisions/product.md#adr-0021) qualification boundary.

## Executable checks

Build `tests/ShaderSamplers.Consumer` against the newly built Electron2DRender module. The `mirror` profile checks 64 configurations against actual GPU readback: float/signed/unsigned values, combined/separate/array descriptors, both U/V orientations, two mips, negative/zero/positive/far coordinates, nearest/linear filters and mixed edge/repeat/border modes. Prepared dispatch/readback intervals require zero owner-thread managed bytes. `mirror-canvas` checks 54 GLSL/HLSL copied-state drawable blits and native pixel snapshots in an Engine window lifecycle with window presentation disabled. The ordinary authoring profile checks copied and archived mirror-clamp state. These are Linux/Vulkan checks, separate from physical display and foreign backend acceptance.

```sh
dotnet tests/ShaderSamplers.Consumer/bin/Release/net10.0/linux-x64/ShaderSamplers.Consumer.dll mirror
dotnet tests/ShaderSamplers.Consumer/bin/Release/net10.0/linux-x64/ShaderSamplers.Consumer.dll mirror-canvas
```

Repeat with strict full-trim NativeAOT. The test-only Linux `tools/render-native/test_mirror_mask.c` uses SDL3_DYNAMIC_API and the pinned `SDL_dynapi_procs.h` to refuse only creation attempts requesting the extension. Run the same profiles with `unavailable`: mirror-clamp must reject, ordinary native sampling must still produce the expected pixels, and a failed high-level preparation must recover after replacing its sampler policy. Require the shim's `MASKED native mirror-clamp extension request` output; a passing fallback test without that output is insufficient evidence. The shim delegates every other SDL operation and is excluded from production packages.

Compile it against the same pinned SDL source tree used by the native producer:

```sh
cc -shared -fPIC -Wall -Wextra -Werror -I "$SDL_SOURCE/include" -I "$SDL_SOURCE/src/dynapi" tools/render-native/test_mirror_mask.c -ldl -o /tmp/libMirrorMask.so
SDL3_DYNAMIC_API=/tmp/libMirrorMask.so dotnet tests/ShaderSamplers.Consumer/bin/Release/net10.0/linux-x64/ShaderSamplers.Consumer.dll mirror unavailable
SDL3_DYNAMIC_API=/tmp/libMirrorMask.so dotnet tests/ShaderSamplers.Consumer/bin/Release/net10.0/linux-x64/ShaderSamplers.Consumer.dll mirror-canvas unavailable
```

`Electron2DRenderCheck` checks raw native rejection on a device where the capability was not negotiated. `ELECTRON2D_TEST_SAMPLER_CONFIGURATION=1` checks validation, placeholder projection and foreign-driver rejection without a GPU. Border, texel-coordinate, device-texture and sized-point consumers retain their own regression checks. Native allocator totals, performance across hardware, physical display presentation and other platforms remain unqualified by these tests.
