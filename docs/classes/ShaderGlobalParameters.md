# ShaderGlobalParameters

Last updated: 2026-10-25

Internal typed storage for renderer-session globals and scene-authored overrides; not part of the public API. Source: [ShaderGlobalParameters.cs](../../src/Servers/Rendering/ShaderGlobalParameters.cs), [ShaderGlobalParameters.Scene.cs](../../src/Servers/Rendering/ShaderGlobalParameters.Scene.cs) and [ShaderGlobalParameters.Project.cs](../../src/Servers/Rendering/ShaderGlobalParameters.Project.cs).

Owns exact typed base/override carriers, copied numeric matrices with prepared override storage and borrowed image references. Validates logical shader roles before submission and writes into reflected physical buffers. Clearing releases registry references without disposing borrowed resources. RenderingServer supplies owner/submission validation; MaterialState supplies ordinary preflight, uniform and texture consumers. See [the full component](../components/global-shader-parameters.md) for behavior and verification.

Scene-authored global shader values now use [ShaderGlobalsOverride](ShaderGlobalsOverride.md). Its typed portable values, tree-order arbitration and renderer-session restoration are described in the [global parameter component](../components/global-shader-parameters.md#scene-owned-overrides).

Project preparation captures registered typed definitions under the registry lock, then validates values and loads isolated image resources before service publication. Normalized path aliases share one owned image. Failed preparation retires staged images; renderer shutdown retires all configured images while collecting disposal failures. Existing add/set references remain borrowed.

NORMAL_MAP_FLAGS is reserved for per-command canvas state and cannot be registered as a global. The public typed global parameter contract otherwise stays unchanged. See [the connected normal producer](../components/visual-shaders.md#fresnel-and-implicit-canvas-normals).
