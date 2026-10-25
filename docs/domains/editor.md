# Editor domain

Last updated: 2026-10-26

The editor is a separate desktop executable at [Electron2D.Editor.csproj](../../editor/Electron2D.Editor.csproj). It consumes the public runtime API and owns no runtime backend. The first slice is a persistent branded startup window, titled `Electron2D`, with a 1152×800 client area.

| Component | Types and integration | Current state |
| --- | --- | --- |
| [Shared project session](../components/project-session.md) | [ProjectSession](../classes/ProjectSession.md), [SessionClient](../classes/SessionClient.md), typed snapshots/requests/history in `Electron2D.Editor.Session.dll` | No-UI owner, two live clients, guarded edits/undo, persistence/recovery; GUI/jobs/transports remain absent |
| [Editor startup](../components/editor-startup.md) | Internal [EditorScene](../classes/EditorScene.md), public Window, TextureRect, Label, FontFile, ResourceLoader and Engine.Run | Centered shaded character, live font-rendered title and descriptor, native resize/close and VS Code F5 configuration |

The runtime never references this assembly. Desktop targets follow [ADR 0021](../decisions/product.md#adr-0021); the current executable gate is Linux/Wayland. [ADR 0027](../decisions/product.md#adr-0027) owns the source/assembly boundary. [ADR 0090](../decisions/agent-native.md#adr-0090) governs shared authoring operations.

The startup application has no project manager, scene inspector or session-bound authoring views. The separate no-UI session library now supplies bounded shared authoring operations. The startup screen stays visible until the window closes. Packaging and other desktop platforms require their own checks; this screen does not complete the editor or agent-native workflow.


The separate [compiled project batch tool](../components/project-batch.md) currently supplies create/open/build, typed detached scene save/load, headless execution and native capture with bounded jobs and structured diagnostics. Its [BatchHost](../classes/BatchHost.md), [BatchScenario](../classes/BatchScenario.md), [BatchContext](../classes/BatchContext.md) and [BatchMode](../classes/BatchMode.md) consume public runtime APIs. It has no shared editor session or unsaved-buffer integration; its controller/SDK/formats are not a compatibility requirement for the replacement workflow.

## Accepted direction and implementation order

[ADR 0090](../decisions/agent-native.md#adr-0090), revised on 2026-10-25, selects one live project session for human and agent authoring, plus task/conversation/control inside the editor with a replaceable agent executor. Session operations own unsaved documents, stable identities, revisions/conflicts, events and grouped history. Editor views and programmatic clients share those operations; the session core remains usable without UI. Agent-control protocols and engine tools have separate adapters, and the runtime stays independent of both.

The [bounded no-UI session](../components/project-session.md) now implements the first project-authoring foundation, before project management, inspector or code editing gains a private UI-only document model. The first functional editor workflow binds minimal views to it and adds jobs over immutable unsaved snapshots; that slice replaces/removes the existing Batch tool contract without a compatibility bridge. One real agent and the in-editor panel then establish co-development before editor polish. Installed distribution, broader tools/executors/platforms, concurrent agents and hot reload follow their own gates. See [delivery stages](../decisions/agent-native.md#delivery-and-acceptance) and the [coverage roadmap](../coverage/roadmap.md#live-project-and-agent-delivery).

The bounded live scene/code session is implemented and tested through independent processes and two local clients. Shared views, snapshot jobs, MCP transport, ACP/executor adapter and conversation panel remain unimplemented. The startup screen, batch checks and this architectural record do not establish live collaboration. Import/export, installed SDK packaging and editor/plugin API families retain their exact implementation prerequisites.
