# ShaderGlobalParameters

Internal typed storage for renderer-session globals and scene-authored overrides; not part of the public API. Source: [ShaderGlobalParameters.cs](../../src/Servers/Rendering/ShaderGlobalParameters.cs) and [ShaderGlobalParameters.Scene.cs](../../src/Servers/Rendering/ShaderGlobalParameters.Scene.cs).

Owns exact typed base/override carriers, copied numeric matrices with prepared override storage and borrowed image references. Validates logical shader roles before submission and writes into reflected physical buffers. Clearing releases registry references without disposing borrowed resources. RenderingServer supplies owner/submission validation; MaterialState supplies ordinary preflight, uniform and texture consumers. See [the full component](../components/global-shader-parameters.md) for behavior and verification.

Scene-authored global shader values now use [ShaderGlobalsOverride](ShaderGlobalsOverride.md). Its typed portable values, tree-order arbitration and renderer-session restoration are described in the [global parameter component](../components/global-shader-parameters.md#scene-owned-overrides).
