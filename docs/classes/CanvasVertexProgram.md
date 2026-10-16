# CanvasVertexProgram

Last updated: 2026-10-16

- Declaration: `internal static class CanvasVertexProgram`
- Source: [CanvasVertexProgram.cs](../../src/Servers/Rendering/CanvasVertexProgram.cs)
- Component: [Shader materials](../components/shader-materials.md#authored-vertex-stages)

## Description

Cold SPIR-V composition for an authored canvas vertex stage. Compose copies its input and creates one ordinary/instanced and unlit/lit variant. It extends the renderer-owned Frame block, wraps main, applies prepared hardware instance transformation/color before the authored body, then canvas modulation and private glyph/light outputs. The published ShaderProgram owns four derived variants; no rewriting occurs in the prepared draw loop.

## Internal API

`internal static byte[] Compose(byte[] code, bool instanced, bool lighting)` returns an independent derived module. Inputs must already pass the canvas reflection contract. Hardware input-pointer transformations preserve pointer storage classes through access chains; escaping transformed input pointers into function arguments rejects explicitly. Native handles and source compilation remain outside this component. Shared instruction parsing comes from MaterialLightingProgram.

## Verification and limits

CanvasVertexProgramTests checks real GLSL/HLSL native pixels, hardware instances and automatic lighting on Linux Vulkan. The import tool and external spirv-val validate fixed input/derived fixtures. Runtime structural/reflection checks do not establish validity of arbitrary external instructions. See the component for the exact ABI, clipping/bounds limitation and foreign-platform gates.
