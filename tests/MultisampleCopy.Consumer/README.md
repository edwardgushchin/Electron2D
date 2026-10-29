# Multisample copy consumer

Public-only raster producers, equal-count raw copies and GLSL/HLSL per-sample readers. Exercises color, all five selected depth/stencil formats, 2X/4X/8X, rectangles, self copies, array layers, shared slices, compatible formats, retirement and rejection paths. Uses the ordinary tools/native-consumer.props overrides and requires a supporting Vulkan GPU plus the pinned native aspect adapter.

Run without arguments to compile source through ELECTRON2D_SHADER_IMPORT_TOOL. ELECTRON2D_MS_COPIES_OUTPUT exports versioned shader containers and original SPIR-V. Pass that directory as the sole argument for compiler-free replay. The root runner selects ELECTRON2D_TEST_MS_COPIES=1 or the binary directory. Package mode uses Electron2DUsePackage/Electron2DVersion; strict NativeAOT runs the same public scenarios.

Each format/count pair measures thirty-two prepared copy/sample-read cycles after sixteen warmups. Owner-thread managed bytes are checked separately from native/driver allocations and other platforms. See [the owning contract and verification limits](../../docs/components/multisample-copies.md).
