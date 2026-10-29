# Depth/stencil transfer consumer

Public-only allocation, update, region/self copy, raw/span/queued readback and native depth/stencil graphics scenarios. Requires a GPU, the pinned native aspect adapter and supported depth formats. Uses the ordinary tools/native-consumer.props overrides.

Run without arguments to compile GLSL source through ELECTRON2D_SHADER_IMPORT_TOOL. ELECTRON2D_DEPTH_TRANSFERS_OUTPUT exports versioned binaries, original SPIR-V and native target PNGs. Pass that directory as the sole argument for compiler-free replay. Package mode uses Electron2DUsePackage/Electron2DVersion; strict NativeAOT uses the same public scenarios and byte layouts.

The root runner selects ELECTRON2D_TEST_DEPTH_TRANSFERS=1 or a binary directory. ELECTRON2D_TEST_DEPTH_ADAPTER_MISSING=1 checks a native library without the aspect exports: transfer queries/allocation reject while ordinary empty depth targets remain executable. MS depth copy roles also reject the missing adapter while empty MS depth targets without copy roles remain executable. No compiler is needed for that refusal case.

See [depth byte layout, ownership and verification limits](../../docs/components/depth-texture-transfers.md). Prepared cycles check owner-thread managed allocation; native heap and other platforms remain separate.
