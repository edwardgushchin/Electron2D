# Editor domain

Last updated: 2026-10-26

The editor is a separate desktop executable at [Electron2D.Editor.csproj](../../editor/Electron2D.Editor.csproj). It consumes the public runtime API and owns no runtime backend. The first slice is a persistent branded startup window, titled `Electron2D`, with a 1152×800 client area.

| Component | Types and integration | Current state |
| --- | --- | --- |
| [Shared project session](../components/project-session.md) | [ProjectSession](../classes/ProjectSession.md), [SessionClient](../classes/SessionClient.md), typed snapshots/requests/history in `Electron2D.Editor.Session.dll` | Shared owner and native views, guarded edits/undo, persistence/recovery; transport remains absent |
| [Project jobs](../components/project-jobs.md) | [SessionProject](../classes/SessionProject.md), [SessionJob](../classes/SessionJob.md), [SessionScenario](../classes/SessionScenario.md) | Unsaved immutable build/run/capture, explicit native backends, stale attribution, cancellation/deadline/cleanup |
| [Editor startup](../components/editor-startup.md) | Internal [EditorScene](../classes/EditorScene.md), public Window, TextureRect, Label, FontFile, ResourceLoader and Engine.Run | Centered shaded character, live font-rendered title and descriptor, native resize/close and VS Code F5 configuration |

The runtime never references this assembly. Desktop targets follow [ADR 0021](../decisions/product.md#adr-0021); the current executable gate is Linux/Wayland. [ADR 0027](../decisions/product.md#adr-0027) owns the source/assembly boundary. [ADR 0090](../decisions/agent-native.md#adr-0090) governs shared authoring operations.

The [shared editor workflow](../components/project-jobs.md) now creates/opens a bounded source project, binds scene tree, position inspector, code/history and passive authored preview to one live session, and builds/runs immutable unsaved snapshots. Actual native captures retain exact input attribution and stale state. Failed opening preserves the current workspace; switching/shutdown preserves recoverable unsaved documents and retires jobs. The separate session library remains usable without UI.

[ADR 0090](../decisions/agent-native.md#adr-0090) stage 2 is implemented for this bounded binary-SDK profile. Stage 3 still requires actual operations transport/authentication, a real executor and the in-editor task/conversation/control panel. Local clients, native views and process results do not establish agent co-development. General scene/resource operations, language services, arbitrary dependency closures, installed delivery, other platforms, concurrent agents and hot reload retain their own gates.

## Accepted delivery order

Stages 1–2 establish the bounded shared document owner, native authoring views and immutable job evidence. Stage 3 connects actual local operations transport and one real executor to the editor's task/conversation panel before further UI polish. Broader operations/context discovery, measured task success and installed/foreign-platform delivery follow. See [ADR 0090](../decisions/agent-native.md#delivery-and-acceptance) and the [coverage roadmap](../coverage/roadmap.md#live-project-and-agent-delivery). Native tests and local clients do not establish live agent collaboration, importer/exporter/plugin coverage or human visual acceptance.
