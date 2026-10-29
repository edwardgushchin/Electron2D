# FP32 atomic public consumer

Uses only the public API and links the root FP32 behavioral fixture. Build with the
matching pinned native bundle through tools/native-consumer.props. Run with no
argument for GLSL/source execution; ELECTRON2D_FLOAT_ATOMIC_OUTPUT saves original
modules and versioned binaries. Pass that directory as the sole argument for
binary-only execution with the source compiler unavailable. Repeat against the
actual managed package and with strict trimmed NativeAOT.

ELECTRON2D_TEST_FLOAT_ATOMIC_MASK=all plus the compiled test-only
`tools/render-native/test_float_atomic_mask.c` SDL3_DYNAMIC_API shim checks optional
request refusal. Replacing only the consumer's render bridge with the preceding
bridge checks missing-export behavior; restore its exact qualified bytes afterward.
Both profiles require the compiled fixture directory and execute ordinary FP32 work.

[Behavior, commands, feature roles and qualification limits](../../docs/components/float-atomics.md).
