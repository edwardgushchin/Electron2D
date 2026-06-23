# Editor Capability Manifest

Статус: реализованный internal contract для `T-0142`.
Обновлено: 2026-06-23.
Связанные документы: [Editor Capability Manifest specification](../../specifications/tooling/editor-capability-manifest.md); [Electron2D.Tooling service boundary](tooling-service-boundary.md); [Локальный MCP adapter](../mcp/mcp-server.md); [Machine-readable API manifest](../documentation/api-manifest.md).

`Editor Capability Manifest` — машиночитаемый список возможностей Editor, которые имеют проектный смысл и должны быть доступны AI через Tooling/MCP без GUI automation. Он не заменяет реализацию Editor UI и не делает частично готовые workflows завершёнными; manifest фиксирует текущий статус каждой capability и проверяет, что заявленный support не расходится между Editor, Tooling, MCP и CLI.

## Artifact

Canonical tracked file:

```text
data/editor/electron2d-editor-capabilities.json
```

Файл создаётся тем же порядком полей, что и `EditorCapabilityManifestSerializer.Serialize(EditorCapabilityManifestFactory.CreateDefault())`. Integration test сравнивает generated output с tracked JSON byte-for-byte после нормализации line endings.

Manifest ссылается на public API manifest:

```text
data/api/electron2d-api-manifest.json
```

Stable API identifiers используются для связи capability с runtime/API профилем, Inspector properties, scene/resource operations и runtime control.

## Current coverage

Текущий manifest покрывает обязательные категории:

- scene, node, Inspector properties, signals и groups;
- resources и import settings;
- Input Map, Project Settings, main scene и export presets;
- SpriteFrames, AnimationPlayer, TileMap и UI themes;
- tests, diagnostics, runtime control, script workspace и managed debugger control.

Часть строк уже `supported`: shared workspace transaction для text project documents, resource import job, test/export/run job, diagnostics query, fine-grained visible runtime controls, script document mutations, live C# IDE queries и managed debugger control. Часть строк помечена `partial`: специализированные SpriteFrames/AnimationPlayer/TileMap/UI theme workflows. Такой статус означает, что строка входит в parity map, но её production workflow закрывается последующими задачами.

Новые release-required строки для `T-0161`:

- `script.workspace.mutate` связывает visible Script workspace, Tooling command `script_apply_text_edits` и MCP tool `script_apply_text_edits`;
- `script.workspace.ide` связывает visible Script workspace, Tooling command `script_get_diagnostics` и MCP tool `script_get_diagnostics`;
- `debugger.managed.control` связывает visible debugger workspace, Tooling command `debug_start` и MCP tool `debug_start`.

## Verifier

`EditorCapabilityManifestVerifier.Verify(...)` принимает manifest, опубликованные MCP tool/resource names, опубликованные Tooling command names и repository root. Он возвращает `EditorCapabilityManifestVerificationResult` со structured diagnostics.

Проверяются:

- schema/version;
- существование `data/api/electron2d-api-manifest.json`;
- дубли capability identifiers;
- покрытие required categories;
- обязательные поля endpoint-ов;
- `supported` Editor capability не имеет более слабых Tooling/MCP binding;
- `releaseRequired` capability не использует partial/experimental Tooling или MCP;
- `projectMutation` и `backgroundJob` release capability имеют `dedicatedCommand` или `genericTransaction`, а не `notApplicable`;
- release-required `runtimeAction` может иметь CLI `notApplicable`, если действие имеет смысл только для активной visible Editor session и покрыто Tooling/MCP parity;
- Tooling command и MCP tool/resource опубликованы в catalog.

## Diagnostics

| Code | Meaning |
| --- | --- |
| `E2D-CAPABILITY-0001` | Editor/Tooling/MCP parity нарушен: поддержанная Editor capability не имеет полной Tooling/MCP поддержки или release-required строка использует partial/experimental binding |
| `E2D-CAPABILITY-0002` | CLI binding policy некорректна: release-required mutation/job capability не имеет dedicated или generic CLI path |
| `E2D-CAPABILITY-0003` | Manifest shape некорректен: версия, API manifest reference, duplicate id, required category или mandatory field |
| `E2D-CAPABILITY-0004` | Capability ссылается на Tooling command или MCP tool/resource, которого нет в опубликованных списках |

## MCP exposure

MCP resource:

```text
electron2d://editor/capabilities
```

возвращает canonical manifest JSON.

`e2d mcp serve --format json` добавляет `data.editorCapabilityManifest`:

- `path`;
- `capabilities`;
- `releaseRequired`;
- `succeeded`;
- `diagnostics`.

Это краткое поле нужно AI-клиентам для fail-closed проверки manifest без отдельного resource read. Полный список capabilities остаётся в resource `electron2d://editor/capabilities`.

## Checks

Focused check:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~EditorCapabilityManifestTests
```

Regression slice:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorCapabilityManifestTests|FullyQualifiedName~Electron2DMcpServerTests|FullyQualifiedName~ToolingServiceBoundaryTests|FullyQualifiedName~DiagnosticsCoreTests|FullyQualifiedName~Electron2DCliWorkflowTests"
```

Source/header and documentation checks remain required before commit when files in `src/`, `tests/`, `tools/` or `docs/` changed.
