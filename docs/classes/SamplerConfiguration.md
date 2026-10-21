# SamplerConfiguration

Last updated: 2026-10-21

- Declaration: `internal readonly record struct SamplerConfiguration(RenderingDevice.SamplerFilter MinFilter, RenderingDevice.SamplerFilter MagFilter, RenderingDevice.SamplerFilter MIPFilter, RenderingDevice.SamplerRepeatMode RepeatU, RenderingDevice.SamplerRepeatMode RepeatV, float LODBias, float MinLOD, float MaxLOD, float AnisotropyMax, bool UseAnisotropy, bool EnableCompare, RenderingDevice.CompareOperator CompareOp)`
- Source: [SamplerConfiguration.cs](../../src/Servers/Rendering/SamplerConfiguration.cs)
- Component: [Shader materials](../components/shader-materials.md#explicit-image-sampler-state)
- Visibility: internal; no public native sampler handles.

Copies the complete supported RDSamplerState value family for independent device resources and high-level Shader bindings. Authoring capture validates live input getters, enum/finite values, ordered nonnegative LOD bounds, anisotropy range and native address availability. High-level Shader additionally rejects comparison and linear/effective-anisotropic integer sampling. The value has no borrowed resource references and supports cache identity without per-frame wrappers.

| Member | Contract |
| --- | --- |
| `internal bool Anisotropic { get; }` | True only when enabled with AnisotropyMax above one. |
| `internal bool Linear { get; }` | Any linear filter or effective anisotropy; used to reject incompatible integer images. |
| `internal static SamplerConfiguration Capture(RDSamplerState state)` | Copies and validates a live caller configuration. |
| `internal void Validate()` | Validates the complete supported value family; border/mirror-clamp reject explicitly. |
| `internal RDSamplerState CreateState()` | Allocates an independent caller-owned authoring copy. |
| `internal SDL.GPUSamplerCreateInfo Native(string driver, bool anisotropyAvailable = true)` | Projects validated fields to the native ABI; rejects nonzero bias on Metal and unavailable effective anisotropy. W addressing remains clamp for the two-dimensional family. |

Native projection is consumed only from validated snapshots. Device SamplerCreate retains its separate owned RID/native handle. Shader preparation shares values through [ShaderSamplerCache](ShaderSamplerCache.md). [SamplerConfigurationTests](../../tests/Electron2D.Tests/SamplerConfigurationTests.cs) verifies all native fields and exact capability guards without creating a device; [the public consumer](../../tests/ShaderSamplers.Consumer/ShaderSamplers.Consumer.csproj) exercises real filtering/address/mip pixels and stored metadata. Metal execution and native allocator behavior remain unverified.
