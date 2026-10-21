# ShaderSamplerCache

Last updated: 2026-10-21

- Declaration: `internal sealed class ShaderSamplerCache(RenderHandle device, string driver, bool anisotropyAvailable = true) : IDisposable`
- Source: [ShaderSamplerCache.cs](../../src/Servers/Rendering/ShaderSamplerCache.cs)
- Component: [Shader materials](../components/shader-materials.md#explicit-image-sampler-state)
- Visibility: internal; owned by one canvas backend or drawable device.

Shares native samplers by complete immutable SamplerConfiguration. Each RenderHandle retains its device owner; creation failure does not mark a value used. No caller RDSamplerState is retained. Access is renderer/device owner-thread only. The cache has no fixed 32-state flush threshold and does not grow indefinitely as authoring values change.

| Member | Contract |
| --- | --- |
| `internal nint Get(SamplerConfiguration configuration)` | Obtains or prepares the native handle, validates backend capabilities and marks successful use. First use can allocate; repeated use at retained capacity does not allocate managed memory. |
| `internal void AdvanceEpoch()` | Retires values absent from both current and previous use sets, swaps/clears history and retains hash capacities. Native release is deferred by the graphics backend. |
| `public void Dispose()` | Releases retained sampler handles and clears history during owner teardown. |

Canvas BeginFrame advances even after an earlier failed frame. Renderer-owned drawable devices advance once before root native frames, preserving 40 distinct blit states across multiple internal submissions. Independent local devices advance after successful submission. A sampler can remain cached during recent unused epochs; backend work retains its native lifetime after release. [The executable consumer](../../tests/ShaderSamplers.Consumer/ShaderSamplers.Consumer.csproj) checks more than 32 simultaneous configurations, actual pixels and 64 active/64 idle whole-frame zero-managed-byte intervals. Native/driver allocation and physical-GPU behavior remain unverified.
