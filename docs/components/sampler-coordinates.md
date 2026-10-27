# Sampler texel coordinates

Last updated: 2026-10-27

[RDSamplerState.UnnormalizedUVW](../classes/RDSamplerState.md#unnormalized-coordinates) switches floating-point sampling coordinates from normalized positions to texels under [ADR 0028](../decisions/rendering.md#adr-0028). Texel centers are at half-integers: (0.5, 0.5) selects the first texel and (1.5, 0.5) the next column. Coordinate mode is copied into RenderingDevice samplers, Shader settings, independent getter copies and shader archives. It does not silently rewrite other state fields.

```csharp
using var state = new RDSamplerState
{
    UnnormalizedUVW = true,
    MaxLOD = 0
};
```

## Sampler and view contract

Creation/authoring require equal min/mag filters, nearest mip selection, zero MinLOD and MaxLOD, clamp-to-edge or clamp-to-border U/V, disabled effective anisotropy and disabled comparison. A limit of one still disables anisotropy even when its authoring flag is true. Existing nearest-only integer image rules and floating-point/integer border-color roles apply. Linear filtering interpolates in texel coordinates, including the half-texel border. Repeat/mirror modes and mismatched filters reject atomically.

When the shader uses the sampler, the actual bound native view must be ordinary two-dimensional, single-layer, single-mip and single-sample. A 2D-array allocation with one layer is still an array view and rejects. Descriptor arrays of independent ordinary images retain their existing support. The pinned TextureCommonHeader declaration validates the actual native texture rather than inferring its mip count from a logical resource. Combined bindings validate at set creation; separately linked or expanded resources validate before dispatch/draw. Copied Shader consumers validate prepared and renderer-owned images, GPU drawable sources and particle image overrides. Failed blit validation leaves the previous source/configuration usable.

The private pinned Vulkan adapter creates and publishes the sampler through retained SDL ownership, with unnormalizedCoordinates enabled on the unpublished Vulkan handle. Native argument guards repeat the state restrictions. Authored Vulkan sampler creation also rejects LOD bias outside the actual device bound before creating an invalid native object. Other drivers explicitly reject coordinate mode. This adds no public backend handles or vendored implementation edits.

## Shader operations

Use explicit-LOD or explicit-gradient sampling, samplerless texel fetch, ordinary gather without offsets, and supported image queries. Implicit sample LOD, projection, depth comparison and image-operation bias/offset operands reject. Sampler LOD bounds are zero; the image view exposes only its base level. The restrictions follow [VkSamplerCreateInfo](https://docs.vulkan.org/refpages/latest/refpages/source/VkSamplerCreateInfo.html), [the sampling operations](https://docs.vulkan.org/spec/latest/chapters/textures.html) and [Vulkan draw validity](https://docs.vulkan.org/refpages/latest/refpages/source/vkCmdDrawIndirect.html).

Samplerless fetch and size/level queries ignore the configured sampler, retaining their ordinary explicit mip and array-view behavior. The existing sampled-pair analysis propagates requirements through opaque arguments/returns, helper calls and control flow. Validation applies to reachable operations of each statically linked sampled binding; dynamic descriptor selection retains the existing conservative pair expansion. Offsets hidden in a helper cannot bypass validation. Paired canvas stages merge their requirements. Reload retires an incompatible coordinate setting, and retained older program snapshots never inherit an unsafe new setting.

Native material, lighting, glyph and component-map composition can derive another module. Canvas preparation therefore checks the final derived fragment/vertex modules as well, using weakly cached requirements. For example, implicit glyph sampling introduced around an explicit user fragment is rejected before native drawing. Actual view validation still runs for the final bound engine texture.

## Storage and lifetime

Metadata version three stores the flag in the existing sampler flags word. Versions one/two read normalized coordinates; version one also uses the original border-color default. Old versions reject the new flag before replacing the existing table. Invalid/truncated/duplicate input remains atomic. Descriptions remain caller-owned ElectronObject values, sampler RIDs retain device/thread ownership, and native cached configurations use the existing two-epoch retirement. No new service lifetime or runtime source compiler is introduced.

## Executable checks

The public `tests/ShaderSamplers.Consumer` selectors `coordinates`, `coordinates-canvas` and `coordinates-glyph` exercise real native texel positions, float/signed/unsigned formats, nearest/linear filtering, border/edge behavior, independent samplers and descriptor arrays; gather/gradient/fetch/query operations; copied state and prepared allocation; actual mip/array view rejection; shader metadata and archives; paired vertex/fragment drawing and derived glyph rejection. The `ELECTRON2D_TEST_SAMPLER_CONFIGURATION=1` root cohort covers descriptors, every restricted state field, effective anisotropy and unsupported drivers.

Run the native layout/check and package producer tests after changing the private ABI. Matching platform packages must carry e2d_render_memory, e2d_render_sampler and e2d_render_sampler_view together. Runtime qualification, NativeAOT, window presentation, native allocator measurements, other platforms and owner acceptance are separate evidence gates.
