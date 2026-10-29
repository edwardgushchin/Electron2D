# Native FP32 shader atomics

The existing RenderingDevice shader, StorageBuffer and Image families execute
native FP32 atomic load, store, exchange and addition. This supports parallel
floating accumulation and reductions without moving their counters to the CPU.
No public type or signature is added. [ADR 0028](../decisions/rendering.md#adr-0028)
owns the existing source/import/runtime boundary.

## Features and publication

The pinned Vulkan 1.2 device profile queries VK_EXT_shader_atomic_float and six independent
physical features: base operations and addition for storage buffers, workgroup
memory and images. Device creation explicitly requests the extension and matching
feature chain. Only a successfully created logical device records those enabled
bits. An unavailable export or refused optional request preserves ordinary device
creation and its existing precision/storage profile.

Every module, including specialization templates and binary imports, classifies
actual typed atomic pointers before publishing a shader or binary. Buffer storage
includes both logical encodings and physical-storage buffer addresses; shared
storage uses its independent feature, and image pointers require the image feature.
AtomicFloat32AddEXT requires its declared extension/capability. Even an unused
addition capability requires an enabled addition feature. Missing operands,
inconsistent floating result types and missing declarations reject before native
pipeline creation. Unsupported floating precision, vector or min/max capabilities
reject explicitly.

TextureIsFormatSupportedForUsage accepts StorageAtomic with R32SFLOAT only when
at least one native image atomic feature is enabled and the ordinary native format
and usage query succeeds. The concrete shader still requires its specific base/add
feature. StorageAtomic is a capability role; an Image descriptor independently
requires Storage. Existing ordinary R32SFLOAT Storage allocations may execute
supported atomics too. SupportsImageAtomic32Bit retains its existing signed/unsigned
integer capability meaning; query the concrete floating format/usage separately.

## Execution and ownership

GLSL GL_EXT_shader_atomic_float and imported SPIR-V preserve the native operation,
returned prior value, Device/Workgroup scope, memory semantics and coherent/volatile
qualifiers. Atomic load/store with explicit acquire/release semantics uses
GL_KHR_memory_scope_semantics. The current HLSL frontend's floating-interlocked
source vocabulary is not qualified by these GLSL/SPIR-V checks.

Scalar and fixed-array Image descriptors execute over ordinary and independent-layer
R32SFLOAT images, including available multisample images. Descriptor lowering chooses
the native image before creating the texel pointer and performing the atomic; it
preserves the coordinate/sample and scope/memory operands. Invalid descriptor
selectors return zero or suppress a store. Descriptor bounds adaptation does not
make invalid texel coordinates valid. The common helper keeps the original constant for ordinary images instead of
turning it into a function parameter; multisample helpers keep their selected sample.
Ordinary image pointers retain a constant-zero
sample operand. Original exported modules remain unchanged.

Compute and independently enabled writable vertex/fragment stages share the existing
GPU timeline. Completed draw/dispatch boundaries order subsequent work and readback.
Within a draw/dispatch, the algorithm owns synchronization and race freedom for
unrelated data and workgroups. Native FP32 arithmetic retains device rounding,
NaN/denormal behavior and order-dependent addition; arbitrary reduction order is
not a reproducible summation guarantee. Exchange preserves the transferred scalar
bits. Physical pointers retain the [address ownership contract](buffer-device-addresses.md).
FreeRID invalidates dependent sets and retires pending native storage normally.

FP16/FP64, floating min/max, vector atomics, sparse images and encoded ImageBuffer
floating atomics retain separate native precision/resource adapters. Integer
ImageBuffer operations remain available. This slice does not add those adapters.

## Verification

[FloatAtomic32.Consumer](../../tests/FloatAtomic32.Consumer/README.md) uses only the
public API. It verifies 64 contending buffer/shared/image contributions and unique
prior values, NaN payload and negative-zero exchange bits, sequential dispatches,
ordinary/fixed descriptors, independent layers and four samples, invalid descriptor
selection, explicit scoped load/store/exchange, both graphics stages and actual
color pixels, physical buffers and descriptor retirement. Prepared buffer/shared
update/dispatch/span-readback cycles assert zero owner-thread managed bytes.

The root runner's ELECTRON2D_TEST_FLOAT_ATOMIC_REQUIREMENTS=1 checks storage classes,
base/add distinctions, unsupported capabilities, malformed/result-mismatched operands
and the feature-chain ABI. ELECTRON2D_TEST_FLOAT_ATOMICS=1 runs source authoring;
a fixture directory runs binary-only execution. The native checker validates query
ABI/null guards, bounded/repeatable feature masks and adjacent resource behavior.
The Linux test-only SDL dynamic shim refuses the extension request, verifying
fallback, false format availability, shader/binary refusal and healthy ordinary
FP32 compute. The preceding render bridge checks the missing-export path separately.

Local qualification covers Linux x64 Vulkan on the RTX 3090 Ti, original/derived
SPIR-V validation, a managed package consumer and strict full-trim NativeAOT with
reflection-based JSON disabled and trimming/compiler warnings treated as errors.
Foreign platforms/backends, native heap totals, broad performance and owner
acceptance remain unverified. Package bytes and local run identities are retained
in ignored bin/float-atomics/2026-10-29 evidence, outside the engine commit.

Native requirements follow the [Vulkan feature contract](https://docs.vulkan.org/refpages/latest/refpages/source/VkPhysicalDeviceShaderAtomicFloatFeaturesEXT.html)
and [SPIR-V addition extension](https://raw.githubusercontent.com/KhronosGroup/SPIRV-Registry/main/extensions/EXT/SPV_EXT_shader_atomic_float_add.asciidoc).
