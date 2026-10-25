# SessionProject

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [SessionProject.cs](../../editor/Session/SessionProject.cs).
- **Declaration:** `public sealed class SessionProject`

An immutable description of one source project. Opening validates paths/version and existence without acquiring an authoring lease or starting gameplay. The [job component](../components/project-jobs.md) defines the bounded binary-SDK dependency profile and Linux job owner. This description owns no disposable runtime resource.

## Example

```csharp
var project = SessionProject.Open(projectDirectory);
using var session = project.OpenSession();
using var client = session.Connect("local tool");
var source = client.InspectCode();
```

Partial snippet: the caller supplies an existing version-two projectDirectory and imports Electron2D.Editor. [Executable consumers](../../tests/ProjectJobs/Program.cs) also create projects and run unsaved snapshots.

## Public members

| Signature | Contract |
| --- | --- |
| `public static SessionProject Open(string directory)` | Validate an existing description. |
| `public static SessionProject Create(string directory, string sdkDirectory)` | Create a minimal project in a new directory. |
| `public ProjectSession OpenSession()` | Acquire/recover the shared scene/code owner. |
| `public string Root { get; }` | Canonical absolute project directory. |
| `public string Name { get; }` | Manifest display name. |
| `public string ProjectFile { get; }` | Relative executable .csproj. |
| `public string SceneFile { get; }` | Relative typed scene. |
| `public string CodeFile { get; }` | Relative C# source. |

## Property descriptions

### Root

Canonical absolute directory. Automatic inputs must stay inside it and cannot traverse symbolic links.

### Name

Nonblank display name, at most 128 characters. Created projects use the destination directory name.

### ProjectFile

Relative .csproj. Snapshot submission currently requires one ordinary root-level framework-dependent net10.0 executable; a description alone does not qualify arbitrary build dependencies.

### SceneFile

Relative typed scene. OpenSession validates/reconstructs it through ordinary public archive APIs.

### CodeFile

Relative .cs buffer, distinct from the scene. The owner enforces strict UTF-8 and document size limits.

## Method descriptions

### Open

Reads the bounded version-two electron2d.project.json, validates name/extensions/distinct documents, inside-root paths and required files. Missing/invalid manifests, links and outside-root paths reject with filesystem/invalid-data exceptions. It neither creates a writer nor launches code. Version-one legacy projects require explicit migration rather than an adapter.

### Create

Requires a new directory and a qualified sdkDirectory containing the runtime/session binaries, session runtime configuration and target native closure. Existing paths reject without overwrite. Creates into a temporary sibling then moves the complete directory into place. Files include a minimal C# SessionScenario, direct binary-SDK references, manifest, typed Entity/Polygon scene and .gitignore. Failure attempts temporary cleanup; ordinary parent-directory creation remains a filesystem effect. The generated project uses public APIs and renders a square; jobs freeze unsaved modifications independently of these disk files.

### OpenSession

Returns a caller-owned ProjectSession over SceneFile/CodeFile. Another live owner rejects. Recovery, trusted constructors/setup, strict data validation and owner-thread rules follow [ProjectSession](ProjectSession.md). Dispose the owner to release its lease; the description remains usable.

## Verification and limits

Create/open/build/run/failure/switch/recovery execute in [ProjectJobs](../../tests/ProjectJobs/JobContracts.cs) and [native application tests](../../tests/ProjectWorkspace/ApplicationTests.cs). No installed project manager, arbitrary package closure, importer/exporter or external-agent acceptance is inferred.
