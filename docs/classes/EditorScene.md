# EditorScene

Last updated: 2026-10-05

- Declaration: `internal static class EditorScene`
- Source: [EditorScene.cs](../../editor/EditorScene.cs)
- Assembly: `Electron2D.Editor`, separate from `Electron2D.dll`
- Component: [Editor startup](../components/editor-startup.md)

This internal scene builder composes the startup screen from public Window, Control, TextureRect and Label nodes. It has no runtime counterpart or public engine API.

| Declaration | Contract |
| --- | --- |
| `internal static Window CreateWindow(Texture mark, Texture wordmark, Font font)` | Creates a detached visible `Electron2D` root with a 1152×800 client area, a 144 px visible mark, an outlined wordmark and a Regular caption. |

Both textures and the font are borrowed and must stay alive until Engine.Run returns. The caller transfers the returned root to Engine.Run, which owns scene/native cleanup. The Ready handler selects the dark identity clear color after renderer initialization. Native window closing follows SceneTree's ordinary AutoAcceptQuit policy. The caption explicitly uses the supplied font through Label's theme override. Center anchors place a 488×334 block; transform snapping keeps its rendered origin on whole pixels even at odd client dimensions. The mark uses nearest filtering at 1.5× its 128 px canvas, giving a 144 px visible silhouette and 12 px modules. The outlined name retains linear filtering. TextureRect's inherited validation, layout and ownership rules apply.

The [entry point](../../editor/Program.cs) is the executable usage example. [EditorSceneTests](../../tests/Electron2D.Tests/EditorSceneTests.cs) links this exact source to check rendered placement, native resize/close and resource lifetime. The builder provides no persistence, project creation, inspector or capture operation.
