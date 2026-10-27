# Effective device limits

Last updated: 2026-10-27

[RenderingDevice.LimitGet](../classes/RenderingDevice.md#limitget) exposes immutable native resource and execution capacities through [RenderingDevice.Limit](../classes/RenderingDevice.Limit.md) under [ADR 0028](../decisions/rendering.md#adr-0028). Applications can size local workgroups, tile large dispatches, choose image extents and construct vertex/framebuffer layouts before recording work. The query retains ordinary owner-thread, callback and disposal guards and requires no submit or GPU wait. A device caches its first complete snapshot; prepared reads allocate no managed memory.

```csharp
using var device = RenderingServer.CreateLocalRenderingDevice();
var localX = Math.Min(64, device.LimitGet(RenderingDevice.Limit.MaxComputeWorkgroupSizeX));
localX = Math.Min(localX, device.LimitGet(RenderingDevice.Limit.MaxComputeWorkgroupInvocations));
```

## Native and executing profile

The private source/hash/version/revision-bound SDL 3.4.18 Vulkan adapter reads VkPhysicalDeviceLimits from the selected retained physical device. Its ABI-one output is a fixed 37-element uint64 table; invalid ABI, size, pointers or unsupported cores/drivers reject before opaque access and leave output untouched. The public table omits excluded one-dimensional, volume and cube resource roles. MetalFX temporal scale selectors remain blocked on their actual scaler/texture consumer and supported scale-range producer.

These are effective maxima for the executing Electron2D profile, subject to simultaneous-resource, format/role and memory constraints. They are not raw hardware advertising: sampled images/samplers retain sixteen paired stage slots, storage buffers and image categories retain eight, uniform data four, vertex attributes/bindings sixteen, color targets eight, uniform bytes 16 KiB and copied push bytes 128. Native smaller limits clip the relevant profile. Vertex stride retains its 2048-byte boundary; attribute offsets must also fit their format and stride. Direct dispatch retains 65535 groups per axis and workgroup dimensions/invocations retain their 1024 profile ceiling. Native per-axis limits can be smaller; the product of local dimensions must fit the invocation count.

Uniform data is copied into native command storage. MinUniformBufferOffsetAlignment describes that native suballocation alignment; public uniform RIDs start at zero and expose no native buffer offset. Lowered push data consumes a uniform slot. Samplerless image fetch may consume an internal sampler slot, and formatted texel buffers consume storage-buffer capacity. Readonly image/input adapters and generated bindings retain their native category constraints.

## Logical descriptors

Application set numbers stay sparse and are remapped to private native sets. MaxBoundUniformSets limits the number of populated logical sets required by a program, not the highest numeric set ID. Its envelope is the sum of effective logical descriptor category budgets; one populated set requires at least one descriptor. An authored module at set 1000 remains executable after remapping even when the native hardware exposes fewer descriptor sets.

Per-set maxima span both graphics stages and are bounded by native combined descriptor capacities; per-stage budgets apply independently. Shared logical resources can require separate native stage slots. These category maxima cannot all be assumed independently additive in an arbitrary program: pair expansion, lowered push data and simultaneous native resources retain their validation. Compilation checks the resolved specialized workgroup and actual stage categories before native program creation. Source-only validation remains independent of the current device.

## Boundaries and responsibility

Texture creation checks ordinary dimensions and array layers before transfer-size calculation/native allocation. Framebuffer creation checks dimensions and active color targets; draw-list creation checks viewport extents. Vertex creation checks the native attribute, binding, stride and offset bounds. Uniform creation and direct dispatch check their effective capacities. Failure does not publish a resource or record invalid native work. Undefined selectors reject rather than returning zero.

Native maxima do not promise sufficient free memory or every texture format/usage combination. Depth initialization retains its separate 256-layer bound. Shader authors own shared-memory footprint and GPU-authored indirect group counts/index values; the device does not add synchronous readback to validate them. Existing semantic/compiler/native failures remain explicit. Other native drivers have no limit-query adapter yet and reject this query; their existing runtime validation stays active.

## Executable verification

The public `tests/DeviceLimits.Consumer` program queries all 34 selectors and uses the results for real native workgroup specialization, shared-array arithmetic, maximum-axis direct dispatch, ordinary texture extent and array-layer readback, and sixteen-binding/eight-target graphics output. It checks invalid dimensions/selectors, sparse-set remapping, copied native identity, wrong-thread/disposed access and prepared query allocation. Its graphics fixture explicitly requires the sixteen-binding/eight-color native profile.

The native check repeats immutable snapshots, rejects malformed arguments and unsupported-core access, and retains its existing allocator/sampler controls. Matching packages must carry e2d_render_limits together with the existing memory/sampler exports. Linux CLR/NativeAOT, selected native execution, physical hardware, native heap costs, foreign platforms, window presentation and owner acceptance are separate evidence categories.

Native field semantics: [VkPhysicalDeviceLimits](https://docs.vulkan.org/refpages/latest/refpages/source/VkPhysicalDeviceLimits.html); resource slots/remapping: [SDL compute pipelines](https://wiki.libsdl.org/SDL3/SDL_CreateGPUComputePipeline).
