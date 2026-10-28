# Viewport mipmap bias

Last updated: 2026-10-28

Viewport.TextureMIPMapBias controls the native level-of-detail offset of the next canvas image. Zero preserves sampler selection, negative values retain finer mips and positive values select coarser mips. The property is stored, copied and restored through ordinary scene files on Window and SubViewport. The root window samples ProjectSettings.TextureMIPMapBias at Engine.Run startup unless assigned explicitly. Other viewports begin at zero; changes to project settings do not mutate live viewports.

RenderingServer.ViewportSetTextureMIPMapBias(RID viewport, float bias) supplies the same next-image policy for a live scene or caller-owned viewport. Its session override preserves the authored property and packed values. Every valid scene assignment, including an equal value, republishes authored state. Target texture identity and native target storage remain stable. RID/session, owner-thread, capture/submission and disposal guards follow the existing viewport owner.

The offset affects implicit mip selection for ordinary canvas images, normal/specular channels, imported material image policies, screen mip sampling and normalized explicit material samplers. Both vertex and fragment material bindings use the common sampler preparation path. Explicit sampler LODBias is added to the viewport offset. Base-level filtering and unnormalized sampling retain their authored policy. Explicit shader LOD/fetch operations retain their native semantics. Independent RenderingDevice and particle-process sampling retain their own configuration. Resource getters, texture data and authored sampler descriptions are unchanged.

All authored values must be finite. Compatibility and Metal reject nonzero viewport offsets before replacing targets; Metal requires a shader-applied bias adapter. Vulkan validates the combined native offset against maxSamplerLodBias and refuses unsupported values before command submission. A finite authored value does not imply native support. Normalized explicit samplers retain their existing native restrictions. Missing textures and image-array/scalar roles retain their existing validation and fallback ownership.

Canvas samplers now use the existing ShaderSamplerCache: configurations used in the current and preceding frame remain available, then unused configurations retire. Alternating viewport policies reuse native objects after preparation; continually changing floating-point values do not retain every historical sampler. Repeated active and idle canvas work remains subject to ADR 0014. Native allocations during new configuration creation and native heap profiling are separate from managed owner-thread measurements.

## Example

Fragment using an existing live viewport:

```csharp
viewport.TextureMIPMapBias = -0.5f;
// A renderer override affects the next image and preserves the scene property.
RenderingServer.ViewportSetTextureMIPMapBias(viewport.GetViewportRID(), 0.5f);
viewport.TextureMIPMapBias = viewport.TextureMIPMapBias; // Republish authored state.
```

## Verification

The focused runner is ELECTRON2D_TEST_VIEWPORT_MIP_BIAS=managed|gpu|gpu-hlsl|compatibility|limit. Independent colored native mip levels distinguish real selection from stored configuration. The tests exercise root startup, nested-view isolation, additive fixed-array material samplers, fractional trilinear filtering, base-level filtering, equal scene republishing, stable target identity, validation, scene files, owner guards, unsupported backend cleanup and native limit refusal. Executed receipts and measured limits are recorded in bin/viewport-mipmap-bias/2026-10-28. Foreign platforms, native heap and human visual acceptance remain separate checks.

Linux Wayland/Vulkan passes both GLSL and HLSL native profiles, each including 64 active and 64 idle prepared frames with 0/0 managed owner-thread bytes. The managed state/file checks, compatibility and native-limit refusal profiles also pass. Caller-owned and nested viewports retain independent pixels; capture mutation and retired RID checks pass. The capture guard was observed failing before the property reused the server guard, then passed after correction. Existing sampler configuration/arrays, MSAA, HDR, masks and managed sampling/lifecycle checks pass. No cross-platform or native-heap qualification is inferred.
