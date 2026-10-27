# Native format filter support

Last updated: 2026-10-27

[RenderingDevice.SamplerIsFormatSupportedForFilter](../classes/RenderingDevice.md#samplerisformatsupportedforfilter) lets an application choose an executable nearest or linear filter before binding an image. [ADR 0028](../decisions/rendering.md#adr-0028) owns the backend contract.

## Query and lifetime

The pinned SDL 3.4.18 Vulkan adapter reads optimal-tiling sampled-image and ordinary linear-filter features for each mapped native format. It also checks SDL sampling usage; depth formats include their required depth-attachment role. Nearest requires sampled-image support. Linear additionally requires the native linear feature. Defined but unmapped or unsupported public formats return false. Undefined formats, Max and invalid filters reject. Owner-thread, callback and disposed-device guards remain active.

One immutable table belongs to the shared physical device. Local devices and a renderer lease reuse it until the last lease retires; a new physical device gets a new table. A prepared public query allocates no managed memory, creates no image and submits or waits for no GPU work. A positive result promises neither available memory nor all usage, sample-count or view combinations. Other native cores/drivers reject this query explicitly.

## Executing bindings

Combined and independently linked device samplers, GPU drawable blits and explicitly configured Shader samplers in canvas/particles validate the actual native texture format before consumption. Minification, magnification and mip interpolation each require ordinary linear support when comparison is disabled. Anisotropy with nearest filters does not by itself require that feature; the separate integer-sampler restrictions remain active. Depth comparison retains its own role and comparison-filter contract, so an absent ordinary linear feature does not reject a legal comparison sampler.

Samplerless fetch/query operations ignore configured filters. Existing default canvas filter policies retain their behavior; this validation covers explicit Shader sampler configurations. The native texture header determines the real allocation format, including converted uploads, borrowed renderer images and screen images. State is copied when authored/created; later description mutation/disposal cannot change the effective filter. Particle sampler/view validation precedes opening its authored compute pass. Failed binding never publishes invalid native work and callers can select nearest and continue. The adapter keeps existing SDL ownership and adds no public backend handles or runtime switch.

## Executable checks

`tests/ShaderSamplers.Consumer` selectors `formats` and `formats-canvas` exercise all public formats, shared-device lifetime, malformed/thread/disposed queries, prepared allocations and real native compute/canvas/blit/particle pixels. Distinct min/mag/mip cases verify copied state. Existing border/depth, coordinate, authored sampling and canvas consumers cover the connected binding paths.

`tools/render-native/test_filter_mask.c` is a separate test-only Linux preload shim. Build it with `cc -shared -fPIC -Wall -Wextra -Werror -I<SDL-source>/include -I<SDL-source>/src/video/khronos tools/render-native/test_filter_mask.c -ldl -o <output>/mask-linear.so`, using the pinned SDL source. Set `LD_PRELOAD=<output>/mask-linear.so` only on the consumer child. It masks ordinary linear support for RGBA32 float and D32 float while retaining sampled support. Run both selectors with `masked` only in that controlled child. It verifies min/mag/mip rejection, nearest recovery, allowed nearest anisotropy and depth comparison, plus renderer teardown/recreation. Do not preload the shim into the compositor. The native check independently compares selected explicit Vulkan formats and allocated native texture headers, repeats snapshots and rejects malformed or unsupported-core calls without overwriting output. Run native layout and package producer tests after ABI changes. Matching bundles must export e2d_render_filter_support and e2d_render_texture_format together with memory, sampler, sampler_view and limits.

Linux CLR/NativeAOT, private-compositor rendered pixels, physical GPU execution, foreign platforms, native heap measurement, window presentation and owner acceptance are separate evidence categories.

Native feature semantics: [VkFormatFeatureFlagBits](https://docs.vulkan.org/refpages/latest/refpages/source/VkFormatFeatureFlagBits.html); comparison conditions: [Vulkan draw validity](https://docs.vulkan.org/refpages/latest/refpages/source/vkCmdDraw.html).
