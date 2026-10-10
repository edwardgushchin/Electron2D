# CompressedTextureLayered API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/CompressedTextureLayered.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompressedTextureLayered.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [TextureLayered](TextureLayered.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class CompressedTextureLayered`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompressedTextureLayered.xml) | — | Blocked | Trigger: compressed array-file parsing/decoding or direct compressed native upload, source format/mipmap ownership and reproducible sampling over the executable TextureArray pipeline (ADR 0028). |
| [`method load(String path) -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompressedTextureLayered.xml) | — | Blocked | Trigger: compressed array-file parsing/decoding or direct compressed native upload, source format/mipmap ownership and reproducible sampling over the executable TextureArray pipeline (ADR 0028). |
| [`property String load_path = ""`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompressedTextureLayered.xml) | — | Blocked | Trigger: compressed array-file parsing/decoding or direct compressed native upload, source format/mipmap ownership and reproducible sampling over the executable TextureArray pipeline (ADR 0028). |
