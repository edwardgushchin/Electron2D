# SamplerConfiguration

Last updated: 2026-11-15

- Declaration: `internal readonly partial record struct SamplerConfiguration`
- Source: [SamplerConfiguration.cs](../../src/Servers/Rendering/SamplerConfiguration.cs)
- Component: [Shader materials](../components/shader-materials.md#explicit-image-sampler-state)
- Visibility: internal; no public native sampler handles.

Copies the complete supported RDSamplerState value family for independent device resources and high-level Shader bindings. Authoring capture validates live input getters, enum and finite values, ordered nonnegative LOD bounds, anisotropy range and texel-coordinate combinations. Native creation checks backend capabilities. High-level Shader rejects comparison and incompatible integer sampling. The value has no borrowed resource references and supports cache identity without per-frame wrappers.

| Member | Contract |
| --- | --- |
| `internal bool Anisotropic { get; }` | True only when enabled with AnisotropyMax above one. |
| `internal bool Linear { get; }` | Any linear filter or requested anisotropy above one. Used to reject incompatible integer images. |
| `internal static SamplerConfiguration Capture(RDSamplerState state)` | Copies and validates a live caller configuration. |
| `internal void Validate()` | Validates the complete value family and supported state combinations. |
| `internal RDSamplerState CreateState()` | Allocates an independent caller-owned authoring copy. |
| `internal SDL.GPUSamplerCreateInfo Native(string driver, bool anisotropyAvailable = true, bool anisotropicPointMIP = false)` | Projects the captured state with the native [filtering and LOD rules](../components/local-compute.md#sampler-filtering-and-lod). W addressing stays clamped. |

Native projection is consumed only from validated snapshots. Device SamplerCreate retains its separate owned RID/native handle. Shader preparation shares values through [ShaderSamplerCache](ShaderSamplerCache.md). [SamplerConfigurationTests](../../tests/Electron2D.Tests/SamplerConfigurationTests.cs) verifies all native fields and exact capability guards without creating a device; [the public consumer](../../tests/ShaderSamplers.Consumer/ShaderSamplers.Consumer.csproj) exercises real filtering/address/mip pixels and stored metadata. Metal execution and native allocator behavior remain unverified.
