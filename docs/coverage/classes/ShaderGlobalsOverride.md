# ShaderGlobalsOverride API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/ShaderGlobalsOverride.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ShaderGlobalsOverride.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node](Node.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class ShaderGlobalsOverride`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ShaderGlobalsOverride.xml) | — | Blocked | Exact dependency: typed per-node global override entries, active-scene selection/priority, entry/exit/disposal restoration and portable scene storage/binding. The renderer-session global service now executes both stages, but does not produce that scene-owned override lifecycle (ADRs 0028/0023). No inert node is exposed. |
