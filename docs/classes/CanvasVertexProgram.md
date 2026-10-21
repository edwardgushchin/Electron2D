# CanvasVertexProgram

Last updated: 2026-10-18

- Declaration: `internal static class CanvasVertexProgram`
- Source: [CanvasVertexProgram.cs](../../src/Servers/Rendering/CanvasVertexProgram.cs)
- Component: [Shader materials](../components/shader-materials.md#authored-vertex-stages)

## Description

Cold SPIR-V composition for an authored canvas vertex stage. Compose copies its input and creates one ordinary/instanced and unlit/lit variant. It extends the renderer-owned Frame block, wraps main, applies prepared hardware instance transformation/color before the authored body, then canvas modulation and private glyph/light outputs. The published ShaderProgram owns four derived variants; no rewriting occurs in the prepared draw loop.

## Internal API

`internal static byte[] Compose(byte[] code, bool instanced, bool lighting)` returns an independent derived module. Inputs must already pass the canvas reflection contract. Hardware input-pointer transformations preserve pointer storage classes through access chains; escaping transformed input pointers into function arguments rejects explicitly. Native handles and source compilation remain outside this component. Shared instruction parsing comes from MaterialLightingProgram.

## Verification and limits

CanvasVertexProgramTests checks real GLSL/HLSL native pixels, hardware instances and automatic lighting on Linux Vulkan. The import tool and external spirv-val validate fixed input/derived fixtures. Runtime structural/reflection checks do not establish validity of arbitrary external instructions. See the component for the exact ABI, clipping/bounds limitation and foreign-platform gates.

## Procedural mesh integration

Procedural variants set authored position input to zero and transform homogeneous output through the retained affine basis/origin. They preserve clip Z/W, apply optional framebuffer snapping and derive built-in group screen UVs. Public Frame remains one float2; physical private metadata is renderer-owned. See [the connected contract](../components/meshes.md#procedural-surfaces-without-positions).

## Writable instance consumer

Exported/indirect MultiMesh storage selects a retained GPU consumer. CanvasMesh captures source vertices/indices, palette deformation, LOD metadata and finite authored canvas state. CanvasMultiMesh retains manual/authored bounds and resource guards. GPUCanvasBackend owns conversion outputs, native index/LOD buffers and indirect commands; the private kernel interpolates raw device records and chooses each instance's LOD. CanvasVertexProgram snaps ordinary instance positions before the authored stage and procedural homogeneous output after it; built-in instance vertices support snapping and group screen UVs. Group color premultiplication, masks and original vertex IDs remain connected. Relaxed Android devices bind each LOD instance separately when nonzero indirect first-instance support is unavailable. See [ownership and Linux-native verification limits](../components/meshes.md#writable-instance-device-buffers).

Original instance identity is carried as two exact numerical 16-bit halves in private translation padding. Canvas vertex composition restores the original 32-bit builtin for indirect subranges and per-instance LOD commands; legacy streams retain their native builtin. GLSL/HLSL command pixels include source ID 65537. Private buffers that were never exported retire their device metadata after pending work, while expired exported identities remain available for explicit FreeRID.

Point topology also receives an initialized float PointSize output. Compose reuses an existing standalone/member builtin or adds an output interface, stores one before the authored entry and preserves later authored writes. Native point/line consumers exercise GLSL/HLSL modules without a size assignment and HLSL with a declared size output; other platforms retain their own execution gates. See [native primitive integration](../components/canvas-rendering.md#native-point-and-line-primitives).
