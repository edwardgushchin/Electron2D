# Logical color operations

`RDPipelineColorBlendState.EnableLogicOp` enables a copied pipeline-wide
`RenderingDevice.LogicOperation`, defaulting to disabled and Clear. All sixteen
operations execute on encoded source/destination component bits of signed/unsigned
integer and normalized color targets. Component complements use the actual format
width. Per-attachment write masks apply after the operation.

Enabling logic disables arithmetic blending for every attachment, even if that
attachment has EnableBlend set. Floating-point and sRGB attachments receive the
unblended source without a logical operation; sRGB encoding still applies. Disabled
logic preserves the existing arithmetic blend contract, including integer-target
rejection. Unknown, negative and exclusive Max values reject at assignment. Typed
property metadata preserves both defaults and reversion. Created pipelines and
private component/view variants retain copied values after description mutation.

## Native feature and composition

The pinned Vulkan adapter queries physical logicOp, and the factory independently
negotiates its logical-device ownership. The five optional raster bits have at most
32 combinations within the current precision/storage profile. Refusing logic never
displaces that profile or disables ordinary graphics. Enabled operations require
both native feature ownership and the pipeline adapter, including Copy and NoOp;
unsupported requests reject before pipeline publication. Native creation failures
propagate and release retained shader references. Disabled state requires neither
feature nor adapter.

The call-local pipeline adapter copies native color state while preserving its
attachment descriptions. It composes with native storage images, component maps,
per-face stencil/depth bounds, multisample masks/shading, ordered views/subpasses
and scissor-driven pass restarts. Shared callbacks remain unchanged and no CPU
framebuffer emulation or extra submission is inserted. The existing device owns
and retires native pipelines and shader variants. No vendored source is changed.

## Verification

[DeviceGraphicsStorage.Consumer](../../tests/DeviceGraphicsStorage.Consumer/README.md)
checks imported and explicit-source GLSL/HLSL truth tables for all sixteen operations
with RGBA8 signed/unsigned/normalized, RGBA32 signed/unsigned, float and sRGB targets.
It checks copied state, mixed integer/normalized MRT masks, component variants,
image atomics, independent depth/stencil faces and bounds, repeated views, ordered
passes/scissor, four-sample masks/shading and exact resolves. Retained canvas checks
consume XOR pixels after freeing the producer pipeline and recreate the renderer.
Prepared draw/readback intervals require zero owner-thread managed bytes.

Query and device-creation masks independently refuse logic; missing adapter and
injected native pipeline failure exercise every operation while disabled graphics
continues to execute. The native C check covers invalid ABI, null and operation
bounds alongside existing reference/cache retirement checks. These checks establish
Linux/Vulkan execution. Foreign backends/hardware, native heap totals and broad
workload performance remain separate qualification gates.

The format behavior and arithmetic suppression follow the Vulkan
[logical operation contract](https://docs.vulkan.org/spec/latest/chapters/framebuffer.html#framebuffer-logicop)
and [color blend state](https://docs.vulkan.org/refpages/latest/refpages/source/VkPipelineColorBlendStateCreateInfo.html).
