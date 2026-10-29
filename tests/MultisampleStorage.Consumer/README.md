# Multisample storage consumer

Public-only GLSL storage-image scenarios for compute and vertex/fragment graphics, including samples, layers, descriptor arrays, signed/unsigned R32 atomics, shared-view lifetime and exact refusal. Requires the pinned native Vulkan render/image adapter and a GPU supporting multisample storage images.

Build with the existing tools/native-consumer.props overrides. Run without arguments to compile source through ELECTRON2D_SHADER_IMPORT_TOOL. ELECTRON2D_MS_STORAGE_OUTPUT exports versioned binaries, original SPIR-V and graphics.png. Run with that output directory as the sole argument to replay without a compiler. The same commands apply to a strict NativeAOT publish and package mode through Electron2DUsePackage/Electron2DVersion.

With the built tools/render-native/test_multisample_mask.c selected by SDL3_DYNAMIC_API and ELECTRON2D_TEST_MULTISAMPLE_MASK=storage, pass the fixture directory to exercise optional device-feature refusal and surviving ordinary/MS sampled paths.

The root runner selects this suite with ELECTRON2D_TEST_MS_STORAGE=1 or a binary directory. ELECTRON2D_TEST_MS_STORAGE_LOWER=<fixture directory> exports actual remapped shaders into its native subdirectory for independent spirv-val validation. Prepared dispatch and graphics/readback measure owner-thread managed allocations; native heap and foreign platform acceptance remain separate.
