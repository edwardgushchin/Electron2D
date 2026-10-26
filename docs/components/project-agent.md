# Shared project agent workflow

Last updated: 2026-10-26

## Executable boundary

[ADR 0090](../decisions/agent-native.md#adr-0090) stages 1–3 now connect one [shared owner](project-session.md), [native views and immutable jobs](project-jobs.md), authenticated local operations, and a real replaceable executor. Everything here lives in `Electron2D.Editor.Session.dll` and the separate editor application. `Electron2D.dll` has no model/provider dependency. The qualified profile is Linux/.NET 10, Unix sockets, MCP stdio 2025-06-18/2025-11-25, and **codex-cli 0.163.0** using its existing login. Other executor versions/providers, ACP, remote connections and foreign platforms are not qualified.

Open/create the source project, select the **Agent** tab beside Inspector, connect, enter a task and Send. The panel attaches observed document/selection tokens; the executor refreshes actual unsaved context through operations. Scene tree, inspector, code, guarded history and snapshot results share the existing owner. The passive authored preview does not execute C# callbacks; Inspector's actual run/capture view shows game output. Selection is owner-local context: it changes neither document revision, dirty state nor history. Jobs submitted through UI or transport share a submission-ordered live-owner registry; terminal removal unregisters the handle. Selection and job handles are not reconstructed across owner lifetimes; retained job folders remain generated evidence.

The panel shows literal bounded conversation, owner-confirmed operation successes/failures, executor status/model, shared selection, and task/interruption outcomes. Inspector shows real job progress, terminal diagnostics and captures with input/stale attribution. Normal history lists attributed human/external groups. **Undo last agent change** uses the same guarded inverse and refuses overlapping later work. Conversation output is limited to 64 KiB; source and operation history remain authoritative. No imported text, source or executor message can expand permissions.

## Owner, discovery and permissions

`ProjectSession.StartOperations(project, sdk)` starts one [SessionOperationsHost](../classes/SessionOperationsHost.md) over this exact existing owner/document pair. The creator calls `Pump` on the owner thread; socket workers only frame/authenticate/queue requests. No hidden writable owner/thread is created. Private `.electron2d/session/operations.json` identifies protocol version one, fresh owner generation, a random capability and an owner-private Unix socket. Discovery/socket permissions are 0600 and the temporary socket directory is 0700. Unsafe project paths/symlinks, world/group-readable discovery, invalid generations and forged/nonnamed capabilities reject. Metadata is secret and must not enter logs or source control.

Default discovery rights allow semantic authoring and jobs, **without file publication or discarding unsaved buffers**. `GrantClient` issues a named token with explicit Save/UseDisk rights. The editor's unchecked **Allow agent to save files** and **Allow agent to reload disk** controls select these rights before connection; they are disabled while connected. Reconnect issues fresh rights and revokes the old capability. The actor name is bound by authentication and external origins receive an owner-generated prefix/connection identity. Save publishes one file and returns publication diagnostics; UseDisk requires discard rights even when a model requests it.

This remains cooperative local ownership. Another process running as the same OS user can read that user's files or ignore the project lease. The executor filesystem policy is read-only, not a restricted-read sandbox. Project constructors, serialization callbacks, builds and game code are trusted; arbitrary game code is not sandboxed by an MCP token. No remote/TCP server or general string-based runtime member invocation is exposed.

## Typed operations and limits

MCP `tools/list` exports version-matched operation schemas from the actual typed transport models. `context` returns current document IDs/revisions, dirty/external state, selection and the operation catalog. `scene_read` pages at most 128 stable typed objects; `code_read` pages at most 8192 UTF-16 characters and preserves surrogate boundaries. This is discovery of the supported authoring operations, not a claim of runtime-wide semantic API search.

| Operation | Executed contract |
| --- | --- |
| `context`, `scene_read`, `code_read` | Copy current unsaved state after flushing pending native source. |
| `scene_apply` | One attributed group, 1..64 typed position/transform/rename/reparent edits. Position preserves basis; reparent checks the whole scene token. |
| `code_edit` | Replace one UTF-16 range at the observed document/revision; insertion max 65536 characters. No implicit save. |
| `history`, `undo`, `redo` | At most 32 retained groups; guarded inversion/replay preserves later unrelated work and rejects overlap. |
| `selection` | Guard both observed scene and selection tokens; empty GUID clears context. |
| `save`, `refresh` | Shared per-file publication/external reconciliation with explicit publication/discard rights. |
| `job_start`, `jobs`, `job_status`, `job_cancel` | Freeze unsaved input/qualified SDK, build/run, inspect actual progress/results and cancel owned execution. Queue acknowledgement is not success. |
| `job_data` | Page inputs, observations, assertions or diagnostics, at most 128 entries. |
| `job_artifact` | Return a terminal validated PNG by owned job/index, with digest and snapshot/document/stale attribution; never accept an arbitrary path. |

Wire requests/responses use length-prefixed bounded JSON (eight MiB), one outstanding request per connection, at most 16 connections and 128 queued owner requests. Structured errors include `CONFLICT` with document/object and expected/actual tokens, `PERMISSION_DENIED`, `INVALID_ARGUMENT` and `CANCELLED`. Image transport accepts at most four MiB, verifies the artifact hash/path and preserves exact snapshot identity. MCP returns actual image content plus provenance, avoiding duplicate base64 in text/structured output. Other logs/artifacts remain owned file references; job data is paged. Runtime API docs and backend limits remain in their class/component pages.

## Programmatic and no-UI paths

The [user workflow contract](../decisions/agent-native.md#user-workflows-and-editor-independent-access) includes both an agent inside the editor and an external client such as Codex. The existing transport supports attachment to either a native workspace owner or the explicit no-UI owner below. Its schemas, unsaved state, revision checks, history and jobs are shared. A local external conversation can therefore use the tools without an editor window; a running owner is still required.

An already open native workspace publishes the endpoint. A client attaches through [SessionOperationsClient](../classes/SessionOperationsClient.md); it never opens a fallback owner. The following partial example requires an existing pumping owner and projectRoot:

```csharp
await using var client = await SessionOperationsClient.ConnectAsync(projectRoot, "automation");
using var empty = System.Text.Json.JsonDocument.Parse("{}");
var response = await client.InvokeAsync("context", empty.RootElement);
```

An explicit no-UI owner is available from a qualified session build/SDK:

```bash
dotnet Electron2D.Editor.Session.dll --session-host /absolute/source/project /absolute/qualified/sdk
```

It acquires the same owner lease and pumps on its main thread. Stdin EOF or Ctrl-C retires it; another host cannot replace an active writer. Attach an MCP stdio client with:

```bash
dotnet Electron2D.Editor.Session.dll --session-mcp /absolute/source/project actor-name
```

The bridge negotiates the exercised MCP versions, tool schemas and live-owner calls. `ELECTRON2D_OPERATIONS_CAPABILITY` privately supplies a named token when issued by the host; otherwise private discovery supplies default rights. Never print/store the token in task text. Stdin EOF/cancel disconnects the client and cancels pending ingress/jobs; committed groups remain recoverable. These entry points are actual commands in the separate session executable; they are not an installed-distribution claim.

For a local Codex CLI, register the bridge as a stdio MCP server using resolved absolute paths to the qualified session assembly and source project:

```bash
codex mcp add electron2d -- dotnet /absolute/session-build/Electron2D.Editor.Session.dll --session-mcp /absolute/source/project external-codex
```

This is a setup example, not a command that this documentation executes. Start the owner first, keep its stdin open, and verify the tools in the client before assigning work. The MCP bridge only attaches; it does not start an owner or acquire the writer lease. With the editor already open, use its endpoint instead of starting another owner. Codex MCP configuration is described in the [official documentation](https://learn.chatgpt.com/docs/extend/mcp).

The explicit host currently issues default discovery rights, so an attached agent can author and run jobs but cannot `save` or discard live buffers with `UseDisk`. Those require a named capability explicitly granted by a host integration; the editor exposes the controls described above. No-UI edits remain dirty and recoverable on host shutdown. To continue in the native editor today, retire the no-UI host first and reopen the same project to recover its documents, review and save. Opening another writable owner while the host is alive is rejected; seamless UI attachment/owner handoff is not implemented.

No-UI authoring does not load the editor's window/assets or require an active renderer. A headless simulation job checks behavior without images. A rendered job runs a separate native game process and can open a game window; it needs the selected renderer/display environment even though the editor stays closed. The existing capture path must not be described as display-free rendering.

The qualified real-model acceptance below covers the in-editor Codex adapter. Transport/public-consumer checks establish the no-UI tools path; an ordinary external Codex CLI session in both owner modes has not received a separate real-model acceptance claim. No-UI publication-rights setup, automatic owner startup, installed setup, spatial viewport/image context and a general composition interface remain future work under stage 4. The current executor's code-mode tool routing is not a new general engine scripting API.

## Executor, cancellation and failure

[ISessionAgentExecutor](../classes/ISessionAgentExecutor.md) separates conversation/control from project semantics. [CodexAgentExecutor](../classes/CodexAgentExecutor.md) launches the installed app-server under the existing owned process worker, verifies exact CLI version/login/read-only policy and all 17 MCP operations, and rejects unrelated active MCP sources. It uses a private cwd outside the repository and per-process configuration: shell/apps/plugins/memory/delegation/hooks/web are disabled; the CLI's code-mode tool router stays enabled because real MCP calls require it. Global configuration and credentials are unchanged. The CLI chooses its configured model; the panel and acceptance receipt record the actual choice. No API key is created/read/copied by the editor.

Setup and model IO run asynchronously; native controls are updated only by owner-thread polling. Missing CLI/login/version/tool capability and unexpected disconnection produce explicit failures. Prompt max is 16384 UTF-16 characters (14000 user characters in the panel, leaving context space), protocol request timeout is 30 seconds, with at most 32 outstanding RPCs. Unsupported external approval/forms are declined/reported; further ordinary instructions can be sent in the conversation. The profile does not advertise interactive approval forms or direct shell/filesystem authoring.

**Stop** cancels undispatched operations and capability-owned jobs, blocks later mutations under that capability while retaining reads, then requests real turn interruption and waits for its terminal event. Submitting a new task calls ResumeClientWork only after the executor is idle, cancelling late queued requests before reopening mutation admission. A running synchronous authoring operation cannot be rolled back by cancelling its response. Committed edits remain in shared history; later manual work survives. A lost submission acknowledgement is an unknown outcome: inspect shared state/history before retrying. Unexpected executor connection failure revokes semantic rights before the next Pump; failed turns cancel owned jobs. Disconnect revokes rights and retires owned processes/pipes; reconnect resumes the same conversation for the same project using a new capability. Switching/closing retires the executor/endpoint and awaits owned jobs/processes outside native frame work. Uncertain publication exposes IsFaulted, stops edits and permits safe close/reopen after handling an unapplied visible draft; lease release still occurs when endpoint cleanup fails.

## Verification and measured limits

[ProjectOperations](../../tests/ProjectOperations/Program.cs) executes the shared path without native editor UI: independent MCP negotiation/tools/errors/EOF, pending manual source, stale conflict/history, selection, Unicode ranges, forged/named/revoked rights, queued stop, missing/unqualified executor, reconnect/disconnect and recovery. Its [job checks](../../tests/ProjectOperations/JobChecks.cs) run an unsaved rendered snapshot, inspect assertions and actual PNG digest/provenance/staleness, observe a real compiler failure, and remove owned results. The [faulted-workspace check](../../tests/ProjectWorkspace/FaultedWorkspaceChecks.cs) forces a post-publication recovery failure and verifies status/close/reopen.

The [native real-agent scenario](../../tests/ProjectWorkspace/AgentTests.cs), explicitly invoked with `--agent <new-output-directory> gpu`, uses actual task controls and **gpt-6.1-sol through codex-cli 0.163.0**, not a fake executor. The measured completed run passed **18 assertions, 7672 software Vulkan frames in 138.6 seconds**: human edits before/during authoring, a real stale source conflict and reread/repair, Square moved to (80,40), unsaved C# LimeGreen output, real two-step PNG/assertions, later stale attribution, cancellation of a million-step job preserving manual work, and same-conversation disconnect/reconnect followed by actual fresh reads. Disk scene/code remained unchanged. One scene-argument repair and one stale-code repair occurred; the human edits and interrupt were scripted interventions. This is one fixed synthetic task, not a comparative productivity benchmark.

Ignored evidence is under `bin/editor-agent-workflow/2026-10-26`: real-native-stop-gate receipt/conversation, actual agent-panel/game PNGs, manifests/results and failure/regression logs. The receipt records the exact conversation/model, job/snapshot identities and SHA-256; no capability token or account secret is included. Human assessment of game feel/visual quality, physical GPU/input/IME, native heap/performance, installed/foreign-platform delivery, other executors/protocols, general language services and broader semantic/API discovery remain separate gates. This tooling slice promotes no runtime census declaration.

## Owned implementation types

Public types: [SessionSelection](../classes/SessionSelection.md), [SessionOperationsHost](../classes/SessionOperationsHost.md), [SessionOperationsClient](../classes/SessionOperationsClient.md), [SessionOperationActivity](../classes/SessionOperationActivity.md), [ISessionAgentExecutor](../classes/ISessionAgentExecutor.md), [CodexAgentExecutor](../classes/CodexAgentExecutor.md), [SessionAgentStatus](../classes/SessionAgentStatus.md), [SessionAgentEventKind](../classes/SessionAgentEventKind.md) and [SessionAgentEvent](../classes/SessionAgentEvent.md). ProjectSession/SessionClient own the shared selection/registry extension.

Private operation-specific records in OperationModels/OperationWire carry bounded typed queries/results, endpoint/handshake and errors; OperationCatalog exports their schemas. SessionMCP/JSONLines implement bounded stdio; SessionOperationHostEntry owns explicit no-UI pumping. AgentRPC handles bounded app-server JSON-RPC and unsupported interactions. Internal AgentPanel owns the native task/conversation/permission controls; ProjectWorkspace binds its host to shared views/jobs. ProcessWorker's interactive mode relays protocol stdin and retires the inherited process group when its owner disappears. No provider package, second scene model or general orchestration framework is added.
