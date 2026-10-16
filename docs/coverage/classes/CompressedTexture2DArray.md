# CompressedTexture2DArray API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/CompressedTexture2DArray.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompressedTexture2DArray.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [CompressedTextureLayered](CompressedTextureLayered.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class CompressedTexture2DArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompressedTexture2DArray.xml) | — | Blocked | ADRs 0028/0039: raw/BC/PNG/WebP and KTX2 Basis LDR/HDR decoding execute. Remaining prerequisite: GSTL array parsing and homogeneous mip/layer validation, legacy .basis channel-role decoding, CompressedTextureLayered/CompressedTextureArray ownership and LoadPath/cache/archive integration, then verified layer/mip sampling through TextureArray. |
