# Writable graphics storage images

Ordinary vertex/fragment `Image` descriptors execute stores, loads, dimensions and
signed/unsigned R32 integer atomics on the original GPU texture. Existing shader,
uniform-set, pipeline and draw APIs apply; no native handle or new public switch is
introduced. Source compilation, imported SPIR-V, typed specialization and versioned
binaries share validation. Fixed descriptor arrays preserve element order and guard
invalid selection before stores or atomics.

The allocation requires `TextureUsageBits.Storage`, matching scalar class and any
explicit shader image format. `StorageAtomic` alone does not replace `Storage`.
The current image role addresses mip zero of an ordinary color texture. Higher mips
retain their contents. Layered/multisample storage and alternate shared image views
still require their subresource-view adapters; wider/float atomics require their
native atomic capabilities. Sampling and transfer usage remain independent roles.
Storage access uses identity channels even when another sampled image has a copied
component mapping.

Each writing stage requires its independently enabled native storage feature.
Missing support rejects shader creation and binary publication, including symbolic
specialization templates. A failed binary load leaves its placeholder uninitialized.
The pinned Vulkan descriptor/layout adapter is also required; its absence does not
disable ordinary buffers or pure readonly image modules. Native pipeline creation
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
is inserted. Pure readonly stages retain the existing fetch conversion, and pure
readonly programs retain their [snapshot path](local-compute.md#readonly-graphics-image-coherence).

Mixed descriptor layouts are cached by the two stages' resource counts and image
kinds on the shared physical device. Each command owns its descriptor pools through
SDL's existing cache and resets/reuses them after completion. Layout IDs are reserved
once per successful profile, not per frame/pipeline attempt. Textures are tracked by
the command until completion or cancellation. Closing one logical device cannot
retire another lease's layouts; final physical-device retirement waits for GPU idle,
destroys the native layouts, then destroys the device. No vendored source is changed.

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

Native descriptor layouts follow [descriptor image layout semantics](https://docs.vulkan.org/refpages/latest/refpages/source/VkDescriptorImageInfo.html). Layout retirement follows [descriptor-layout destruction](https://docs.vulkan.org/refpages/latest/refpages/source/vkDestroyDescriptorSetLayout.html) and [pipeline-layout destruction](https://docs.vulkan.org/refpages/latest/refpages/source/vkDestroyPipelineLayout.html), after the engine has completed all using work.
