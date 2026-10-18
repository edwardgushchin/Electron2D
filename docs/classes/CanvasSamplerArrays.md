# CanvasSamplerArrays

Last updated: 2026-10-18

- Declaration: `internal static partial class CanvasSamplerArrays`
- Sources: [CanvasSamplerArrays.cs](../../src/Servers/Rendering/CanvasSamplerArrays.cs), [CanvasSamplerArrays.Pairs.cs](../../src/Servers/Rendering/CanvasSamplerArrays.Pairs.cs)
- Component: [Shader materials](../components/shader-materials.md#fixed-sampled-descriptor-arrays)
- Visibility: internal; no backend handles or parser API are public.

## Description

Cold SPIR-V adaptation from logical fixed sampled descriptor arrays to the backend's scalar sampler slots. Reuses MaterialLightingProgram.Module, Instruction and Category. Source bytes stay immutable in ShaderProgram.Code; native consumers receive NativeCode.

## Member contract

`internal static byte[] Lower(byte[] code, ShaderTexture[] textures, bool needsSampler = false)` returns the original array when no adaptation is needed, otherwise a derived module. UniformConstant opaque resources, accesses, loads, copies, function parameters/results and selection flow become uint slot handles. Per-operation helpers select actual scalar descriptors with the same sampled image type. Implicit-derivative operations evaluate candidates before selecting values; explicit fetch/query operations branch before accessing a selected image. Private matching samplers adapt separate HLSL Load/query images. Generated declarations preserve combined versus separate descriptor representation. Entry interfaces and nonuniform annotations are rewritten for the derived layout; generated size/ID limits fail explicitly.

The private ValidatePairs propagates symbolic image/sampler binding constraints through helper arguments and returns to a fixed point, then checks the main entry. It accepts reusable correctly paired helpers and rejects cross-binding sampling. Descriptor index validity inside the authored shader remains its author's responsibility; every named sampler uses the existing fixed image sampling policy.

## Verification and limits

SamplerArrayTests covers both source languages, helper calls, indexed defaults, atomic callback-sensitive material updates, typed resource/RID arrays, full sixteen-slot capacity, overflow, reload, graph aliases and archives. Native operation oracles use independent scalar shader modules; stage/glyph/light/instance/HDR/MSAA/group consumers use framebuffer checks on Linux Wayland software Vulkan. Derived modules pass independent SPIR-V validation. Candidate evaluation may perform up to sixteen image operations per source operation; hardware performance, other backends/platforms and native allocation totals remain unverified.

## Slot

The private sealed Slot record stores the sampled SPIR-V element Type, native scalar Variable and flattened Index used while building per-operation helpers. It is cold compiler data, owns no native handle and does not enter frame state.
