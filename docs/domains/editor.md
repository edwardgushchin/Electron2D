# Editor domain

Last updated: 2026-10-05

The editor is a separate desktop executable at [Electron2D.Editor.csproj](../../editor/Electron2D.Editor.csproj). It consumes the public runtime API and owns no runtime backend. The first slice is a persistent branded startup window, titled `Electron2D`, with a 1152×800 client area.

| Component | Types and integration | Current state |
| --- | --- | --- |
| [Editor startup](../components/editor-startup.md) | Internal [EditorScene](../classes/EditorScene.md), public Window, TextureRect, Label, ResourceLoader and Engine.Run | Centered approved dark stacked logo, native resize/close and VS Code F5 configuration |

The runtime never references this assembly. Desktop targets follow [ADR 0021](../decisions/product.md#adr-0021); the current executable gate is Linux/Wayland. [ADR 0027](../decisions/product.md#adr-0027) owns the source/assembly boundary. [ADR 0090](../decisions/agent-native.md#adr-0090) still governs future shared authoring operations.

There is no project manager, scene inspector, authoring operation, CLI or rendered-batch command in this slice. The startup screen stays visible until the window closes. Packaging and other desktop platforms require their own checks; this screen does not complete the editor or agent-native workflow.
