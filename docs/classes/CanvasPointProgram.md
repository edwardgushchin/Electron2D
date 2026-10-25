# CanvasPointProgram

Last updated: 2026-10-25

Source: [CanvasPointProgram.cs](../../src/Servers/Rendering/CanvasPointProgram.cs).

Internal runtime helper. Cold native shader capability validation. SPIR-V BuiltIn decorations identify PointCoord and PointSize independently of debug names. A vertex program that stores only the literal float one needs no optional capability. Other writes, output-block copies, pointer escapes and uncertain output-pointer selection conservatively require sized points. Vulkan/Metal accept point coordinates; other SDL drivers reject them before native shader publication. This classifier does not replace structural reflection or independent module validation.

Contract and verification: [canvas point programs](../components/canvas-points.md).
