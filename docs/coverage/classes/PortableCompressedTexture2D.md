# PortableCompressedTexture2D API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/PortableCompressedTexture2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Texture2D](Texture2D.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class PortableCompressedTexture2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`enum CompressionMode`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`enum_value COMPRESSION_MODE_ASTC [CompressionMode] = 6`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`enum_value COMPRESSION_MODE_BASIS_UNIVERSAL [CompressionMode] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`enum_value COMPRESSION_MODE_BPTC [CompressionMode] = 5`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`enum_value COMPRESSION_MODE_ETC2 [CompressionMode] = 4`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`enum_value COMPRESSION_MODE_LOSSLESS [CompressionMode] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`enum_value COMPRESSION_MODE_LOSSY [CompressionMode] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`enum_value COMPRESSION_MODE_S3TC [CompressionMode] = 3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method create_from_image(Image image, int compression_mode [PortableCompressedTexture2D.CompressionMode], bool normal_map = false, float lossy_quality = 0.8) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method get_compression_mode() -> int [PortableCompressedTexture2D.CompressionMode]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method is_keeping_all_compressed_buffers() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method set_basisu_compressor_params(int uastc_level, float rdo_quality_loss) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method set_keep_all_compressed_buffers(bool keep) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`property bool keep_compressed_buffer = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`property bool resource_local_to_scene = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`property Vector2 size_override = Vector2(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PortableCompressedTexture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
