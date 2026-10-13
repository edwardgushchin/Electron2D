# MaterialLightingProgram

Last updated: 2026-10-14

- Declaration: `internal static class MaterialLightingProgram`
- Source: [MaterialLightingProgram.cs](../../src/Servers/Rendering/MaterialLightingProgram.cs)
- Component: [shader-materials](../components/shader-materials.md#automatic-scene-lighting)
- Visibility: internal; unavailable to engine consumers.

Composes an embedded lighting function into validated material SPIR-V on the cold renderer path. Exact build-generated ID positions distinguish IDs from literals; shared nonaggregate types/imports are deduplicated. Material code and descriptors remain unchanged. Entry returns load their authored output and pass color, UV, fragment position and private item index to the helper, then publish its result. Discard retains its original control flow.

The helper's sampler representation follows the source module's combined or paired form. Binding offsets preserve all material slots; combined capacity is checked before native creation. SPIR-V 1.4+ modules retain complete entry interfaces and modern storage-buffer class. Cached bytecode is renderer-owned and evicted with unused programs. Nested private parsing values introduce no public surface.

MaterialLightingTests executes native output and error cleanup; the independent pinned validator checks 257 lighting source/profile combinations. Native-driver success alone is insufficient proof of module validity. Other platform and native allocation acceptance remain unverified.

ComposeGlyph reuses the same exact relocation linker for incoming COLOR decoding and a post-output LCD helper. Component access chains become by-value extracts; paired source image depth metadata is retained, interpolation decorations and entry interfaces are deduplicated. An extra sampler shifts only set-two storage bindings, preserving set-three material uniform slots. Original discard/return control flow remains intact. check_canvas_glyphs.py independently validates 732 glyph variants; check_material_lighting.py validates 257 existing lighting variants.
