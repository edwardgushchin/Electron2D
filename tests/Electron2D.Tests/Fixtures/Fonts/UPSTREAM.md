# Text test fonts

Pinned source revision: `ed1daf0bf001b61586d9930840f2f1394092c079`. Files are copied unchanged.

| File | Source | SHA-256 |
| --- | --- | --- |
| `Vazirmatn_Regular.woff2` | [upstream](https://raw.githubusercontent.com/godotengine/godot/ed1daf0bf001b61586d9930840f2f1394092c079/thirdparty/fonts/Vazirmatn_Regular.woff2) | `ce58edf0377c327417034a10ba7274cc61b61999552fcb992c33f87539942bbf` |
| `LICENSE.Vazirmatn.txt` | [upstream](https://raw.githubusercontent.com/godotengine/godot/ed1daf0bf001b61586d9930840f2f1394092c079/thirdparty/fonts/LICENSE.Vazirmatn.txt) | `17e355067c8284f47743a1ee3b1ef7ff684ff0601eda357f9353b10b3016ab31` |
| `NotoSansHebrew_Regular.woff2` | [upstream](https://raw.githubusercontent.com/godotengine/godot/ed1daf0bf001b61586d9930840f2f1394092c079/thirdparty/fonts/NotoSansHebrew_Regular.woff2) | `06068dc89adf6045e66e3811eb19dd805e0073c5fa37c7d769004ee960e90efb` |
| `LICENSE.Noto.txt` | [upstream](https://raw.githubusercontent.com/godotengine/godot/ed1daf0bf001b61586d9930840f2f1394092c079/thirdparty/fonts/LICENSE.Noto.txt) | `6a73f9541c2de74158c0e7cf6b0a58ef774f5a780bf191f2d7ec9cc53efe2bf2` |
| `DroidSansFallback.woff2` | [upstream](https://raw.githubusercontent.com/godotengine/godot/ed1daf0bf001b61586d9930840f2f1394092c079/thirdparty/fonts/DroidSansFallback.woff2) | `b41f0f026055325a1c78c7f380e811ab0a5122d1c26a85ec1243f3c024c66002` |
| `LICENSE.DroidSans.txt` | [upstream](https://raw.githubusercontent.com/godotengine/godot/ed1daf0bf001b61586d9930840f2f1394092c079/thirdparty/fonts/LICENSE.DroidSans.txt) | `d0588452395c674334105d9a39e4d8627c66080f45491013e877139581a78ed1` |

These fixtures and their accompanying licenses are test-only and are not embedded in Electron2D.dll or application publishes. The Open Sans fixture uses the engine's embedded resource.

`ColorTest.ttf` is an engine-authored COLR/CPAL fixture under the repository MIT license: glyph A consists of adjacent red/blue rectangles. Rebuild it with `python3 tools/coverage/generate_color_font.py` (fontTools 4.65.0 was used). It contains no third-party font outlines.
