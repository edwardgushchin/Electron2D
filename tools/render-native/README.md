# Private render allocator and sampler bridge

This producer compiles the allocator/sampler declarations of SDL 3.4.18 from the archive
SHA-256 pinned in CMakeLists.txt. It does not build or replace SDL. The selected
target's SDL core is supplied through SDL3_LIBRARIES. The generated declarations
retain SDL's notice; the platform package includes licence/SDL-LICENSE.txt.

e2d_render_memory preserves ABI 1 and returns 1 for a coherent native snapshot, 0 for
an unsupported core/revision/driver, and -1 for invalid arguments or inconsistent
allocator data. Output contains three uint64 fields: textures, buffers and total.
The current native adapter is Vulkan. It locks the native allocator without
submitting, waiting for GPU work or invoking application callbacks.

Build Electron2DRender for delivery and Electron2DRenderCheck for the native
concurrency/allocation check. Run test_generate_layout.py with Python -B for
bounded extraction and modified-source rejection. Runtime acceptance requires
the selected matching core; a successful compilation alone does not qualify it.

`e2d_render_sampler` uses ABI 1, copies the SDL sampler fields, normalized
U/V address identities (0..4), border color (0..5), and a zero/one unnormalized
flag. It validates the pinned Vulkan core, physical LOD-bias bounds and the
unnormalized filter/mip/LOD/address/anisotropy/compare restrictions. Ordinary
normalized samplers without border/mirror-clamp use SDL creation directly. Extended states replace the
unpublished Vulkan handle while SDL retains command references and deferred
release. Failure returns null with SDL_GetError.

`e2d_render_sampler_view` uses ABI 1 and the pinned TextureCommonHeader. It
returns 1 only for an ordinary single-layer/single-level/single-sample native
texture, 0 for an incompatible view and -1 for an invalid/unsupported call.
It records no commands and invokes no application callbacks. The managed
binding owner validates shader operations and lifetime before native sampling.

The public sampler consumers check real normalized and texel-coordinate
float/integer/depth pixels, descriptor arrays, mixed/filtering modes and
operation/view rejection. Mirror-clamp requires the private device capability
recorded only after successful VK_KHR_sampler_mirror_clamp_to_edge negotiation.
The factory retries creation without this optional extension on failure, retaining
the existing point and relaxation policy. Raw samplers without that capability
reject mirror-clamp; unnormalized coordinates still allow only addresses 2/3.
The Linux test_mirror_mask.c SDL3_DYNAMIC_API shim refuses extension requests
through the pinned dynapi table, leaving all ordinary native operations intact.
It is test-only and is not linked into this producer.

`e2d_render_limits` uses ABI 1 and exactly 37 uint64 output elements. It reads the immutable selected Vulkan physical-device limits without allocation, submission or waiting. Invalid pointers/count/ABI return -1; unsupported core/revision/driver returns 0; success returns 1. Failure leaves output untouched. Slots 11/13/14 are zero for excluded image roles. Managed code applies the executing profile and logical descriptor remapping; packages must carry this export with the existing three exports.

The Linux test_fp64_mask.c SDL3_DYNAMIC_API shim refuses shaderFloat64 feature requests while delegating ordinary device creation. DeviceFP64.Consumer unavailable verifies explicit FP64 refusal and executing float32 fallback. It is a development fixture, never packaged with the runtime. See the [FP64 contract](../../docs/components/local-compute.md#native-fp64-arithmetic-and-data).

The Linux test_int64_mask.c development shim selects int64/fp64/both through ELECTRON2D_TEST_MASK_PRECISION. DeviceInt64.Consumer verifies independent fallback and executing remaining precision; the fixture is excluded from runtime packaging.

The Linux test_atomic64_mask.c development shim selects buffer/shared/both/api through ELECTRON2D_TEST_MASK_ATOMIC64. DeviceAtomic64.Consumer checks independent storage fallback and rejection before shader/binary publication. The api profile refuses Vulkan 1.2 requests. This shim is excluded from runtime packaging.

`e2d_render_shader16_support` preserves ABI 1 and returns the selected Vulkan 1.2 physical device's supported buffer/uniform/push/input-output storage bits (0..3) and Float16 arithmetic bit (4). ABI 2 additionally returns core Int16 arithmetic in bit 5; ABI 1 remains unchanged. ABI 3 additionally returns Int8 arithmetic in bit 6 and byte buffer/uniform/push storage in bits 7/8/9; the ABI 1/2 masks are preserved. Zero means no supported narrow profile; invalid arguments return -1. The mask describes support before optional device creation, not enabled device features. The managed factory retires the unpublished probe before creating a final device and retains the accepted mask privately. The export is included in all three export lists and the native package closure.

The Linux test_shader16_mask.c development shim selects integer/arithmetic/buffer/uniform/push/io/all/api through ELECTRON2D_TEST_MASK_SHADER16. DeviceFP16.Consumer and DeviceInt16.Consumer verify retained sibling profiles, original-module/binary refusal and legacy HLSL. Never package this shim.

The private narrow feature property retains only successfully enabled features. The Linux test_shader8_mask.c fixture refuses byte arithmetic/buffer/uniform/push, Int16/Float16 arithmetic, all byte features or Vulkan 1.2. DeviceInt8.Consumer checks available siblings and pre-publication failures; never package the shim.
