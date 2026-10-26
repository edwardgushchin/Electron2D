# SessionClient

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [SessionClient.cs](../../editor/Session/SessionClient.cs).
- **Declaration:** `public sealed class SessionClient : IDisposable`

A trusted in-process capability created by [ProjectSession.Connect](ProjectSession.md#connect). Operations require the owner thread; snapshots copy state. Construction is internal. See the [shared-session example](ProjectSession.md#example) and [failure/publication contract](../components/project-session.md).

## Public members

| Signature | Effect |
| --- | --- |
| `public string Origin { get; }` | Attribution supplied by the host. |
| `public SessionScene InspectScene()` | Snapshot all ordinary nodes, identities, revisions, poses and persisted-base observations. |
| `public SessionCode InspectCode()` | Snapshot unsaved source and persistence state. |
| `public IReadOnlyList<SessionOperation> InspectHistory()` | Copies retained operation IDs, labels, origins and applied states, including recovered groups. |
| `public SessionResult Apply(string label, params SessionEdit[] edits)` | Validate and durably publish one group without saving project files. |
| `public SessionResult Undo(Guid operationID)` | Guarded inverse of a retained applied group. |
| `public SessionResult Redo(Guid operationID)` | Guarded replay of a retained undone group. |
| `public SessionResult Save(SessionDocumentKind kind, ulong expectedRevision)` | Publish one project file with conflict checks and exact write diagnostics. |
| `public SessionResult Refresh(SessionDocumentKind kind, ulong expectedRevision, SessionExternalResolution resolution = SessionExternalResolution.Reject)` | Reconcile the persisted base and live document without writing a project file. |
| `public SessionCodeBuffer BindCodeBuffer(Func<SessionCodeEdit?> readPending, Action<SessionCodeEdit, Exception>? rejected = null)` | Bind one local pending-source reader before observations/mutations. |
| `public Node CreatePreview(ulong expectedSceneRevision)` | Return a caller-owned passive built-in scene projection at a fresh token. |
| `public SessionJob StartJob(SessionJobOptions options, ulong expectedSceneRevision, ulong expectedCodeRevision)` | Submit an immutable unsaved snapshot for asynchronous build/run. |
| `public void Dispose()` | Release this handle, leaving the session/documents open. |

## Property descriptions

### Origin

Immutable nonblank host attribution, at most 128 characters; recorded on each submitted operation/result.

## Method descriptions

### InspectScene

Returns [SessionScene](SessionScene.md). Nodes are copied into a read-only list; Transform is a nullable value copy. SHA-256 observation reports external changes/deletion. Unreadable, unsafe or oversized external files report HasExternalChange without hiding the live snapshot; a query does not implicitly refresh or save.

### InspectCode

Returns [SessionCode](SessionCode.md) with the actual unsaved source, not merely disk content. Exact text/line endings remain intact. Unreadable or oversized external files report HasExternalChange while the unsaved source remains inspectable; Refresh supplies the concrete read error.

### InspectHistory

Returns a read-only copied list of [SessionOperation](SessionOperation.md), in original publication order, at most 32 entries. Recovered clients can discover IDs for Undo/Redo without a stale external cursor.

### Apply

label is nonblank, up to 128 characters. edits contains 1–64 typed requests, copied before staging. Complete group validation includes IDs, expected revisions, ownership, finite transforms, names/hierarchy and strict UTF-8/size limits. A no-op returns an empty OperationID and unchanged revisions. Otherwise the result names one retained history group and fresh affected revisions. No user document is written. Invalid requests throw ArgumentException/NotSupportedException; stale overlaps throw SessionConflictException. Trusted runtime reconstruction/setter or recovery exceptions leave published buffers unchanged; arbitrary trusted external callback effects are outside this transaction. Up to the last 32 groups remain undoable; the recovery byte bound can retire older groups sooner.

### Undo

operationID must identify a retained applied group. Validates affected tokens before constructing/persisting an inverse. Later overlapping edits cause SessionConflictException. Unrelated object/code edits survive. Hierarchy groups use a conservative whole-scene token. Returns a new monotonic revision, never the history cursor version.

### Redo

operationID must identify a retained undone group. The same overlap/recovery rules apply before replay. Already applied/undone state mismatches throw InvalidOperationException; unknown/retired IDs throw ArgumentException.

### Save

kind identifies one scene/code document and expectedRevision must match that document's current token. Independent file changes/deletion reject with SessionConflictException. Writes use the existing authoring-save lifecycle for scenes and flushed same-directory replacement for source. Applying changes and saving are separate operations. Inspect FileWritten and Diagnostics: post-save or recovery finalization can fail after the file was written. A faulted owner must be disposed/reopened; recovery retains the live buffers. This method does not claim group atomicity across multiple files or power loss.

### Refresh

Requires a fresh token. Reject refreshes clean state and rejects unsaved overlap. UseDisk explicitly replaces live content with disk. KeepLive preserves live content while acknowledging disk as the persisted base. Missing/invalid files reject without replacing live state. Reconciliation advances document revision, retires affected history and persists recovery; it never saves a project file. Replacing scene content creates new node IDs; document ID remains stable. Source replacement preserves the observed disk BOM, while KeepLive retains the live buffer's BOM. There is no automatic text merge.

### BindCodeBuffer

readPending synchronously returns a typed source proposal with the view's observed token, or null. One ingress is admitted per owner. It must be side-effect-free, owner-thread and non-reentrant. The session drains it before reads, mutations, jobs/staleness and normal disposal so deferred native notifications cannot omit manual input. rejected receives a failed proposal and original exception before propagation; it must retain the visible draft for review. The returned binding is caller-owned; this client also disposes it. Invalid binding/reentry rejects with InvalidOperationException. See [SessionCodeBuffer](SessionCodeBuffer.md).

### CreatePreview

expectedSceneRevision must match the live scene. Returns a detached caller-owned duplicate of exact built-in Node/Entity/Polygon/Line/Sprite roles with the unsaved pose overlay. No project class/script/connection or gameplay role is attached; unsupported roles throw NotSupportedException. Ordinary archive construction still has trusted callbacks. This is static authored drawing, not actual game verification. Dispose the projection after removing its view. See [the preview boundary](../components/project-jobs.md#human-and-programmatic-views).

### StartJob

options names one qualified root .csproj and binary SDK. Both expected tokens must be fresh; external scene/code changes reject. Pending manual text is drained before token checks, so callers must inspect after visible input. Returns caller-owned SessionJob without waiting for compilation/execution. One active job is admitted. Timing, files, dependency-profile and disk admission limits are explicit in [shared jobs](../components/project-jobs.md#immutable-input-and-project-profile). Undefined inputs reject; platform/control/profile failures never fall back silently. Compilation/failure/cancel/deadline become terminal structured results. Cancel then await DisposeAsync before removal/shutdown. Authoring continues and IsStale reports later document changes.

### Dispose

Releases only this capability. Subsequent methods throw ObjectDisposedException; disposal is idempotent. It does not discard unsaved buffers or unsubscribe session-wide observers.

## Shared errors and verification

Owner operations reject wrong-thread access, mutation reentry, disposed owners and a faulted persistence boundary. [Executable checks](../../tests/ProjectSession/Program.cs) cover these rules and cross-process recovery. [Native editor views and immutable jobs](../components/project-jobs.md) use the same clients. Transport/authentication and actual agent acceptance remain separate stages.

## Shared selection and job inspection

- `SessionSelection InspectSelection()` copies owner-local selection after pending source is flushed.
- `SessionResult SelectNode(Guid nodeID, ulong expectedSceneRevision, ulong expectedSelectionRevision)` checks both observed tokens; empty nodeID clears selection. Unknown objects/stale tokens reject. Selection changes neither document revision, dirty state nor history. Native views and operations use the same selection.
- `IReadOnlyList<SessionJob> InspectJobs()` returns a copied submission-ordered list of borrowed live-owner job handles, including jobs submitted by external clients. Job objects remain owned by their submitters/host. Terminal RemoveResult unregisters the handle; disposing a client does not clear shared results. Jobs are not reconstructed across owner lifetimes.

See [SessionSelection](SessionSelection.md) and [the agent workflow](../components/project-agent.md).
