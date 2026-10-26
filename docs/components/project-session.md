# Shared project session

Last updated: 2026-10-26

## Scope and dependency direction

[ProjectSession](../classes/ProjectSession.md) is the first no-UI authoring core selected by [ADR 0090](../decisions/agent-native.md#adr-0090). `editor/Session/Electron2D.Editor.Session.csproj` builds `Electron2D.Editor.Session.dll`, separately from the startup application and `Electron2D.dll`. It references only public runtime APIs. The editor UI and immutable jobs consume this library through the shared operation boundary. The runtime does not reference it.

One owner opens one existing typed scene and one strict UTF-8 `.cs` file. Trusted local [SessionClient](../classes/SessionClient.md) handles inspect the same unsaved state and submit transform, rename, reparent and complete-source-buffer operations. This bounded document boundary now backs the [editor workflow and snapshot jobs](project-jobs.md). Language services remain separate; [authenticated local agent transport](project-agent.md) now consumes the same boundary.

## Shared views and pending input

[SessionCodeBuffer](../classes/SessionCodeBuffer.md) binds one synchronous local pending-code reader. Before observations, mutations, job snapshots/staleness and normal owner disposal, the session publishes visible pending text through a guarded SessionCodeEdit. Readers cannot mutate/reenter/dispose their binding. Rejected proposals reach a client callback before the original error propagates, so the view retains a draft for explicit review instead of overwriting it. No edit is a no-op. Disposing a binding unregisters it; clients flush before retiring a view. Unix recovery metadata uses owner-only directory access.

## Ownership, identities and commit boundary

`ProjectSession.Open` holds an exclusive `FileStream` lease on the stable `.electron2d/session/owner.lock` inode for its lifetime. It never unlinks that inode. Another process or local owner fails explicitly; a live but unreachable owner does not cause a writable fallback. Process death releases the OS lease. A second no-UI host must use the same canonical project root. Input/metadata symbolic links, outside-root documents, metadata documents and identical scene/code paths reject. This is cooperative local ownership, not a security boundary against a process that ignores the lease or changes filesystem paths concurrently. Local client handles are capabilities granted by the host. The [local operations transport](project-agent.md) now authenticates external clients over this same owner; it never falls back to a second writer.

All operations, queries and owner disposal use the creating thread. No model/network work or game loop runs there. Job submission captures document values and bounded filesystem inventories; compilation/execution runs asynchronously outside the owner. Mutations and disposal reject reentry while staging or notifying. Snapshots contain copied value data and read-only node lists, never mutable engine references. Events run in registration order after a committed transition. Observer failures are returned as `observer-failed` diagnostics; they cannot turn a committed edit into an apparent rollback.

Document GUIDs, node GUIDs and revisions live in the recovery record. GUIDs survive owner restart, rename and reparent. Revisions use a checked increasing session clock, including undo/redo. Unrelated source edits do not invalidate object tokens; independent object-property edits can continue. A hierarchy group conservatively guards the complete scene. External UseDisk scene refresh creates new object identities and retires scene-dependent history instead of guessing correspondence from names or paths.

An Apply group copies 1–64 typed requests, checks every observed token and request, reconstructs a private detached scene when needed, applies public runtime setters, packs through the typed archive and persists the next recovery record before publishing it in memory. Construction, setter, capture or recovery failure leaves published document content and revisions unchanged. Trusted constructors, stored-property restoration, local-resource setup, scene-instantiation notifications and setters can execute; external side effects of arbitrary trusted callbacks are not rolled back. No SceneTree is created and no EnterTree/Ready/process loop or native window is activated.

## History and selective undo

Each retained group owns a real public runtime UndoRedo action. The session validates overlapping tokens and prepares the inverse/replay and recovery record before the action publishes its prepared state. `UndoRedo.Version` is never a concurrency token. Up to the last 32 groups survive restart as typed data, with callbacks recreated locally, not serialized. New work can proceed on unrelated objects/code while a retained group is undone. A later overlapping mutation rejects undo/redo rather than restoring an older whole document over it. Source-buffer replacement guards the whole source document; this slice has no automatic text merge.

Rename groups preserve identity, reject ambiguous sibling names and restore simultaneous name exchanges through private temporary names. Reparent uses the runtime's keep-global-transform policy, rejects cycles/root movement and restores parent/order/pose on guarded undo. Only the root and nodes whose Owner is that root are editable; linked/inherited authoring needs its owning runtime capabilities. Other reconstructed nodes can be inspected.

## Persistence and recovery

Apply writes only session metadata, never the user's scene or source. Dirty snapshots and both unsaved buffers are shared immediately. Code keeps exact text/line endings and the original UTF-8 BOM policy. Invalid UTF-8, unpaired surrogates and oversize content reject. Scene graphs/resources use PackedScene, ResourceLoader and ResourceSaver, with bundled isolated archive data. Identity metadata includes exact Entity transform overlays because stored component reconstruction can round floats; it does not implement another scene/resource serializer. Project files retain the runtime archive's normal stored-component and RESET/save projection semantics.

Save publishes **one** requested document at a fresh revision, after checking its persisted SHA-256 base against disk and ensuring recovery is present. Code uses a same-directory flushed temporary file and replacement, retaining existing Unix permissions. Scene uses ResourceSaver.SaveScene and its pre-save/RESET/write/post-save lifecycle on the private scene, preserving live authoring data. Its private encoded copy gets its own UID; the project file's UID is never reassigned. A changed file after failed post-save delivery returns FileWritten with diagnostics. Failure to observe/finalize a possibly written file faults the owner and returns an explicit reopen-required diagnostic; callers must inspect the result.

There is no multi-file atomic SaveAll. A caller saves documents separately. Recovery preserves the other unsaved buffer if the process dies between publications, or even after a scene file was written but before its persisted-base receipt was updated. On reopen, the old recovery and independently changed file remain explicit. Refresh with Reject updates clean documents only; UseDisk explicitly discards live changes; KeepLive keeps the buffer and acknowledges the current file as its new base. Each resolution needs the current document revision. Deleted/unreadable/oversized files remain conflicts and are not silently recreated; the live buffer stays inspectable. Refresh never writes a project document.

The version-one `.electron2d/session/state.json` is replaced from a flushed temporary file. `encode.e2dscene` and `decode.e2dscene` are bounded private scratch files; the recovery record is authoritative if scratch content differs. Invalid recovery does not silently fall back to stale disk files. Recovery tests cover process crashes, not sudden power loss/filesystem directory durability. Direct hostile filesystem races and side effects of custom savers/callbacks require their own isolation policy.

Limits are 4096 scene nodes, 16 MiB encoded scene, 4 MiB UTF-8 source including BOM, 64 MiB recovery envelope, 32 retained groups, 64 requests/group and 128-character origin/history labels. Older groups retire automatically to fit the 64 MiB envelope as well as the 32-group limit; if the live documents and newest group still exceed it, the edit fails before publication. This cold allocating authoring path makes no frame allocation or large-project latency claim.

## Owned implementation types

| Types | Responsibility |
| --- | --- |
| [ProjectSession](../classes/ProjectSession.md), [SessionClient](../classes/SessionClient.md), [SessionCodeBuffer](../classes/SessionCodeBuffer.md) | Owner lifetime, shared operations, client attribution, snapshots and publication |
| [SessionScene](../classes/SessionScene.md), [SessionCode](../classes/SessionCode.md), [SessionNode](../classes/SessionNode.md) | Immutable document/object observations |
| [SessionEdit](../classes/SessionEdit.md), [SessionTransformEdit](../classes/SessionTransformEdit.md), [SessionRenameEdit](../classes/SessionRenameEdit.md), [SessionReparentEdit](../classes/SessionReparentEdit.md), [SessionCodeEdit](../classes/SessionCodeEdit.md) | Bounded typed authoring requests |
| [SessionDocumentKind](../classes/SessionDocumentKind.md), [SessionExternalResolution](../classes/SessionExternalResolution.md), [SessionChangeKind](../classes/SessionChangeKind.md) | Document, reconciliation and event vocabulary |
| [SessionOperation](../classes/SessionOperation.md) | Copied/discoverable retained history IDs, labels and applied state |
| [SessionResult](../classes/SessionResult.md), [SessionDiagnostic](../classes/SessionDiagnostic.md), [SessionConflictException](../classes/SessionConflictException.md) | Attribution, exact commit/write outcome and useful conflict identity |

<a id="storage-model"></a>
Internal SessionStorage owns file/path/size/encoding validation, the writer lease and replacement. Internal DocumentState is the versioned recovery envelope; NodeState records stable identity, hierarchy/order, exact pose and token; HistoryState records bounded typed before/after values and overlap tokens. SessionJSON is the source-generated strict JSON metadata context, with no reflection-based runtime invocation. The nested HistoryLease disposes an uncommitted UndoRedo or transfers it to the owner. These helpers stay inside the tooling assembly.

## Executable evidence and remaining work

`dotnet run --project tests/ProjectSession/ProjectSession.Tests.csproj -c Release` exercises two clients, selective/overlap history, atomic failed groups, callback/reentry failures, exact affine/singular recovery, rename/reparent, code encodings/bounds, external reconciliation, failed apply/save, independent owner exclusion, killed owner recovery, recovered grouped undo and fresh-process scene/source persistence. A second process is killed at the actual scene post-write boundary and then reconciles/re-saves recovered state. No native window is created.

Linux/.NET 10 is the exercised owner/persistence platform. [Native views and snapshot jobs](project-jobs.md) and [authenticated MCP/real executor co-development](project-agent.md) consume this same core. Other platforms, owner visual acceptance, installed packaging, large-project performance and power-loss durability remain separate gates. This core adds no runtime declaration and promotes no absent editor/plugin API row.
