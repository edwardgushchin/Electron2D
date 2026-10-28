# Writable graphics storage images

Ordinary and independent-layer array vertex/fragment `Image` descriptors execute stores, loads, dimensions and
signed/unsigned R32 integer atomics on the original GPU texture. Existing shader,
uniform-set, pipeline and draw APIs apply; no native handle or new public switch is
introduced. Source compilation, imported SPIR-V, typed specialization and versioned
binaries share validation. Fixed descriptor arrays preserve element order and guard
invalid selection before stores or atomics.

The allocation requires `TextureUsageBits.Storage`, matching scalar class and any
explicit shader image format. `StorageAtomic` alone does not replace `Storage`.
The image role addresses mip zero of an ordinary color texture or every layer of
an independent color array, including a one-layer array. The storage view exposes
one mip; image coordinates select the layer. Higher mips retain their contents.
[Shared views](shared-texture-views.md) preserve the selected format and owner.
Multisample storage and wider/float atomics require their native capabilities. Sampling and transfer usage remain independent roles.
Storage access uses identity channels even when another sampled image has a copied
component mapping.

Each writing stage requires its independently enabled native storage feature.
Missing support rejects shader creation and binary publication, including symbolic
specialization templates. A failed binary load leaves its placeholder uninitialized.
The pinned Vulkan descriptor/layout adapter is also required; its absence does not
disable ordinary buffers or converted ordinary readonly image modules. Array image
modules, including pure readonly ones, require the version-two view adapter. Native pipeline creation
failure propagates and retires retained shader references.

## Ordering and ownership

Before each consuming draw, the pass stores active attachments and closes. The
adapter transitions all participating original images to GENERAL, snapshots any
framebuffer inputs before that transition, and starts the retained pass again.
Storage descriptors use the original base-mip view. Sampled aliases use GENERAL
descriptors while that scope is active. Closing the pass restores each image's
default layout before compute, transfers, later readonly graphics or canvas use.
Repeated views and individual writable indirect commands establish the same scope.
Copied push bytes, scissor and ordinary attachment state survive the restart.

Active color/depth/resolve output feedback rejects. Each draw's shader invocations
own their race freedom: pass boundaries do not synchronize a vertex write with a
fragment read inside that draw. No intermediate CPU copy, readback or submission
is inserted. Modules containing array storage use native storage descriptors for
both readonly and writable images. The native pipeline request carries the actual
writing-stage mask separately from descriptor kinds, so readonly arrays require
no graphics-store capability. HLSL RWTexture declarations keep write permission
even when a fixture only loads; readonly external SPIR-V supplies NonWritable.
Ordinary pure readonly modules retain their
[snapshot path](local-compute.md#readonly-graphics-image-coherence).

Mixed descriptor layouts are cached by the two stages' resource counts and image
kinds on the shared physical device. Each command owns its descriptor pools through
SDL's existing cache and resets/reuses them after completion. Layout IDs are reserved
once per successful profile, not per frame/pipeline attempt. Textures are tracked by
the command until completion or cancellation. Closing one logical device cannot
retire another lease's layouts; final physical-device retirement waits for GPU idle,
destroys the native layouts, then destroys the device. No vendored source is changed.

## Compute and array consumers

Compute `Image` bindings use the same cached full-layer/single-mip native view.
GLSL image2DArray/iimage2DArray/uimage2DArray and HLSL RWTexture2DArray retain typed
loads, stores, dimensions and signed/unsigned R32 atomics. Fixed descriptor arrays
remain separate from image layers: a guarded descriptor index selects an image,
then its three-component coordinate selects a texel and layer. Scope, memory
semantics and atomic prior values follow the existing image lowering.

A sampler may read a disjoint layer or mip of the same physical allocation. The
compute adapter binds cached image-write views and transitions each deduplicated
physical root, across every layer/mip, to GENERAL and back. This bypasses SDL’s
single-subresource image-write transition. Shared samplers use GENERAL and
independent samplers retain READ_ONLY through a replaced readonly set. SDL retains
write/uniform descriptor allocation, buffer barriers and command retirement. An older adapter rejects simultaneous
sampled/storage aliases before dispatch; the compute pass still closes on failure.
Private views are prepared at set creation, cached for dispatch, and retired before
an owned image or borrowed renderer allocation. No CPU image or snapshot is added.

`TextureArrayRD` accepts one or more layers. Storage writes reach its named canvas
sampler directly, including arrays exported from authored `TextureArray` storage.
Compatible authored updates preserve the borrowed identity; disposal/replacement
expires private views, sets and canvas metadata. Explicit snapshots remain copied
raw readbacks, and renderer recreation requires a new live binding.

## Verification

[DeviceGraphicsStorage.Consumer](../../tests/DeviceGraphicsStorage.Consumer/README.md)
checks GLSL/HLSL imported and explicit-source writes, exact original bytes, preserved
higher mip data, both writing stages, signed/unsigned atomic prior/final values,
image loads/queries, mixed buffers/uniforms/push data, sampled aliases, readonly
stage composition, typed specialization, fixed arrays and invalid selectors,
attachmentless direct/indirect ordering, vertex-only discard, repeated views, ordered input snapshots, feedback refusal, later
compute/copy/readback, and retained canvas pixels across renderer retirement.
Prepared intervals require zero managed owner-thread bytes. Feature/procedure masks
and injected pipeline failure exercise pre-publication refusal and surviving paths.

The native C check verifies stable layout IDs, mixed descriptor categories, 260
resource sets per command across three completion/reuse cycles, deduplicated texture
tracking, pass/ABI/profile guards and explicit idle cache retirement. These checks
establish Linux/Vulkan execution. Foreign backends/hardware, native heap allocation
totals and broad workload performance remain separate qualification gates.

The array profiles additionally exercise one/three layers, fixed descriptor arrays
of arrays, signed atomic values, invalid selectors, shared sampled mip layouts,
readonly GLSL and readonly HLSL-derived SPIR-V without stage-store features,
authored array updates/expiration, actual ABI-1 refusal and failure recovery.
The native array fixture observes real descriptor writes and both complete
physical-root barriers, checking every layer/mip and exact old/new layouts.

Native descriptor layouts follow [descriptor image layout semantics](https://docs.vulkan.org/refpages/latest/refpages/source/VkDescriptorImageInfo.html). Layout retirement follows [descriptor-layout destruction](https://docs.vulkan.org/refpages/latest/refpages/source/vkDestroyDescriptorSetLayout.html) and [pipeline-layout destruction](https://docs.vulkan.org/refpages/latest/refpages/source/vkDestroyPipelineLayout.html), after the engine has completed all using work.

### Array storage qualification, 2026-10-28

The source-bound cohort passes 37 native process profiles: nine new CLR and nine
strict NativeAOT profiles, nine existing image/shared-view consumers, six local
device regressions, three retained renderer/canvas/multiview regressions on the
ordinary Wayland desktop, and the native descriptor/barrier check. Vulkan runs on
RTX 3090 Ti. All 122 imported/derived/native fixture modules pass Vulkan 1.2
spirv-val; this is module validation, separate from execution. Prepared compute
and graphics intervals and the ordinary canvas's 32 active frames retain zero
managed owner bytes. Release, compiled coverage and wiki gates pass.

The temporary headless Mutter session stalls in NVIDIA WSI swapchain acquisition
for the two broad canvas/renderer regressions, with both the previous and current
bridge; a local lavapipe prerequisite does not repair that host session. Those
runs are retained as failed diagnostics. The unchanged tests pass on the ordinary
Wayland desktop. No production presentation workaround or system setting is
introduced. Whole-runtime formatting reports 24 pre-existing whitespace issues in
unchanged TextLayout.cs; all fourteen changed runtime files pass separately.
