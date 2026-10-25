# GPUDeviceFactory

Last updated: 2026-10-25

Source: [GPUDeviceFactory.cs](../../src/Servers/Rendering/GPUDeviceFactory.cs).

Internal runtime helper. Creates the retained canvas or local-compute device with optional Vulkan sized points, preserves the core fallback and the existing Android relaxation policy. SizedPoints records successful feature negotiation; Metal uses its native point support. Debug mode remains disabled, matching the prior constructor. Each synchronous create owns and releases its temporary property set; callers retain native device ownership.

Contract and verification: [canvas point programs](../components/canvas-points.md).
