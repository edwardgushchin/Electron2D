# EditorScene

Last updated: 2026-10-05

- Declaration: `internal static class EditorScene`
- Source: [EditorScene.cs](../../editor/EditorScene.cs)
- Assembly: `Electron2D.Editor`, separate from `Electron2D.dll`
- Component: [Editor startup](../components/editor-startup.md)

This internal scene builder composes the startup screen from public Window, Control, TextureRect and Label nodes. It has no runtime counterpart or public engine API.

| Declaration | Contract |
| --- | --- |
| `internal static Window CreateWindow(Texture mark, Texture sparkle, Texture appIcon, Font semibold, Font regular)` | Creates a detached visible Electron2D root with a 1152×800 client area, the shaded character, two open pixel sparkles, a live font-rendered two-color name and the one-line `Agent-native cross-platform 2D game engine` descriptor. |

Textures and fonts are borrowed until Engine.Run returns. The caller transfers the root to Engine.Run for scene/native cleanup. The Ready handler chooses the dark clear color and copies the app icon into the native icon where Feature.Icon is available; its temporary image is disposed after submission. Desktop launcher identity belongs to the entry point and platform packaging. Native close follows SceneTree's AutoAcceptQuit policy.

Center anchors place a compact 640×320 block; transform snapping keeps its origin on whole pixels at odd client dimensions. The character uses a 160×160 canvas at (240, 12) with nearest filtering. Two open sparkles occupy 15×15 and 18×18 canvases. Electron and 2D use IBM Plex Sans SemiBold at 82 px, share a baseline at local y=260 and are centered by their shaped advance widths. The descriptor uses Regular at 16 px, centered at baseline y=292. Public Font metrics position the labels; no wordmark/descriptor texture or glyph path is used by this scene.

[Program.cs](../../editor/Program.cs) supplies assets and bundled fonts. [EditorSceneTests](../../tests/Electron2D.Tests/EditorSceneTests.cs) links the exact source and checks live label/font identity, rendered pixels, native resize/close and borrowed-resource cleanup. The builder provides no persistence, project creation, inspector or capture operation.
