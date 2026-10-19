# TextServerDummy API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/TextServerDummy.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TextServerDummy.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [TextServerExtension](TextServerExtension.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class TextServerDummy`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TextServerDummy.xml) | — | Blocked | ADR 0046/0107: requires the complete external TextServerExtension instance/virtual dispatch and font-cache ownership family, including callback errors, provider retirement and registration/replacement integration. The retained built-in TextServer shaped-buffer producer does not expose this separate constructible implementation/extension contract. This member enters that extension/provider slice; no zero-result dummy or inert virtual mirror is added. |
