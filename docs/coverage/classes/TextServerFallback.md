# TextServerFallback API coverage

Last updated: 2026-09-23

Godot source: [modules/text_server_fb/doc_classes/TextServerFallback.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/text_server_fb/doc_classes/TextServerFallback.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [TextServerExtension](TextServerExtension.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class TextServerFallback`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/text_server_fb/doc_classes/TextServerFallback.xml) | — | Blocked | ADR 0046/0107: requires the complete external TextServerExtension instance/virtual dispatch and font-cache ownership family, including callback errors, provider retirement and registration/replacement integration. The retained built-in TextServer shaped-buffer producer does not expose this separate constructible implementation/extension contract. This member enters that extension/provider slice; no zero-result dummy or inert virtual mirror is added. |
