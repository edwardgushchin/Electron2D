# EditorScene

Last updated: 2026-10-05

- Declaration: `internal static class EditorScene`
- Source: [EditorScene.cs](../../editor/EditorScene.cs)
- Assembly: `Electron2D.Editor`, separate from `Electron2D.dll`
- Component: [Editor startup](../components/editor-startup.md)

This internal scene builder composes the startup screen from public Window, Control, TextureRect and Label nodes. It has no runtime counterpart or public engine API.

| Declaration | Contract |
| --- | --- |
| `internal static Window CreateWindow(Texture texture)` | Creates a detached visible `Electron2D` root with a 1152×800 client area and one centered logo control. |

The texture is borrowed and must stay alive until Engine.Run returns. The caller transfers the returned root to Engine.Run, which owns scene/native cleanup. The Ready handler selects the dark identity clear color after renderer initialization. Native window closing follows SceneTree's ordinary AutoAcceptQuit policy. The descriptor uses inherited Label theme overrides. Center anchors update placement when the viewport changes size. TextureRect's inherited validation, layout and ownership rules apply.

The [entry point](../../editor/Program.cs) is the executable usage example. [EditorSceneTests](../../tests/Electron2D.Tests/EditorSceneTests.cs) links this exact source to check rendered placement, native resize/close and resource lifetime. The builder provides no persistence, project creation, inspector or capture operation.
