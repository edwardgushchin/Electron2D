# ShaderPrecision

Last updated: 2026-10-29

Internal helper in [ShaderPrecision.cs](../../src/Servers/Rendering/ShaderPrecision.cs). The bounded SPIR-V instruction scan identifies independent declared FP64/Int64 requirements and rejects malformed floating/integer type widths and unsupported precision profiles. Native creation requires Vulkan and each corresponding retained feature property set only after successful device negotiation. Symbolic templates pass the same original-module guard before publication.

This is a precision guard, not a full semantic validator or a per-frame operation. The [native FP64 contract](../components/local-compute.md#native-fp64-arithmetic-and-data) owns raw data, specialization, fallback and verification limits.

The [multisample storage contract](../components/graphics-storage-images.md#multisample-storage-images) adds independently negotiated Vulkan storage-image samples. The bounded shader guard checks the declared capability and actual multisample storage type before native publication; optional feature refusal retains the other precision, storage and raster capabilities.
