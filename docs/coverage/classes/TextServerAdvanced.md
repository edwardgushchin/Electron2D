# TextServerAdvanced API coverage

Last updated: 2026-09-23

Godot source: [modules/text_server_adv/doc_classes/TextServerAdvanced.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/text_server_adv/doc_classes/TextServerAdvanced.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [TextServerExtension](TextServerExtension.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class TextServerAdvanced`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/text_server_adv/doc_classes/TextServerAdvanced.xml) | — | Blocked | ADR 0046/0107: built-in buffers and service-owned fonts/caches now execute. Requires constructible external TextServerExtension virtual/callback dispatch for the complete family, provider-owned source/cache lifetimes, callback errors/reentry, retirement and public provider registration/replacement integration. This member enters that external implementation/dispatch slice. |
