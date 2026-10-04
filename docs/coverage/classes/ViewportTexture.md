# ViewportTexture API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/ViewportTexture.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ViewportTexture.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Texture2D](Texture.md#godot-texture2d). Electron2D type: [`public sealed class Electron2D.ViewportTexture`](../../classes/ViewportTexture.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class ViewportTexture`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ViewportTexture.xml) | [`public sealed class Electron2D.ViewportTexture`](../../classes/ViewportTexture.md) | Implemented | ADRs 0008/0028 executable single-layer native offscreen targets. SubViewportTests verifies authoring/defaults/guards, stable RID views, scene-local copies, resize observer failure, input/stretch isolation and AA recording invalidation. GPU/compatibility Engine.Run hosts verify clear/update/resize/transparency/visibility, dependent textures, external layers, prior-frame feedback, hidden root and detached cleanup; 64 warm active/64 idle frames with stable native dimensions distinguish managed memory from explicit readback/native driver allocation. GPU uniform/server RID sampling is separately exercised. Container/non-root GUI, layered/multiview, browser/other-platform, file/editor and human acceptance remain exact gaps. |
| [`property NodePath viewport_path = NodePath("")`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ViewportTexture.xml) | [`public System.String ViewportPath { get; set; }`](../../classes/ViewportTexture.md) | Implemented | ADRs 0008/0028 executable single-layer native offscreen targets. SubViewportTests verifies authoring/defaults/guards, stable RID views, scene-local copies, resize observer failure, input/stretch isolation and AA recording invalidation. GPU/compatibility Engine.Run hosts verify clear/update/resize/transparency/visibility, dependent textures, external layers, prior-frame feedback, hidden root and detached cleanup; 64 warm active/64 idle frames with stable native dimensions distinguish managed memory from explicit readback/native driver allocation. GPU uniform/server RID sampling is separately exercised. Container/non-root GUI, layered/multiview, browser/other-platform, file/editor and human acceptance remain exact gaps. |
