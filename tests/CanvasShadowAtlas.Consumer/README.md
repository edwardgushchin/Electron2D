# Canvas shadow atlas consumer

Uses only the public engine API. Build the fragment fixture with the separately published ShaderImport tool:

```text
ShaderImport Shaders/White.frag.glsl fragment OUT/White.frag.spv
CanvasShadowAtlas.Consumer author OUT
ShaderImport OUT/Authored.source.e2dres graph OUT/Authored.e2dres --visual-shader
ShaderImport OUT/AuthoredFar.source.e2dres graph OUT/AuthoredFar.e2dres --visual-shader
```

Build/run this project with `gpu`, `gpu-msaa`, `gpu-hdr` or `compatibility`, followed by the compiled fixture folder. Select the intended native GPU driver explicitly (`SDL_GPU_DRIVER=vulkan` on the qualified Windows profile). Set `ELECTRON2D_SHADOW_ATLAS_OUTPUT` to a directory for point/directional PCF13 pixel captures.

For binary qualification build with `Electron2DUsePackage=true` and a freshly packed engine package/feed/cache. Publish with PublishAot=true, PublishTrimmed=true, TrimMode=full and linker/compiler warnings as errors; execute the same profiles with the matching complete native cohort. The consumer does not compile shader source at execution. Root tests expose the same checks through ELECTRON2D_TEST_SHADOW_ATLAS and ELECTRON2D_SHADOW_ATLAS_SHADERS.

The check compares 105 whole images, independent scene/server/SubViewport rows, live resolution and resource/owner failures, plus strict prepared draw allocation brackets. It exercises rendering through ForceDraw(false), separately from simulation/input processing. Other platforms, automatic-frame/native allocations and owner acceptance remain unqualified.
