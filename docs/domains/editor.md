# Editor domain

Last updated: 2026-10-26

The editor is a separate desktop executable at [Electron2D.Editor.csproj](../../editor/Electron2D.Editor.csproj). It consumes the public runtime API and owns no runtime backend. The first slice is a persistent branded startup window, titled `Electron2D`, with a 1152×800 client area.

| Component | Types and integration | Current state |
| --- | --- | --- |
| [Shared project session](../components/project-session.md) | [ProjectSession](../classes/ProjectSession.md), [SessionClient](../classes/SessionClient.md), typed snapshots/requests/history in `Electron2D.Editor.Session.dll` | Shared owner/native views, guarded edits/undo, persistence/recovery and authenticated local attach |
| [Project jobs](../components/project-jobs.md) | [SessionProject](../classes/SessionProject.md), [SessionJob](../classes/SessionJob.md), [SessionScenario](../classes/SessionScenario.md) | Unsaved immutable build/run/capture, explicit native backends, stale attribution, cancellation/deadline/cleanup |
| [Agent workflow](../components/project-agent.md) | [SessionOperationsHost](../classes/SessionOperationsHost.md), [SessionOperationsClient](../classes/SessionOperationsClient.md), [ISessionAgentExecutor](../classes/ISessionAgentExecutor.md), [CodexAgentExecutor](../classes/CodexAgentExecutor.md) | Native task/conversation/control, shared selection/jobs, actual MCP and qualified real executor |
| [Editor startup](../components/editor-startup.md) | Internal [EditorScene](../classes/EditorScene.md), public Window, TextureRect, Label, FontFile, ResourceLoader and Engine.Run | Centered shaded character, live font-rendered title and descriptor, native resize/close and VS Code F5 configuration |

The runtime never references this assembly. Desktop targets follow [ADR 0021](../decisions/product.md#adr-0021); the current executable gate is Linux/Wayland. [ADR 0027](../decisions/product.md#adr-0027) owns the source/assembly boundary. [ADR 0090](../decisions/agent-native.md#adr-0090) governs shared authoring operations.

The [shared editor workflow](../components/project-jobs.md) now creates/opens a bounded source project, binds scene tree, position inspector, code/history and passive authored preview to one live session, and builds/runs immutable unsaved snapshots. Actual native captures retain exact input attribution and stale state. Failed opening preserves the current workspace; switching/shutdown preserves recoverable unsaved documents and retires jobs. The separate session library remains usable without UI.

[ADR 0090](../decisions/agent-native.md#adr-0090) stages 1–3 now execute for this bounded Linux binary-SDK/Codex profile. The real native-panel scenario verifies unsaved human/agent edits, stale conflict repair, actual capture, interruption and same-conversation reconnect. Read the [qualified profile and measured limits](../components/project-agent.md); this does not establish general agent productivity or human visual acceptance. General scene/resource operations, language services, arbitrary dependency closures, installed delivery, other platforms, concurrent agents and hot reload retain their own gates.

## Accepted delivery order

Stages 1–3 establish the bounded shared document owner, native authoring views, immutable job evidence and one actual executor through local operations and the task/conversation panel. Broader operations/context discovery, measured task success and installed/foreign-platform delivery follow. See [ADR 0090](../decisions/agent-native.md#delivery-and-acceptance) and the [coverage roadmap](../coverage/roadmap.md#live-project-and-agent-delivery). The measured real-agent task establishes only the stated bounded workflow; importer/exporter/plugin coverage, broader task productivity and human visual acceptance remain separate.
