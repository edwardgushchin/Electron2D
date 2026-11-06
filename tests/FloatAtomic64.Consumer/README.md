# FP64 atomic public consumer

Uses only the public API and links the root FP64 behavioral fixtures. Build with
the matching native bundle through tools/native-consumer.props. No argument runs
GLSL/source authoring; ELECTRON2D_FLOAT_ATOMIC64_OUTPUT saves original modules and
versioned binaries. The directory as the sole argument runs without a compiler.
Repeat against the managed package and strict full-trim NativeAOT.

Compile the Linux/Windows test-only tools/render-native/test_float_atomic_mask.c against
the pinned SDL headers and use SDL3_DYNAMIC_API to select it. With compiled fixtures,
ELECTRON2D_TEST_FLOAT_ATOMIC_MASK=base|add|buffer|shared tests logical gates while
native features remain enabled. Values wide and precision refuse native creation
requests, verifying recovery to FP32 atomics and ordinary double arithmetic where
available. On Windows select Vulkan with SDL_GPU_DRIVER=vulkan. A complete native
module built with the preceding ABI1 query checks version fallback; the module
must retain its allocator, SDL dispatch and other component exports. Restore exact
qualified bytes afterward.

[Shared behavior, evidence and full-platform gaps](../../docs/components/float-atomics.md#fp64-buffer-and-workgroup-atomics).
