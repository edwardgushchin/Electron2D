# ProjectSession

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`, outside `Electron2D.dll`.
- **Source:** [ProjectSession.cs](../../editor/Session/ProjectSession.cs), [persistence](../../editor/Session/ProjectSession.Persistence.cs).
- **Declaration:** `public sealed partial class ProjectSession : IDisposable`

Owns the first shared no-UI authoring model: one existing scene and one C# buffer. [The component](../components/project-session.md) defines lease, isolation, recovery, limits and verified failure boundaries. Only the creating thread may operate/dispose the owner. Ordinary trusted runtime reconstruction can execute user constructors/setup/notifications; no active scene/game loop or window is started.

## Example

```csharp
using var session = ProjectSession.Open(projectRoot, "scene.e2dscene", "Game.cs");
using var human = session.Connect("human");
using var agent = session.Connect("agent");
var source = human.InspectCode();
human.Apply("manual code", new SessionCodeEdit(source.ID, source.Revision, "// unsaved\n"));
var entity = agent.InspectScene().Nodes.First(n => n.Transform.HasValue);
var pose = entity.Transform!.Value;
pose.Origin = new Vector2(20, 30);
var edit = agent.Apply("move", new SessionTransformEdit(entity.ID, entity.Revision, pose));
agent.Undo(edit.OperationID); // preserves the unrelated unsaved manual source
```

Partial snippet: the caller supplies an existing projectRoot and imports Electron2D/Electron2D.Editor. The independent test consumer exercises this public operation path.

## Public members

| Signature | Contract |
| --- | --- |
| `public static ProjectSession Open(string projectRoot, string scenePath, string codePath)` | Acquire/recover the sole project owner for the existing document pair. |
| `public string ProjectRoot { get; }` | Canonical leased root directory. |
| `public event Action<SessionResult>? Changed` | Committed transitions in registration order. |
| `public SessionClient Connect(string origin)` | Create a trusted local capability with immutable attribution. |
| `public void Dispose()` | Release histories/lease, preserving recoverable unsaved documents. |

## Method and property descriptions

### Open

projectRoot is an existing directory; scenePath/codePath are distinct relative paths, outside `.electron2d`, with codePath ending `.cs`. Resolving outside the root or through symlinks rejects. Existing recovery must describe this same document pair and reconstruct its exact identity catalog; it never silently falls back after corruption. The owner holds an exclusive OS file lease until disposal/process death. IOException covers another owner or filesystem failure; invalid paths throw ArgumentException; invalid/oversize files or recovery reject before a writable session is returned. Loading uses registered typed factories with the ordinary trusted-code constraints. The returned instance is caller-owned.

### ProjectRoot

Returns the canonical absolute project directory. Queries require the owner thread and a live, nonfaulted owner.

### Connect

origin is nonblank and at most 128 characters. Returns a disposable [SessionClient](SessionClient.md) over the shared documents. It supplies attribution, not a network principal. Disposing a client does not close the owner. Blank/oversize origin rejects. No IPC or model provider is started.

### Dispose

Idempotent on an already disposed owner; otherwise requires the idle creating thread. Disposal during mutation/event delivery or from another thread rejects. Histories release their retained callbacks and the OS lease is released even if retirement fails. Committed dirty buffers/history remain in recovery for the next owner. Existing clients then reject operations with ObjectDisposedException.

## Event descriptions

### Changed

Receives [SessionResult](SessionResult.md) after one committed Apply/Undo/Redo/Save/Refresh (including an explicit no-op result). Mutations cannot reenter the event. Each registered observer is attempted; failures append diagnostics to the returned result. Observers see committed state. Subscriptions follow ordinary CLR lifetime and must be used on the session thread; owner disposal releases them. This is a synchronous no-UI core; stage 2 editor dispatch subscribes separately.

## Dependencies and verification

Uses public PackedScene/ResourceLoader/ResourceSaver, typed Node/Entity operations and UndoRedo. Internal storage/model helpers are documented in [the component](../components/project-session.md#storage-model). [ProjectSession.Tests](../../tests/ProjectSession/Program.cs) exercises independent processes, clients and actual persistence. The editor startup does not yet attach views, jobs or an agent; those gates remain open under ADR 0090.
