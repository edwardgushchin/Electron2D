# Canonical document model

Текущий baseline `T-0145` добавляет отдельный internal project `src/Electron2D.ProjectSystem`. Он не расширяет публичный runtime API `Electron2D`, а даёт общий слой для будущих Editor, Tooling, CLI, MCP и headless-сценариев.

`internal project` здесь означает отдельную .NET-сборку с типами, доступными только дружественным сборкам: тестам, редактору и будущему tooling. Пользовательская игра не должна зависеть от этих типов напрямую.

## Classification

`ProjectDocumentClassifier` распознаёт:

- scene documents: `*.scene.json` или JSON format `Electron2D.SceneFile`;
- resource documents: `*.e2res` или JSON format `Electron2D.ResourceFile`;
- settings documents: `project.e2project.json`, input/export settings и JSON format с `Settings`;
- C# code: `*.cs`;
- generic JSON;
- plain text;
- generated files, включая `.electron2d/import-cache/`, `.electron2d/workspaces/`, `.electron2d/context/`, `.electron2d/session/`, `bin/`, `obj/` и paths с `/generated/`;
- binary assets through `ClassifyBinary()`;
- editor metadata under `.electron2d/tasks/` and `.electron2d/user/`.

Generated and binary flags are explicit, so future merge policy can reject unsafe automatic merges before touching file contents.

## Identity

`ProjectDocumentParser` normalizes project-relative paths to `/` separators, rejects `..`, and creates stable `DocumentId`:

- scene: `document://scene/res://...`;
- resource: `document://resource/uid://...` when top-level `uid` exists;
- settings: `document://settings/res://...`;
- code/json/text/generated/binary/editor metadata use their own kind segment and normalized `res://...` path.

Scene and settings documents currently use the normalized `res://` path because their current file formats do not yet contain a schema-level document UID. Resource documents already use the top-level `uid://...`.

## Revisions

`ProjectDocumentRevisionState.Clean(value)` creates equal persisted and in-memory revisions. `ProjectDocumentSnapshot.IsDirty` becomes true when `InMemoryRevision` differs from `PersistedRevision`.

`WithInMemoryRevision()` updates only the in-memory revision. `MarkPersisted()` copies the current in-memory revision to persisted revision after a successful save. Revisions are monotonic numbers inside one workspace session, not timestamps.

## Object map

Parser output is a flat `ProjectDocumentSnapshot.Objects` map:

- scene nodes become `scene-node:<id>`;
- resource main object becomes `resource:main`;
- internal resources become `resource-internal:<id>`;
- external references become `resource-external:<id>`;
- settings root becomes `settings:root`;
- generic JSON root becomes `json:root`;
- text/code root becomes `text:root`;
- binary root becomes `binary:root`.

Each object carries parent UID, type, name, order and canonical property strings. This is enough for the first structural diff without taking ownership of stable formatting, migrations or JSON Schema; those remain in `T-0117`.

## Serializer

`ProjectDocumentStructuralSerializer` writes and reads a debug JSON snapshot with format `Electron2D.ProjectDocumentSnapshot`. `debug JSON snapshot` means a test/tooling representation of the parsed object map, not a replacement for user project files.

The serializer preserves document identity, classification, revisions, object UIDs, parent links and properties. It is used to prove that parser output can be round-tripped as canonical structure.

## Structural diff

`ProjectDocumentStructuralDiff.Compare(before, after)` requires the same `DocumentId` and returns typed changes:

- `PropertyChanged`;
- `Renamed`;
- `Moved`;
- `Deleted`;
- `Added`.

Rename and move are detected by stable `ObjectUid`; they do not become delete/add changes when the UID remains the same. `AreNonOverlappingPropertyChanges()` returns true only for two property changes on different objects or different property paths.

## Проверка

Focused check:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~ProjectDocumentModelTests
```

Source header check for the new C# files:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
```
