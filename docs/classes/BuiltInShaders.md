# BuiltInShaders

Last updated: 2026-10-04

- Declaration: `internal static class BuiltInShaders`
- Source: [BuiltInShaders.cs](../../src/Servers/Rendering/BuiltInShaders.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

## Description

Loads the three embedded SPIR-V programs once from Electron2D.dll. The corresponding HLSL sources live beside the artifacts. Initialization copies manifest resource streams to engine-owned arrays and disposes streams. Missing resources raise InvalidOperationException during type initialization; callers must treat the arrays as immutable.

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal static readonly byte[] Vertex` | [Vertex program](#vertex-program) |
| `internal static readonly byte[] Fragment` | [Fragment program](#fragment-program) |
| `internal static readonly byte[] Clip` | [Clip program](#clip-program) |

## Member descriptions

### Vertex program

`internal static readonly byte[] Vertex`

Canvas.vert.spv converts physical framebuffer positions using a dimensions uniform and forwards color/UV.

### Fragment program

`internal static readonly byte[] Fragment`

Canvas.frag.spv samples the reserved command TEXTURE and multiplies it by the interpolated drawing color.

## Verification and limits

[tools/shaders/check.py](../../tools/shaders/check.py) recompiles and compares all embedded programs byte-for-byte; native canvas tests verify their rendered output. Updating interface source requires regenerating artifacts together.

### Clip program

`internal static readonly byte[] Clip`

Clip.frag.spv samples command TEXTURE alpha multiplied by vertex alpha and uses SCREEN_TEXTURE RGB at physical SCREEN_PIXEL_SIZE coordinates. The immutable bytes are imported from Clip.frag.hlsl and verified by the pinned shader check. Native mask pixel checks exercise both backends.
